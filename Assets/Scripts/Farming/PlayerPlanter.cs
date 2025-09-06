using UnityEngine;

[RequireComponent(typeof(PlayerInventoryHolder))]
[RequireComponent(typeof(ActiveItemController))]
public class PlayerPlanter : MonoBehaviour
{
    [Header("Config")]
    public SeedPlantDataSO plantDB;
    [Tooltip("可种植的层")] public LayerMask plantableLayers;
    [Tooltip("最大种植距离")] public float maxDistance = 5f;
    [Tooltip("种植键")] public KeyCode plantKey = KeyCode.F;
    [Tooltip("使用相机中心射线")] public bool useCameraRay = true;

    PlayerInventoryHolder _inv;
    ActiveItemController _active;
    float _cooldown = 0f;

    void Awake()
    {
        _inv = GetComponent<PlayerInventoryHolder>();
        _active = GetComponent<ActiveItemController>();
    }

    void Update()
    {
        if (_cooldown > 0f) _cooldown -= Time.deltaTime;
        if (Input.GetKeyDown(plantKey)) TryPlant();
    }

    void TryPlant()
    {
        if (_inv == null || _active == null || plantDB == null) return;
        if (_cooldown > 0f) return;

        string id = _active.ActiveId;                // 直接用当前手上物品ID（比如 "apple"/"banana"）
        if (string.IsNullOrEmpty(id)) return;

        var entry = plantDB.GetByPlantItemId(id);    // 查表：该物品是否可直接种
        if (entry == null || entry.cropPrefab == null) return;

        if (!RaycastPlantPoint(out Vector3 pos, out Vector3 normal)) return;

        Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal);
        pos += normal * entry.spawnYOffset;

        var cropObj = Instantiate(entry.cropPrefab, pos, rot);
        var crop = cropObj.GetComponent<CropPlant>();
        if (!crop) crop = cropObj.AddComponent<CropPlant>();
        crop.Init(entry);

        // 扣除 1 个当前物品（不是“种子”，就是 apple/banana 本体）
        _inv.RemoveItem(id, 1);

        _cooldown = Mathf.Max(0.05f, entry.plantCooldown);
    }

    bool RaycastPlantPoint(out Vector3 point, out Vector3 normal)
    {
        Ray ray;
        if (useCameraRay && Camera.main)
            ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
        else
            ray = new Ray(transform.position + Vector3.up * 1.2f, transform.forward);

        if (Physics.Raycast(ray, out var hit, maxDistance, plantableLayers, QueryTriggerInteraction.Ignore))
        {
            point = hit.point; normal = hit.normal; return true;
        }
        point = Vector3.zero; normal = Vector3.up; return false;
    }
}
