// Assets/Scripts/Items/PlayerPickupController.cs
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif

/// <summary>
/// - ÿ֡�ڰ뾶��Ѱ������� ItemWorld
/// - ��ʾ��ʾ����ѡ��
/// - �� E / Gamepad South��A/����ʰȡ�������Զ�ʰȡ
/// </summary>
[RequireComponent(typeof(PlayerInventoryHolder))]
public class PlayerPickupController : MonoBehaviour
{
    [Header("Detect")]
    public float searchRadius = 2.0f;
    public LayerMask pickupMask;           // ֻ��ѡ��Pickup����
    public bool autoPickup = false;        // �����������ͼ�

    [Header("UI (Optional)")]
    public PickupHUD hud;                  // ����Ҳ����

    PlayerInventoryHolder _inv;
    ItemWorld _candidate;                  // ����Ŀ�ʰȡ��

    void Awake()
    {
        _inv = GetComponent<PlayerInventoryHolder>();
        if (pickupMask.value == 0)
        {
            // ������������ã������Զ�ѡ����Ϊ Pickup �Ĳ�
            int lp = LayerMask.NameToLayer("Pickup");
            if (lp >= 0) pickupMask = (1 << lp);
        }
    }

    void Update()
    {
        // 1) ������� ItemWorld
        _candidate = FindNearestItem();

        // 2) UI ��ʾ
        if (hud)
        {
            if (_candidate)
                hud.Show($"�� E ʰȡ��{_candidate.itemId} x{_candidate.amount}");
            else
                hud.Hide();
        }

        if (!_candidate) return;

        // 3) �Զ�ʰȡ or ����ʰȡ
        if (autoPickup || InteractPressedThisFrame())
        {
            int got = _candidate.TryPickUp(_inv);
            if (got > 0 && hud)
                hud.Show($"��ã�{_candidate.itemId} x{got}");
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
