// Assets/Scripts/Items/PlayerPickupController.cs
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif

/// <summary>
/// - 每帧在半径内寻找最近的 ItemWorld
/// - 显示提示（可选）
/// - 按 E / Gamepad South（A/×）拾取，或开启自动拾取
/// </summary>
[RequireComponent(typeof(PlayerInventoryHolder))]
public class PlayerPickupController : MonoBehaviour
{
    [Header("Detect")]
    public float searchRadius = 2.0f;
    public LayerMask pickupMask;           // 只勾选“Pickup”层
    public bool autoPickup = false;        // 开启后贴近就捡

    [Header("UI (Optional)")]
    public PickupHUD hud;                  // 不挂也能用

    PlayerInventoryHolder _inv;
    ItemWorld _candidate;                  // 最近的可拾取物

    void Awake()
    {
        _inv = GetComponent<PlayerInventoryHolder>();
        if (pickupMask.value == 0)
        {
            // 如果你忘了设置，尝试自动选择名为 Pickup 的层
            int lp = LayerMask.NameToLayer("Pickup");
            if (lp >= 0) pickupMask = (1 << lp);
        }
    }

    void Update()
    {
        // 1) 找最近的 ItemWorld
        _candidate = FindNearestItem();

        // 2) UI 提示
        if (hud)
        {
            if (_candidate)
                hud.Show($"按 E 拾取：{_candidate.itemId} x{_candidate.amount}");
            else
                hud.Hide();
        }

        if (!_candidate) return;

        // 3) 自动拾取 or 按键拾取
        if (autoPickup || InteractPressedThisFrame())
        {
            int got = _candidate.TryPickUp(_inv);
            if (got > 0 && hud)
                hud.Show($"获得：{_candidate.itemId} x{got}");
        }
    }

    ItemWorld FindNearestItem()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, pickupMask, QueryTriggerInteraction.Collide);
        float best = float.MaxValue;
        ItemWorld bestItem = null;

        for (int i = 0; i < hits.Length; i++)
        {
            var it = hits[i].GetComponentInParent<ItemWorld>();
            if (!it) it = hits[i].GetComponent<ItemWorld>();
            if (!it) continue;

            float d = (it.transform.position - transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestItem = it;
            }
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
}
