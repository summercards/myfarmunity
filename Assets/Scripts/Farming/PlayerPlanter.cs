using UnityEngine;

[RequireComponent(typeof(PlayerInventoryHolder))]
[RequireComponent(typeof(ActiveItemController))]
public class PlayerPlanter : MonoBehaviour
{
    [Header("Config")]
    public SeedPlantDataSO plantDB;
    [Tooltip("用于射线命中的层（把你的地面层勾上）")]
    public LayerMask plantableLayers = ~0;
    [Tooltip("最大种植距离")] public float maxDistance = 8f;
    [Tooltip("种植键")] public KeyCode plantKey = KeyCode.F;
    [Tooltip("使用相机中心射线")] public bool useCameraRay = true;

    [Header("Marker (指示环，现阶段默认关闭)")]
    public bool showMarker = false;                 // ★ 先关闭圆环
    public float markerRadius = 0.35f;
    public float markerWidth = 0.02f;
    public float markerYOffset = 0.02f;
    public int markerSegments = 36;
    public Color okColor = new Color(0f, 1f, 0f, 0.95f);
    public Color badColor = new Color(1f, 0f, 0f, 0.95f);

    PlayerInventoryHolder _inv;
    ActiveItemController _active;
    float _cooldown = 0f;

    // ---- 指示环（先不用，也保留以便后面开启）----
    LineRenderer _markerLR;
    bool _hasValidPoint = false;
    Vector3 _cachedPoint, _cachedNormal;

    void Awake()
    {
        _inv = GetComponent<PlayerInventoryHolder>();
        _active = GetComponent<ActiveItemController>();
        if (plantableLayers.value == 0) plantableLayers = ~0; // Inspector 丢失时兜底
        if (showMarker) CreateMarker();
    }

    void Update()
    {
        if (_cooldown > 0f) _cooldown -= Time.deltaTime;

        if (showMarker) UpdateMarker();

        if (Input.GetKeyDown(plantKey)) TryPlant();
    }

    // ======= 仅命中“有 PlantableSurface 的地面”才返回 true =======
    bool RaycastPlantPoint(out Vector3 point, out Vector3 normal, out PlantableSurface surface)
    {
        surface = null;
        point = Vector3.zero;
        normal = Vector3.up;

        Ray ray = useCameraRay && Camera.main
            ? Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f))
            : new Ray(transform.position + Vector3.up * 1.2f, transform.forward);

        if (Physics.Raycast(ray, out var hit, maxDistance, plantableLayers, QueryTriggerInteraction.Ignore))
        {
            // 关键：必须命中带 PlantableSurface（本体或父物体）
            surface = hit.collider.GetComponentInParent<PlantableSurface>();
            if (surface == null) return false;

            // 位置吸附与抬高（由 PlantableSurface 决定）
            point = surface.SnapPosition(hit.point);
            normal = hit.normal;
            return true;
        }
        return false;
    }

    // ========== 种植 ==========
    void TryPlant()
    {
        if (_inv == null || _active == null || plantDB == null) return;
        if (_cooldown > 0f) return;

        string id = _active.ActiveId;                   // 用当前手上物品ID（apple/banana）
        if (string.IsNullOrEmpty(id)) return;

        var entry = plantDB.GetByPlantItemId(id);
        if (entry == null || entry.cropPrefab == null) return;

        // ★ 必须在“带 PlantableSurface 的地面”上命中
        if (!RaycastPlantPoint(out Vector3 pos, out Vector3 normal, out PlantableSurface surf)) return;

        // 朝向由 PlantableSurface 决定（是否贴合法线）
        Quaternion rot = surf.alignToNormal
            ? Quaternion.FromToRotation(Vector3.up, normal)
            : Quaternion.identity;

        var cropObj = Instantiate(entry.cropPrefab, pos, rot);
        var crop = cropObj.GetComponent<CropPlant>(); if (!crop) crop = cropObj.AddComponent<CropPlant>();
        crop.Init(entry);

        // ★★★ 这里插入这段 ★★★
        var cp = cropObj.GetComponent<CropPersistence>();
        if (!cp) cp = cropObj.AddComponent<CropPersistence>();
        cp.entryId = id;
        // ★★★★★★★★★★★★★★★★

        // 扣1个，尽量保持当前激活不变
        string keepId = id;
        _inv.RemoveItem(id, 1);
        var ui = FindObjectOfType<InventoryUI>(); if (ui) ui.RefreshAll();
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

    // ========== （暂时不用的）指示环 ==========
    void CreateMarker()
    {
        var go = new GameObject("PlantMarker");
        _markerLR = go.AddComponent<LineRenderer>();
        _markerLR.useWorldSpace = true;
        _markerLR.loop = true;
        _markerLR.widthMultiplier = markerWidth;
        _markerLR.positionCount = markerSegments;

        var sh = Shader.Find("Unlit/Color");
        if (!sh) sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (!sh) sh = Shader.Find("Standard");
        var mat = new Material(sh);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", okColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", okColor);
        _markerLR.material = mat;

        _markerLR.enabled = false;
    }

    void UpdateMarker()
    {
        if (_markerLR == null) return;

        bool hasHit = RaycastPlantPoint(out Vector3 hitPos, out Vector3 hitNormal, out _);
        if (hasHit)
        {
            _cachedPoint = hitPos + hitNormal * markerYOffset;
            _cachedNormal = hitNormal;
            _hasValidPoint = true;

            DrawCircle(_cachedPoint, _cachedNormal, markerRadius);
            if (_markerLR.material.HasProperty("_BaseColor")) _markerLR.material.SetColor("_BaseColor", okColor);
            if (_markerLR.material.HasProperty("_Color")) _markerLR.material.SetColor("_Color", okColor);
            _markerLR.enabled = true;
        }
        else
        {
            _hasValidPoint = false;
            _markerLR.enabled = false;
        }
    }

    void DrawCircle(Vector3 center, Vector3 normal, float radius)
    {
        if (_markerLR == null) return;
        normal = normal.normalized;
        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent.sqrMagnitude < 1e-4f) tangent = Vector3.Cross(normal, Vector3.right);
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(normal, tangent);

        for (int i = 0; i < markerSegments; i++)
        {
            float ang = (i / (float)markerSegments) * Mathf.PI * 2f;
            Vector3 dir = Mathf.Cos(ang) * tangent + Mathf.Sin(ang) * bitangent;
            _markerLR.SetPosition(i, center + dir * radius);
        }
    }
}
