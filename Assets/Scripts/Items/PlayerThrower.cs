// Assets/Scripts/Items/PlayerThrower.cs
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif

/// 丢弃=以“当前激活物品”为准：背包里有才允许丢；扣减1后在地面生成可拾取Trigger；
/// 然后刷新激活物与手上显示（如果数量为0就清空/切到下一个有货的）。
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

    void Awake()
    {
        _inv = GetComponent<PlayerInventoryHolder>();
        _active = GetComponent<ActiveItemController>();
        _pickupCtrl = GetComponent<PlayerPickupController>();

        if (groundMask.value == 0)
        {
            int g = LayerMask.NameToLayer("Ground");
            groundMask = g >= 0 ? (1 << g) : ~0;
        }
    }

    void Update()
    {
        if (DropPressedThisFrame()) TryPlace(true);   // Q 近
        if (ThrowPressedThisFrame()) TryPlace(false);  // R 远（同逻辑，不物理）
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
        // —— 以“当前激活物品”为准 —— 
        if (_active == null || string.IsNullOrEmpty(_active.ActiveId) || !_active.HasActive)
        {
            if (debugLogs) Debug.Log("[Thrower] 没有激活物品或数量为 0，无法丢弃。");
            return;
        }

        string id = _active.ActiveId;
        ItemSO def = _active.ActiveItemSO;

        // 背包检查 & 扣减
        if (_inv.GetCount(id) <= 0) { _active.OnInventoryChanged(); return; }
        if (_inv.RemoveItem(id, 1) <= 0) { _active.OnInventoryChanged(); return; }

        // 计算落点并生成
        float dist = isNear ? nearDistance : farDistance;
        Vector3 approxPoint = ComputeApproxPlacePoint(dist);
        SpawnPickupAt(id, def, approxPoint);

        // 刷新激活物/手上显示（可能切换到下一个有货的）
        _active.OnInventoryChanged(id);

        // 阻断自动捡回
        _pickupCtrl?.BlockFor(pickupBlockSecondsAfterPlace);

        if (debugLogs) Debug.Log($"[Thrower] 丢出 {id}，剩余：{_inv.GetCount(id)}");
    }

    // —— 放置：无物理 + 自动垫高 —— 
    Vector3 ComputeApproxPlacePoint(float horizontalDistance)
    {
        Transform src = Camera.main ? Camera.main.transform : transform;
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
