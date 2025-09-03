// Assets/Scripts/Inventory/ActiveItemController.cs
using UnityEngine;

[RequireComponent(typeof(PlayerInventoryHolder))]
public class ActiveItemController : MonoBehaviour
{
    [Header("Refs")]
    public HeldItemDisplay heldDisplay;      // ������ʾ
    PlayerInventoryHolder _inv;

    [Header("State (ReadOnly)")]
    [SerializeField] string _activeId = "";  // ��ǰ������Ʒ id

    public string ActiveId => _activeId;
    public bool HasActive => !string.IsNullOrEmpty(_activeId) && _inv.GetCount(_activeId) > 0;
    public ItemSO ActiveItemSO => _inv.itemDB ? _inv.itemDB.Get(_activeId) : null;

    void Awake()
    {
        _inv = GetComponent<PlayerInventoryHolder>();
        if (!heldDisplay) heldDisplay = GetComponentInChildren<HeldItemDisplay>(true);
        EnsureValidActive();
    }

    void OnEnable()
    {
        // ����/��������ʱҲˢ��һ�Σ����⿪�ֱ���������Ʒ�����ϲ���ʾ
        EnsureValidActive();
    }

    /// �����仯����ã����Ȱѡ��ձ仯���Ǹ� id����Ϊ�������֤��ǰ��������Ч������ĳɱ������һ���л��ġ�
    public void OnInventoryChanged(string preferId = null) => EnsureValidActive(preferId);

    public void EnsureValidActive(string preferId = null)
    {
        if (!string.IsNullOrEmpty(preferId) && _inv.GetCount(preferId) > 0)
            _activeId = preferId;
        else if (string.IsNullOrEmpty(_activeId) || _inv.GetCount(_activeId) <= 0)
            _activeId = FindFirstNonEmptyId(); // ����Ϊ�գ�����ȫ�գ�

        RefreshHeldVisual();
    }

    /// �ֶ��л��������������/���֣�
    public void SetActive(string id)
    {
        _activeId = (!string.IsNullOrEmpty(id) && _inv.GetCount(id) > 0) ? id : "";
        RefreshHeldVisual();
    }

    /// �ؼ��޸���**������ɵ�/����������ʾ**����������Ѹ����ɵ�����ģ��ɾ��
    public void RefreshHeldVisual()
    {
        if (!heldDisplay) return;

        // ������ɵģ�������ʷ������ HELD_ �����壩
        heldDisplay.Clear();
        heldDisplay.PurgeHeldVisuals();

        // Ȼ����ݱ������������Ƿ���ʾһ������ۡ�
        if (HasActive && ActiveItemSO != null)
        {
            // 0 �� = һֱ��ʾ��ֱ������������ 0 ���л�����
            heldDisplay.Show(ActiveItemSO, 0f);
        }
        // else��û�оͲ���ʾ��Clear �Ѿ�����
    }

    string FindFirstNonEmptyId()
    {
        if (_inv.Inventory != null && _inv.Inventory.slots != null)
        {
            foreach (var s in _inv.Inventory.slots)
                if (s != null && !string.IsNullOrEmpty(s.id) && s.count > 0)
                    return s.id;
        }
        return "";
    }
}
