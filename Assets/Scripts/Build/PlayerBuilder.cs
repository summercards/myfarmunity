// Assets/Scripts/Build/PlayerBuilder.cs
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 简易「建造/家园摆放」：
/// - 玩家手持某个物品（ActiveItemController.ActiveId）
/// - 若该物品在 BuildCatalogSO 里有条目，则进入预览/摆放模式
/// - 按键放置：扣除背包数量1，实例化物体，并记录存档
/// </summary>
[RequireComponent(typeof(PlayerInventoryHolder))]
[RequireComponent(typeof(ActiveItemController))]
public class PlayerBuilder : MonoBehaviour
{
    [Header("Catalog & Raycast")]
    public BuildCatalogSO catalog;
    public float maxDistance = 8f;

    [Tooltip("用于命中预览/摆放的层（通常是地面、墙体、桌面等）")]
    public LayerMask surfaceLayers = ~0;

    [Header("Preview")]
    public Material previewValidMat;
    public Material previewInvalidMat;
    public float previewAlpha = 0.6f;

    [Header("Keys")]
    public KeyCode placeKey = KeyCode.Mouse0;
    public KeyCode cancelKey = KeyCode.Mouse1;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;

    [Header("Camera")]
    public Camera viewCamera;  // 不指定则自动抓主相机

    // 运行时
    PlayerInventoryHolder _inv;
    ActiveItemController _active;
    GameObject _ghost;
    string _ghostItemId = "";
    float _yaw = 0f;                       // 离散旋转累计
    BuildCatalogSO.Entry _entry;           // 当前物品的规则

    // 预览材质缓存
    readonly List<(Renderer r, Material[] original)> _renderers = new();

    void Awake()
    {
        _inv = GetComponent<PlayerInventoryHolder>();
        _active = GetComponent<ActiveItemController>();
        if (!viewCamera) viewCamera = Camera.main;
    }

    void Update()
    {
        string id = _active ? _active.ActiveId : "";
        var entry = catalog ? catalog.Get(id) : null;

        if (entry == null)
        {
            ClearGhost();
            return;
        }

        // 若切换了物品，重建预览
        if (_ghost == null || _ghostItemId != id)
        {
            BuildGhost(entry, id);
        }

        // 旋转快捷键
        if (Input.GetKeyDown(rotateLeftKey)) _yaw -= entry.yawStep;
        if (Input.GetKeyDown(rotateRightKey)) _yaw += entry.yawStep;

        // 计算命中与最终姿态
        bool ok = ComputePose(entry, out Vector3 pos, out Quaternion rot);

        // 更新预览位置与颜色
        if (_ghost)
        {
            _ghost.transform.SetPositionAndRotation(pos, rot);
            SetGhostValid(ok);
        }

        // 下单摆放
        if (ok && GetPlacePressed())
        {
            if (TryConsumeOne(id))
            {
                Place(entry, id, pos, rot);
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("[PlayerBuilder] 背包数量不足");
#endif
            }
        }

        // 右键取消预览
        if (Input.GetKeyDown(cancelKey))
            ClearGhost();
    }

    bool GetPlacePressed()
    {
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
        if (Mouse.current != null) return Mouse.current.leftButton.wasPressedThisFrame;
#endif
        return Input.GetKeyDown(placeKey);
    }

    bool TryConsumeOne(string id)
    {
        if (_inv == null) return false;
        return _inv.RemoveItem(id, 1) > 0;
    }

    void Place(BuildCatalogSO.Entry e, string id, Vector3 pos, Quaternion rot)
    {
        var parent = e.optionalParentAtRuntime ? e.optionalParentAtRuntime : null;
        var obj = Instantiate(e.prefab, pos, rot, parent);
        var po = obj.GetComponent<PlacedObject>();
        if (!po) po = obj.AddComponent<PlacedObject>();
        po.itemId = id;

        // 可选：再朝下投一根射线让它落地更“贴地”
        if (e.alignToGroundAfterPlace)
        {
            if (Physics.Raycast(obj.transform.position + Vector3.up * 0.1f, Vector3.down,
                out var hit, e.groundProbeDistance, e.surfaceLayers, QueryTriggerInteraction.Ignore))
            {
                obj.transform.position = hit.point + Vector3.up * e.yOffset;
            }
        }
    }

    bool ComputePose(BuildCatalogSO.Entry e, out Vector3 pos, out Quaternion rot)
    {
        // 从视口中心发射一条射线
        var cam = viewCamera ? viewCamera : Camera.main;
        var ray = cam ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)) :
                        new Ray(transform.position + Vector3.up * 1.5f, transform.forward);

        if (!Physics.Raycast(ray, out var hit, maxDistance, e.surfaceLayers, QueryTriggerInteraction.Ignore))
        {
            pos = Vector3.zero; rot = Quaternion.identity;
            return false;
        }

        // 根据 snap 模式求姿态
        Vector3 targetPos = hit.point;
        Quaternion targetRot = Quaternion.identity;

        switch (e.snapMode)
        {
            case BuildSnapMode.Ground:
                targetPos = SnapXZ(targetPos + Vector3.up * e.yOffset, e.grid);
                targetRot = Quaternion.Euler(0f, SnapYaw(_yaw, e.yawStep), 0f);
                break;
            case BuildSnapMode.Surface:
                targetPos = SnapXZ(targetPos + Vector3.up * e.yOffset, e.grid);
                targetRot = Quaternion.Euler(0f, SnapYaw(_yaw, e.yawStep), 0f);
                break;
            case BuildSnapMode.Wall:
                // 让 forward 朝向 -法线（面向玩家）
                var forward = -hit.normal; forward.y = 0f; forward.Normalize();
                var yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                yaw = SnapYaw(yaw + _yaw, e.yawStep);
                targetRot = Quaternion.Euler(0f, yaw, 0f);
                targetPos = hit.point + hit.normal * 0.01f;
                break;
        }

        // Overlap 检测：使用盒体（半尺寸）
        bool collide = Physics.CheckBox(
            targetPos,
            e.checkBoxSize,
            targetRot,
            e.blockerLayers,
            QueryTriggerInteraction.Ignore);

        pos = targetPos;
        rot = targetRot;
        return !collide;
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

        _entry = entry;
        _ghostItemId = id;
        _yaw = 0f;

        _ghost = Instantiate(entry.prefab);
        _ghost.name = "[GHOST] " + entry.prefab.name;
        _ghost.transform.position = transform.position + transform.forward * 2f;

        foreach (var c in _ghost.GetComponentsInChildren<Collider>())
            c.enabled = false;

        foreach (var rb in _ghost.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;

        // 缓存原材质，并换成预览材质
        _renderers.Clear();
        foreach (var r in _ghost.GetComponentsInChildren<Renderer>())
        {
            if (!r) continue;
            _renderers.Add((r, r.sharedMaterials));
        }
        SetGhostValid(false);
    }

    void SetGhostValid(bool valid)
    {
        var mat = valid ? previewValidMat : previewInvalidMat;
        if (!mat) return;

        for (int i = 0; i < _renderers.Count; i++)
        {
            var (r, original) = _renderers[i];
            if (!r) continue;

            var arr = new Material[original.Length];
            for (int j = 0; j < arr.Length; j++) arr[j] = mat;
            r.sharedMaterials = arr;

            // 设置透明
            foreach (var m in r.sharedMaterials)
            {
                if (!m) continue;
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
        if (_ghost) Destroy(_ghost);
        _ghost = null;
        _renderers.Clear();
        _ghostItemId = "";
        _entry = null;
    }
}
