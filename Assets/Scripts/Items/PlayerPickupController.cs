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
    public LayerMask pickupMask;   // ֻ��ѡ��Pickup����
    public bool autoPickup = false;

    [Header("UI (Optional)")]
    public PickupHUD hud;

    [Header("Held Visual (Optional)")]
    public HeldItemDisplay heldDisplay;
    public float heldShowSeconds = -1f;

    PlayerInventoryHolder _inv;
    ItemWorld _candidate;

    // NonAlloc ����
    const int kDefaultMaxHits = 16;
    [SerializeField] int maxHits = kDefaultMaxHits;
    Collider[] _hitsCache;

    void Awake()
    {
        _inv = GetComponent<PlayerInventoryHolder>();
        if (!_inv) Debug.LogError("[PlayerPickupController] û���ҵ� PlayerInventoryHolder��");

        if (!heldDisplay) heldDisplay = GetComponentInChildren<HeldItemDisplay>(true);

        if (pickupMask.value == 0)
        {
            int lp = LayerMask.NameToLayer("Pickup");
            if (lp >= 0) pickupMask = (1 << lp);
        }

        if (maxHits < 1) maxHits = kDefaultMaxHits;
        _hitsCache = new Collider[maxHits];
    }

    void Update()
    {
        _candidate = FindNearestItem();

        if (hud)
        {
            if (_candidate)
                hud.Show($"�� E ʰȡ��{_candidate.itemId} x{_candidate.amount}");
            else
                hud.Hide();
        }

        if (!_candidate) return;

        if (autoPickup || InteractPressedThisFrame())
        {
            string pickedId = _candidate.itemId;
            int got = _candidate.TryPickUp(_inv);
            if (got > 0)
            {
                if (hud) hud.Show($"��ã�{pickedId} x{got}");

                if (heldDisplay && _inv != null && _inv.itemDB != null)
                {
                    var def = _inv.itemDB.Get(pickedId);
                    if (def != null)
                    {
                        heldDisplay.Show(def, heldShowSeconds);
                    }
                    else
                    {
                        Debug.LogWarning($"[Pickup] itemDB û�� id='{pickedId}' ����Ŀ���޷���ʾ�ֳ֡�");
                    }
                }
                else
                {
                    // ��Щ��־������ȷ��Ϊʲôû����ʾ
                    if (!heldDisplay) Debug.Log("[Pickup] û�� HeldItemDisplay �������ѡ����");
                    if (_inv == null) Debug.LogWarning("[Pickup] _inv Ϊ�ա�");
                    else if (_inv.itemDB == null) Debug.LogWarning("[Pickup] itemDB δ�󶨣�PlayerInventoryHolder.itemDB����");
                }
            }
        }
    }

    ItemWorld FindNearestItem()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, _hitsCache, pickupMask, QueryTriggerInteraction.Collide);
        float best = float.MaxValue;
        ItemWorld bestItem = null;

        for (int i = 0; i < count; i++)
        {
            var c = _hitsCache[i];
            if (!c) continue;

            var it = c.GetComponentInParent<ItemWorld>();
            if (!it) it = c.GetComponent<ItemWorld>();
            if (!it) continue;

            float d = (it.transform.position - transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d; bestItem = it;
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

    void OnValidate()
    {
        if (maxHits < 1) maxHits = kDefaultMaxHits;
        if (_hitsCache == null || _hitsCache.Length != maxHits)
            _hitsCache = new Collider[maxHits];

        if (pickupMask.value == 0)
        {
            int lp = LayerMask.NameToLayer("Pickup");
            if (lp >= 0) pickupMask = (1 << lp);
        }
    }
}
