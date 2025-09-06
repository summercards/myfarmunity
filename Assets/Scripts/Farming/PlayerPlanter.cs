using UnityEngine;

[RequireComponent(typeof(PlayerInventoryHolder))]
[RequireComponent(typeof(ActiveItemController))]
public class PlayerPlanter : MonoBehaviour
{
    [Header("Config")]
    public SeedPlantDataSO plantDB;
    [Tooltip("����ֲ�Ĳ�")] public LayerMask plantableLayers;
    [Tooltip("�����ֲ����")] public float maxDistance = 5f;
    [Tooltip("��ֲ��")] public KeyCode plantKey = KeyCode.F;
    [Tooltip("ʹ�������������")] public bool useCameraRay = true;

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

        string id = _active.ActiveId;                // ֱ���õ�ǰ������ƷID������ "apple"/"banana"��
        if (string.IsNullOrEmpty(id)) return;

        var entry = plantDB.GetByPlantItemId(id);    // �������Ʒ�Ƿ��ֱ����
        if (entry == null || entry.cropPrefab == null) return;

        if (!RaycastPlantPoint(out Vector3 pos, out Vector3 normal)) return;

        Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal);
        pos += normal * entry.spawnYOffset;

        var cropObj = Instantiate(entry.cropPrefab, pos, rot);
        var crop = cropObj.GetComponent<CropPlant>();
        if (!crop) crop = cropObj.AddComponent<CropPlant>();
        crop.Init(entry);

        // �۳� 1 ����ǰ��Ʒ�����ǡ����ӡ������� apple/banana ���壩
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
