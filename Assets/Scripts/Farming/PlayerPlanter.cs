using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInventoryHolder))]
[RequireComponent(typeof(ActiveItemController))]
public class PlayerPlanter : MonoBehaviour
{
    [Header("Config")]
    public SeedPlantDataSO plantDB;
    [Tooltip("可种植的地面层（射线命中层）")]
    public LayerMask plantableLayers;
    [Tooltip("最大种植距离")]
    public float maxDistance = 8f;
    [Tooltip("种植键")]
    public KeyCode plantKey = KeyCode.F;
    [Tooltip("使用相机中心射线")]
    public bool useCameraRay = true;

    [Header("放置校验（防重叠/被阻挡）")]
    [Tooltip("与其它作物的最小间距（米）")]
    public float minSpacing = 1.2f;
    [Tooltip("从地面向上需要的净空高度（米）")]
    public float clearanceHeight = 1.6f;
    [Tooltip("净空半径（米）— 近似植株半径")]
    public float clearanceRadius = 0.45f;
    [Tooltip("阻挡层（树/石头/建筑/道具等）— 不要勾地面层！")]
    public LayerMask blockLayers;

    [Header("Marker (地面指示环)")]
    public bool showMarker = true;
    [Tooltip("只在可种地面显示红圈")]
    public bool markerOnlyOnPlantable = true;
    public float markerRadius = 0.35f;
    public float markerWidth = 0.02f;
    public float markerYOffset = 0.02f;
    public int markerSegments = 36;
    public Color okColor = new Color(0f, 1f, 0f, 0.95f);
    public Color badColor = new Color(1f, 0f, 0f, 0.95f);

    [Header("Rotation Control (Yaw)")]
    public bool enableYawControl = true;          // 开关
    [Tooltip("Q/E 按住旋转的角速度（度/秒）")]
    public float rotateSpeed = 120f;
    [Tooltip("左旋键")]
    public KeyCode yawLeftKey = KeyCode.Q;
    [Tooltip("右旋键")]
    public KeyCode yawRightKey = KeyCode.E;
    [Tooltip("滚轮可调角度（每格度数）")]
    public float wheelStep = 10f;
    public bool useMouseWheel = true;
    [Tooltip("角度吸附（开启后按 snapStep 取整）")]
    public bool snapYaw = true;
    [Tooltip("吸附步长（度）例如 15/30/45/90")]
    public float snapStep = 15f;
    [Tooltip("随机朝向（每次种植都随机一个 yaw ）")]
    public bool randomYawOnPlant = false;
    [Tooltip("R 键恢复为 0 度")]
    public KeyCode resetYawKey = KeyCode.R;

    [Header("Debug")]
    public bool debugPlantCheck = false;
    public bool requirePlantableSurface = true;

    PlayerInventoryHolder _inv;
    ActiveItemController _active;
    float _cooldown = 0f;

    // marker
    LineRenderer _markerLR;
    bool _hasValidPoint = false;
    Vector3 _cachedPoint, _cachedNormal;

    // 忽略玩家自身（防止顶空检查误判）
    HashSet<Collider> _selfCols = new HashSet<Collider>();

    // —— 当前要应用的 Yaw（绕法线方向的角度，度）——
    float _currentYaw = 0f;

    void Awake()
    {
        _inv = GetComponent<PlayerInventoryHolder>();
        _active = GetComponent<ActiveItemController>();

        foreach (var c in GetComponentsInChildren<Collider>())
            _selfCols.Add(c);

        CreateMarker();
    }

    void Update()
    {
        if (_cooldown > 0f) _cooldown -= Time.deltaTime;

        HandleYawInput();       // <== 新增：处理朝向输入
        UpdateMarker();

        if (Input.GetKeyDown(plantKey)) TryPlant();
    }

    // =============== 朝向输入 ===============
    void HandleYawInput()
    {
        if (!enableYawControl) return;

        float delta = 0f;

        // Q / E 按住旋转
        if (Input.GetKey(yawLeftKey)) delta -= rotateSpeed * Time.deltaTime;
        if (Input.GetKey(yawRightKey)) delta += rotateSpeed * Time.deltaTime;

        // 滚轮（每格固定步进）
        if (useMouseWheel)
        {
            float w = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(w) > 0.0001f) delta += Mathf.Sign(w) * wheelStep;
        }

        if (Mathf.Abs(delta) > 0.0001f)
        {
            _currentYaw += delta;
            if (snapYaw && snapStep > 0.1f)
                _currentYaw = Mathf.Round(_currentYaw / snapStep) * snapStep;
        }

        // 重置
        if (Input.GetKeyDown(resetYawKey)) _currentYaw = 0f;
    }

    // ================== 指示环 ==================
    void CreateMarker()
    {
        if (!showMarker) return;

        var go = new GameObject("PlantMarker");
        _markerLR = go.AddComponent<LineRenderer>();
        _markerLR.useWorldSpace = true;
        _markerLR.loop = true;
        _markerLR.widthMultiplier = markerWidth;
        _markerLR.positionCount = Mathf.Max(12, markerSegments);

        Shader sh = Shader.Find("Unlit/Color");
        if (!sh) sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (!sh) sh = Shader.Find("Sprites/Default");
        if (!sh) sh = Shader.Find("Standard");
        var mat = new Material(sh);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", okColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", okColor);
        _markerLR.material = mat;

        _markerLR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _markerLR.receiveShadows = false;
        _markerLR.enabled = false;
    }

    void UpdateMarker()
    {
        if (!showMarker || _markerLR == null) return;

        bool hasHit = RaycastPlantPoint(out Vector3 hitPos, out Vector3 hitNormal, out RaycastHit hit);
        if (!hasHit) { _markerLR.enabled = false; _hasValidPoint = false; return; }

        var surf = hit.collider ? hit.collider.GetComponentInParent<PlantableSurface>() : null;
        string curId = (_active != null) ? _active.ActiveId : null;

        if (markerOnlyOnPlantable)
        {
            if (requirePlantableSurface && surf == null) { _markerLR.enabled = false; _hasValidPoint = false; return; }
            if (surf != null)
            {
                if (string.IsNullOrEmpty(curId) || !surf.IsItemAllowed(curId) || !surf.IsSlopeOK(hitNormal))
                { _markerLR.enabled = false; _hasValidPoint = false; return; }
            }
        }

        Vector3 pos = (surf != null) ? surf.SnapPosition(hitPos) : hitPos + hitNormal * markerYOffset;
        Vector3 normal = hitNormal;
        bool canPlant = !IsTooCloseToOtherCrops(pos, minSpacing) && !IsTopBlocked(pos, normal);

        _cachedPoint = pos + normal * markerYOffset;
        _cachedNormal = normal;
        _hasValidPoint = canPlant;

        DrawCircle(_cachedPoint, _cachedNormal, markerRadius);

        var c = canPlant ? okColor : badColor;
        if (_markerLR.material.HasProperty("_BaseColor")) _markerLR.material.SetColor("_BaseColor", c);
        if (_markerLR.material.HasProperty("_Color")) _markerLR.material.SetColor("_Color", c);

        _markerLR.enabled = true;
    }

    void DrawCircle(Vector3 center, Vector3 normal, float radius)
    {
        if (_markerLR == null) return;
        int seg = Mathf.Max(12, markerSegments);

        normal = normal.normalized;
        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent.sqrMagnitude < 1e-4f) tangent = Vector3.Cross(normal, Vector3.right);
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(normal, tangent);

        _markerLR.positionCount = seg;
        for (int i = 0; i < seg; i++)
        {
            float ang = (i / (float)seg) * Mathf.PI * 2f;
            Vector3 dir = Mathf.Cos(ang) * tangent + Mathf.Sin(ang) * bitangent;
            _markerLR.SetPosition(i, center + dir * radius);
        }
    }

    // ================== 种植 ==================
    void TryPlant()
    {
        if (_inv == null || _active == null || plantDB == null) return;
        if (_cooldown > 0f) return;

        string id = _active.ActiveId;
        if (string.IsNullOrEmpty(id)) { Log("ActiveId 为空"); return; }

        var entry = plantDB.GetByPlantItemId(id);
        if (entry == null || entry.cropPrefab == null) { Log("SeedDB 未配置或无 CropPrefab"); return; }

        Vector3 pos, normal;
        if (_hasValidPoint)
        {
            pos = _cachedPoint;
            normal = _cachedNormal;
        }
        else
        {
            if (!TryGetValidPlantPoint(id, out pos, out normal)) return;
            pos += normal * markerYOffset;
        }

        // —— 计算最终朝向：先对齐法线，再绕法线加上你调的 yaw（可选随机）——
        float yaw = _currentYaw;
        if (randomYawOnPlant) yaw += Random.Range(0f, 360f);

        Quaternion alignRot = Quaternion.FromToRotation(Vector3.up, normal);
        Quaternion yawRot = Quaternion.AngleAxis(yaw, normal);
        Quaternion rot = yawRot * alignRot;

        var cropObj = Instantiate(entry.cropPrefab, pos, rot);
        var crop = cropObj.GetComponent<CropPlant>();
        if (!crop) crop = cropObj.AddComponent<CropPlant>();
        crop.Init(entry);


        // ★★ 新增：确保有 CropPersistence，并把 entryId 写进去（用你种下去的那个种子/作物 id）
        var cp = cropObj.GetComponent<CropPersistence>();
        if (cp == null) cp = cropObj.AddComponent<CropPersistence>();
        cp.entryId = id;   // 这里的 id 就是你当前种下的商店/背包里的“作物 ItemId”

        string keepId = id;
        _inv.RemoveItem(id, 1);
        var ui = FindObjectOfType<InventoryUI>();
        if (ui) ui.RefreshAll();
        if (StillHasItem(keepId)) { try { _active.SetActive(keepId, true); } catch { } }

        _cooldown = Mathf.Max(0.05f, entry.plantCooldown);
    }

    bool StillHasItem(string id)
    {
        if (string.IsNullOrEmpty(id) || _inv?.Inventory?.slots == null) return false;
        foreach (var s in _inv.Inventory.slots)
            if (s != null && s.count > 0 && s.id == id) return true;
        return false;
    }

    bool TryGetValidPlantPoint(string itemId, out Vector3 outPos, out Vector3 outNormal)
    {
        outPos = Vector3.zero; outNormal = Vector3.up;

        if (!RaycastPlantPoint(out Vector3 hitPos, out Vector3 hitNormal, out RaycastHit hit))
        { Log("Raycast 未命中 plantableLayers"); return false; }

        var surf = hit.collider ? hit.collider.GetComponentInParent<PlantableSurface>() : null;
        if (requirePlantableSurface && !surf) { Log("没有 PlantableSurface"); return false; }
        if (surf != null)
        {
            if (!surf.IsItemAllowed(itemId)) { Log($"物品 {itemId} 未被允许"); return false; }
            if (!surf.IsSlopeOK(hitNormal)) { Log("坡度过大"); return false; }
            outPos = surf.SnapPosition(hitPos);
        }
        else
        {
            outPos = hitPos + hitNormal * markerYOffset;
        }
        outNormal = hitNormal;

        if (minSpacing > 0f && IsTooCloseToOtherCrops(outPos, minSpacing))
        { Log("距离其它作物太近"); return false; }

        if (IsTopBlocked(outPos, outNormal))
        { Log("头顶空间被阻挡"); return false; }

        return true;
    }

    bool IsTopBlocked(Vector3 pos, Vector3 normal)
    {
        var clearanceMask = blockLayers & ~plantableLayers;
        if (clearanceHeight <= 0f || clearanceRadius <= 0f || clearanceMask == 0) return false;

        Vector3 a = pos + normal * (clearanceRadius + 0.03f);
        Vector3 b = pos + normal * Mathf.Max(clearanceHeight, clearanceRadius + 0.05f);

        var cols = Physics.OverlapCapsule(a, b, clearanceRadius, clearanceMask, QueryTriggerInteraction.Ignore);
        foreach (var c in cols)
        {
            if (!c) continue;
            if (_selfCols.Contains(c)) continue; // 忽略玩家自身
            return true;
        }
        return false;
    }

    bool IsTooCloseToOtherCrops(Vector3 pos, float radius)
    {
        var cols = Physics.OverlapSphere(pos, radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var c in cols)
        {
            var cp = c ? c.GetComponentInParent<CropPlant>() : null;
            if (cp != null) return true;
        }
        foreach (var cp in FindObjectsOfType<CropPlant>())
        {
            if (!cp) continue;
            if (Vector3.Distance(cp.transform.position, pos) < radius) return true;
        }
        return false;
    }

    bool RaycastPlantPoint(out Vector3 point, out Vector3 normal, out RaycastHit hit)
    {
        Ray ray;
        if (useCameraRay && Camera.main)
            ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
        else
            ray = new Ray(transform.position + Vector3.up * 1.2f, transform.forward);

        if (Physics.Raycast(ray, out hit, maxDistance, plantableLayers, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            normal = hit.normal;
            return true;
        }
        point = Vector3.zero;
        normal = Vector3.up;
        return false;
    }

    void Log(string msg)
    {
        if (debugPlantCheck) Debug.Log($"[PlayerPlanter] {msg}");
    }
}
