using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(PlayerInventoryHolder))]
[RequireComponent(typeof(ActiveItemController))]
public class PlayerBuilder : MonoBehaviour
{
    [Header("Catalog & Raycast")]
    public BuildCatalogSO catalog;
    public float maxDistance = 8f;
    public LayerMask surfaceLayers = ~0;

    [Header("Preview")]
    public Material previewValidMat;
    public Material previewInvalidMat;
    [Range(0.1f, 1f)] public float previewAlpha = 0.6f;

    [Header("Keys")]
    public KeyCode placeKey = KeyCode.Mouse0;
    public KeyCode cancelKey = KeyCode.Mouse1;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;

    [Header("Camera")]
    public Camera viewCamera;

    [Header("Placed Objects")]
    public string placedLayerName = "Buildable";
    public bool forceSolidColliders = true;

    [Header("Surface stacking")]
    [Tooltip("仅当命中表面的朝上程度 >= 该阈值时，才允许按 Surface 贴放")]
    [Range(0f, 1f)] public float minUpDotForSurface = 0.6f;

    // runtime
    PlayerInventoryHolder _inv;
    ActiveItemController _active;
    GameObject _ghost;
    string _ghostItemId = "";
    float _yaw = 0f;

    readonly List<(Renderer r, Material[] original)> _renderers = new();
    Collider[] _playerCols;
    static readonly Collider[] _hits = new Collider[64];
    int _placedLayer = -1;

    void Awake()
    {
        _inv = GetComponent<PlayerInventoryHolder>();
        _active = GetComponent<ActiveItemController>();
        if (viewCamera == null) viewCamera = Camera.main;

        _playerCols = GetComponentsInChildren<Collider>(true);

        _placedLayer = LayerMask.NameToLayer(placedLayerName);
#if UNITY_EDITOR
        if (_placedLayer < 0)
            Debug.LogWarning($"[PlayerBuilder] 找不到图层 \"{placedLayerName}\"，将使用Prefab原图层。建议在 Project Settings → Tags and Layers 新建该层，并把它加入角色的 Ground Layers。");
#endif
    }

    void Update()
    {
        string id = _active != null ? _active.ActiveId : "";
        var entry = catalog != null ? catalog.Get(id) : null;

        if (entry == null) { ClearGhost(); return; }

        if (_ghost == null || _ghostItemId != id)
            BuildGhost(entry, id);

        if (Input.GetKeyDown(rotateLeftKey)) _yaw -= entry.yawStep;
        if (Input.GetKeyDown(rotateRightKey)) _yaw += entry.yawStep;

        bool ok = ComputePoseAndCheck(entry, out Vector3 pos, out Quaternion rot);

        if (_ghost != null)
        {
            _ghost.transform.SetPositionAndRotation(pos, rot);
            SetGhostValid(ok);
        }

        if (ok && GetPlacePressed())
        {
            if (TryConsumeOne(id)) Place(entry, id, pos, rot);
#if UNITY_EDITOR
            else Debug.Log("[PlayerBuilder] 背包数量不足");
#endif
        }

        if (Input.GetKeyDown(cancelKey)) ClearGhost();
    }

    bool GetPlacePressed()
    {
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
        if (Mouse.current != null) return Mouse.current.leftButton.wasPressedThisFrame;
#endif
        return Input.GetKeyDown(placeKey);
    }

    bool TryConsumeOne(string id) => _inv != null && _inv.RemoveItem(id, 1) > 0;

    void Place(BuildCatalogSO.Entry e, string id, Vector3 pos, Quaternion rot)
    {
        var parent = e.optionalParentAtRuntime != null ? e.optionalParentAtRuntime : null;
        var obj = Instantiate(e.prefab, pos, rot, parent);

        if (_placedLayer >= 0) SetLayerRecursively(obj, _placedLayer);

        if (forceSolidColliders)
        {
            foreach (var c in obj.GetComponentsInChildren<Collider>(true))
            {
                if (c == null) continue;
                c.isTrigger = false;
                c.enabled = true;
            }
        }

        var po = obj.GetComponent<PlacedObject>();
        if (po == null) po = obj.AddComponent<PlacedObject>();
        po.itemId = id;
    }

    // ---------------- core ----------------

    bool ComputePoseAndCheck(BuildCatalogSO.Entry e, out Vector3 pos, out Quaternion rot)
    {
        var cam = viewCamera != null ? viewCamera : Camera.main;
        var ray = cam != null
            ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(transform.position + Vector3.up * 1.5f, transform.forward);

        if (!Physics.Raycast(ray, out var hit, maxDistance, e.surfaceLayers, QueryTriggerInteraction.Ignore))
        {
            pos = Vector3.zero; rot = Quaternion.identity;
            return false;
        }

        // 对 Surface：只允许“顶面”（法线朝上）
        if (e.snapMode == BuildSnapMode.Surface)
        {
            float upDot = Vector3.Dot(hit.normal, Vector3.up);
            if (upDot < minUpDotForSurface)    // 命中墙面/斜面则不允许
            {
                pos = hit.point; rot = Quaternion.identity;
                return false;
            }
        }

        Vector3 targetPos = hit.point;
        Quaternion targetRot = Quaternion.identity;

        switch (e.snapMode)
        {
            case BuildSnapMode.Ground:
            case BuildSnapMode.Surface:
                targetPos = SnapXZ(targetPos, e.grid);
                targetRot = Quaternion.Euler(0f, SnapYaw(_yaw, e.yawStep), 0f);
                break;
            case BuildSnapMode.Wall:
                var forward = -hit.normal; forward.y = 0f; forward.Normalize();
                var yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                yaw = SnapYaw(yaw + _yaw, e.yawStep);
                targetRot = Quaternion.Euler(0f, yaw, 0f);
                targetPos = hit.point + hit.normal * 0.01f; // 贴墙
                break;
        }

        // 先把 Ghost 放到候选位姿
        if (_ghost != null) _ghost.transform.SetPositionAndRotation(targetPos, targetRot);

        // 用世界包围盒让“底面=命中点+yOffset+skin”（兼容轴心在底/中/偏移）
        if (_ghost != null && (e.snapMode == BuildSnapMode.Ground || e.snapMode == BuildSnapMode.Surface))
        {
            if (TryGetWorldBounds(_ghost, out Bounds b))
            {
                float skin = Mathf.Max(0.001f, e.boundsInflation * 0.5f);
                float wantBottomY = hit.point.y + e.yOffset + skin;
                float deltaY = wantBottomY - b.min.y;
                targetPos.y += deltaY;
                _ghost.transform.SetPositionAndRotation(targetPos, targetRot);
            }
        }

        // === 重叠检测 ===
        // 允许“贴在承载物体上”：忽略这次射线命中的那棵层级（桌子/箱子等）
        Transform supportRoot = hit.collider != null ? hit.collider.transform.root : null;

        LayerMask layers = (e.blockerLayers.value == 0) ? (LayerMask)~0 : e.blockerLayers;
        bool blocked = HasColliderOverlapExcept(_ghost, layers, supportRoot);

        pos = targetPos;
        rot = targetRot;
        return !blocked;
    }

    // 忽略 supportRoot（命中承载物体）进行 Overlap
    bool HasColliderOverlapExcept(GameObject ghostRoot, LayerMask layers, Transform supportRoot)
    {
        if (ghostRoot == null) return true;

        foreach (var bc in ghostRoot.GetComponentsInChildren<BoxCollider>(true))
        {
            if (bc == null) continue;
            Vector3 center = bc.transform.TransformPoint(bc.center);
            Vector3 half = Vector3.Scale(bc.size * 0.5f, Abs(bc.transform.lossyScale));
            Quaternion rot = bc.transform.rotation;

            int n = Physics.OverlapBoxNonAlloc(center, half, _hits, rot, layers, QueryTriggerInteraction.Ignore);
            if (AnyBlocking(_hits, n, supportRoot)) return true;
        }

        foreach (var sc in ghostRoot.GetComponentsInChildren<SphereCollider>(true))
        {
            if (sc == null) continue;
            Vector3 center = sc.transform.TransformPoint(sc.center);
            float maxS = MaxAbs(sc.transform.lossyScale);
            float radius = Mathf.Abs(sc.radius) * maxS;

            int n = Physics.OverlapSphereNonAlloc(center, radius, _hits, layers, QueryTriggerInteraction.Ignore);
            if (AnyBlocking(_hits, n, supportRoot)) return true;
        }

        foreach (var cc in ghostRoot.GetComponentsInChildren<CapsuleCollider>(true))
        {
            if (cc == null) continue;
            GetWorldCapsule(cc, out Vector3 p0, out Vector3 p1, out float r);

            int n = Physics.OverlapCapsuleNonAlloc(p0, p1, r, _hits, layers, QueryTriggerInteraction.Ignore);
            if (AnyBlocking(_hits, n, supportRoot)) return true;
        }

        return false;
    }

    bool AnyBlocking(Collider[] arr, int count, Transform supportRoot)
    {
        for (int i = 0; i < count; i++)
        {
            var col = arr[i];
            if (col == null) continue;
            if (!col.enabled) continue;
            if (col.isTrigger) continue;
            if (_ghost != null && col.transform.IsChildOf(_ghost.transform)) continue; // 忽略预览
            if (IsPlayerCollider(col)) continue;                                       // 忽略玩家
            if (supportRoot != null && col.transform.IsChildOf(supportRoot)) continue; // 忽略承载物体（允许接触）
            return true;
        }
        return false;
    }

    // --------- bounds / helpers ---------

    bool TryGetWorldBounds(GameObject root, out Bounds b)
    {
        b = default;

        var rs = root.GetComponentsInChildren<Renderer>(true);
        if (rs.Length > 0)
        {
            b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return true;
        }

        var cs = root.GetComponentsInChildren<Collider>(true);
        if (cs.Length > 0)
        {
            b = cs[0].bounds;
            for (int i = 1; i < cs.Length; i++) b.Encapsulate(cs[i].bounds);
            return true;
        }
        return false;
    }

    static void GetWorldCapsule(CapsuleCollider c, out Vector3 p0, out Vector3 p1, out float radius)
    {
        Transform t = c.transform;
        Vector3 lossy = Abs(t.lossyScale);
        float maxScale = MaxAbs(lossy);
        radius = Mathf.Abs(c.radius) * maxScale;

        Vector3 center = t.TransformPoint(c.center);
        float height = Mathf.Max(c.height * lossy[c.direction], radius * 2f);
        float half = height * 0.5f - radius;
        Vector3 axis = (c.direction == 0) ? t.right : (c.direction == 1) ? t.up : t.forward;

        p0 = center + axis * half;
        p1 = center - axis * half;
    }

    static Vector3 SnapXZ(Vector3 p, float grid)
    {
        if (grid <= 0.0001f) return p;
        p.x = Mathf.Round(p.x / grid) * grid;
        p.z = Mathf.Round(p.z / grid) * grid;
        return p;
    }

    static float SnapYaw(float y, float step)
    {
        if (step <= 0.0001f) return y;
        return Mathf.Round(y / step) * step;
    }

    void BuildGhost(BuildCatalogSO.Entry entry, string id)
    {
        ClearGhost();
        if (entry == null || entry.prefab == null) return;

        _ghost = Instantiate(entry.prefab);
        _ghost.name = "[GHOST] " + entry.prefab.name;
        _ghost.transform.position = transform.position + transform.forward * 2f;
        _ghostItemId = id;
        _yaw = 0f;

        foreach (var c in _ghost.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var rb in _ghost.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;

        _renderers.Clear();
        foreach (var r in _ghost.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            _renderers.Add((r, r.sharedMaterials));
        }
        SetGhostValid(false);
    }

    void SetGhostValid(bool valid)
    {
        var mat = valid ? previewValidMat : previewInvalidMat;
        if (mat == null) return;

        for (int i = 0; i < _renderers.Count; i++)
        {
            var (r, original) = _renderers[i];
            if (r == null) continue;

            var arr = new Material[original.Length];
            for (int j = 0; j < arr.Length; j++) arr[j] = mat;
            r.sharedMaterials = arr;

            foreach (var m in r.sharedMaterials)
            {
                if (m == null) continue;
                if (m.HasProperty("_Color"))
                {
                    var c = m.color; c.a = previewAlpha;
                    m.color = c;
                }
            }
        }
    }

    void ClearGhost()
    {
        if (_ghost != null) Destroy(_ghost);
        _ghost = null;
        _renderers.Clear();
        _ghostItemId = "";
    }

    bool IsPlayerCollider(Collider c)
    {
        if (c == null || _playerCols == null) return false;
        for (int i = 0; i < _playerCols.Length; i++)
            if (c == _playerCols[i]) return true;
        return false;
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursively(t.gameObject, layer);
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    static float MaxAbs(Vector3 v) => Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
}
