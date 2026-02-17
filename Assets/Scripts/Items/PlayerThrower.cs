// Assets/Scripts/Items/PlayerThrower.cs
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif

/// ����=�ԡ���ǰ������Ʒ��Ϊ׼���������в����������ۼ�1���ڵ������ɿ�ʰȡTrigger��
/// Ȼ��ˢ�¼�������������ʾ���������Ϊ0�����/�е���һ���л��ģ���
[RequireComponent(typeof(PlayerInventoryHolder))]
public class PlayerThrower : MonoBehaviour
{
    [Header("Place Distances")]
    public float nearDistance = 0.9f;   // Q
    public float farDistance = 2.5f;   // R

    [Header("Ground Ray")]
    public float rayStartHeight = 1.2f;
    public float placeUpOffset = 0.02f;
    public LayerMask groundMask = ~0;

    [Header("Pickup Shape/Layer")]
    public float triggerRadius = 0.22f;
    public string pickupLayerName = "Pickup";
    public float visualScale = 1.0f;

    [Header("Anti Auto-Pickup")]
    public float pickupBlockSecondsAfterPlace = 0.6f;

    [Header("Debug")]
    public bool debugLogs = false;

    PlayerInventoryHolder _inv;
    ActiveItemController _active;
    PlayerPickupController _pickupCtrl;
    Camera _cachedCamera;

    void Awake()
    {
        _inv = GetComponent<PlayerInventoryHolder>();
        _active = GetComponent<ActiveItemController>();
        _pickupCtrl = GetComponent<PlayerPickupController>();
        _cachedCamera = Camera.main;

        if (groundMask.value == 0)
        {
            int g = LayerMask.NameToLayer("Ground");
            groundMask = g >= 0 ? (1 << g) : ~0;
        }
    }

    void Update()
    {
        if (DropPressedThisFrame()) TryPlace(true);   // Q ��
        if (ThrowPressedThisFrame()) TryPlace(false);  // R Զ��ͬ�߼�����������
    }

    bool DropPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
        return (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.JoystickButton4);
#endif
    }
    bool ThrowPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
        return (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.JoystickButton5);
#endif
    }

    void TryPlace(bool isNear)
    {
        // ���� �ԡ���ǰ������Ʒ��Ϊ׼ ���� 
        if (_active == null || string.IsNullOrEmpty(_active.ActiveId) || !_active.HasActive)
        {
            if (debugLogs) Debug.Log("[Thrower] û�м�����Ʒ������Ϊ 0���޷�������");
            return;
        }

        string id = _active.ActiveId;
        ItemSO def = _active.ActiveItemSO;

        // ������� & �ۼ�
        if (_inv.GetCount(id) <= 0) { _active.OnInventoryChanged(); return; }
        if (_inv.RemoveItem(id, 1) <= 0) { _active.OnInventoryChanged(); return; }

        // ������㲢����
        float dist = isNear ? nearDistance : farDistance;
        Vector3 approxPoint = ComputeApproxPlacePoint(dist);
        SpawnPickupAt(id, def, approxPoint);

        // ˢ�¼�����/������ʾ�������л�����һ���л��ģ�
        _active.OnInventoryChanged(id);

        // ����Զ����
        _pickupCtrl?.BlockFor(pickupBlockSecondsAfterPlace);

        if (debugLogs) Debug.Log($"[Thrower] ���� {id}��ʣ�ࣺ{_inv.GetCount(id)}");
    }

    // ���� ���ã������� + �Զ���� ���� 
    Vector3 ComputeApproxPlacePoint(float horizontalDistance)
    {
        Transform src = _cachedCamera ? _cachedCamera.transform : transform;
        Vector3 fwd = Vector3.ProjectOnPlane(src.forward, Vector3.up).normalized;
        if (fwd.sqrMagnitude < 1e-4f) fwd = transform.forward;

        Vector3 basePos = transform.position + fwd * horizontalDistance;
        Vector3 start = basePos + Vector3.up * rayStartHeight;

        if (Physics.Raycast(start, Vector3.down, out var hit, rayStartHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point;

        Vector3 feetStart = transform.position + Vector3.up * rayStartHeight;
        if (Physics.Raycast(feetStart, Vector3.down, out hit, rayStartHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point;

        return transform.position;
    }

    void SpawnPickupAt(string id, ItemSO def, Vector3 approxGroundPoint)
    {
        GameObject root = new GameObject($"Item_{id}");
        root.transform.position = approxGroundPoint;

        int lp = LayerMask.NameToLayer(pickupLayerName);
        if (lp >= 0) root.layer = lp;

        var col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = triggerRadius;

        var iw = root.AddComponent<ItemWorld>();
        iw.itemId = id;
        iw.amount = 1;

        Transform vis = null;
        if (def && def.heldPrefab)
        {
            var go = Instantiate(def.heldPrefab, root.transform, false);
            go.name = $"HELD_{id}_VIS";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = def.heldLocalScale == Vector3.zero
                ? Vector3.one * visualScale
                : def.heldLocalScale * visualScale;
            if (!go.activeSelf) go.SetActive(true);
            vis = go.transform;
        }

        float groundY = ProbeGroundY(root.transform.position);
        float lift = ComputeBottomToPivot(vis);
        if (lift <= 0f) lift = triggerRadius;
        float targetY = groundY + lift + placeUpOffset;

        var p = root.transform.position;
        root.transform.position = new Vector3(p.x, targetY, p.z);
    }

    float ProbeGroundY(Vector3 around)
    {
        Vector3 start = around + Vector3.up * rayStartHeight;
        if (Physics.Raycast(start, Vector3.down, out var hit, rayStartHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point.y;

        start = around - Vector3.up * rayStartHeight;
        if (Physics.Raycast(start, Vector3.up, out hit, rayStartHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point.y;

        return around.y;
    }

    float ComputeBottomToPivot(Transform visualRoot)
    {
        if (!visualRoot) return 0f;
        var rends = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (rends == null || rends.Length == 0) return 0f;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        float bottom = b.min.y;
        float pivotY = visualRoot.parent ? visualRoot.parent.position.y : visualRoot.position.y;
        float dist = pivotY - bottom;
        return dist > 0f ? dist : 0f;
    }
}
