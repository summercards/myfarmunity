// Assets/Scripts/Items/PlayerPickupController.cs
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(PlayerInventoryHolder))]
public class PlayerPickupController : MonoBehaviour
{
    [Header("Detect")]
    public float searchRadius = 2.0f;
    public LayerMask pickupMask;
    public bool autoPickup = false;

    [Header("UI (Optional)")]
    public PickupHUD hud;

    [Header("Block")]
    public float defaultBlockSeconds = 0.6f;

    PlayerInventoryHolder _inv;
    ActiveItemController _active;
    ItemWorld _candidate;
    float _blockTimer = 0f;

    void Awake()
    {
        _inv = GetComponent<PlayerInventoryHolder>();
        _active = GetComponent<ActiveItemController>();
        if (pickupMask.value == 0)
        {
            int lp = LayerMask.NameToLayer("Pickup");
            if (lp >= 0) pickupMask = (1 << lp);
        }
    }

    void Update()
    {
        if (_blockTimer > 0f)
        {
            _blockTimer -= Time.deltaTime;
            if (hud) hud.Hide();
            return;
        }

        _candidate = FindNearestItem();

        if (hud)
        {
            if (_candidate)
                hud.Show($"按 E 拾取：{_candidate.itemId} x{_candidate.amount}");
            else
                hud.Hide();
        }

        if (!_candidate) return;

        if (autoPickup || InteractPressedThisFrame())
        {
            // —— 拾取：只改背包 —— 
            int got = _candidate.TryPickUp(_inv);
            if (got > 0)
            {
                // 通知激活物刷新（优先让刚拾到的类型成为激活）
                _active?.OnInventoryChanged(_candidate.itemId);
                if (hud) hud.Show($"获得：{_candidate.itemId} x{got}");
            }
        }
    }

    ItemWorld FindNearestItem()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, pickupMask, QueryTriggerInteraction.Collide);
        float best = float.MaxValue;
        ItemWorld bestItem = null;

        for (int i = 0; i < hits.Length; i++)
        {
            var it = hits[i].GetComponentInParent<ItemWorld>() ?? hits[i].GetComponent<ItemWorld>();
            if (!it) continue;

            float d = (it.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; bestItem = it; }
        }
        return bestItem;
    }

    bool InteractPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
        bool key = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool pad = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        return key || pad;
#else
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton0);
#endif
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.25f);
        Gizmos.DrawSphere(transform.position, searchRadius);
    }

    /// 外部调用：阻断拾取若干秒（丢出后用）
    public void BlockFor(float seconds)
    {
        _blockTimer = Mathf.Max(_blockTimer, seconds > 0 ? seconds : defaultBlockSeconds);
    }
}
