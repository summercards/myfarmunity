// Assets/Scripts/Inventory/ActiveItemController.cs
using UnityEngine;

[RequireComponent(typeof(PlayerInventoryHolder))]
public class ActiveItemController : MonoBehaviour
{
    [Header("Refs")]
    public HeldItemDisplay heldDisplay;      // 手上显示
    PlayerInventoryHolder _inv;

    [Header("State (ReadOnly)")]
    [SerializeField] string _activeId = "";  // 当前激活物品 id

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
        // 进场/重新启用时也刷新一次，避免开局背包已有物品但手上不显示
        EnsureValidActive();
    }

    /// 背包变化后调用：优先把“刚变化的那个 id”设为激活；否则保证当前激活仍有效；否则改成背包里第一个有货的。
    public void OnInventoryChanged(string preferId = null) => EnsureValidActive(preferId);

    public void EnsureValidActive(string preferId = null)
    {
        if (!string.IsNullOrEmpty(preferId) && _inv.GetCount(preferId) > 0)
            _activeId = preferId;
        else if (string.IsNullOrEmpty(_activeId) || _inv.GetCount(_activeId) <= 0)
            _activeId = FindFirstNonEmptyId(); // 可能为空（背包全空）

        RefreshHeldVisual();
    }

    /// 手动切换（将来做快捷栏/滚轮）
    public void SetActive(string id)
    {
        _activeId = (!string.IsNullOrEmpty(id) && _inv.GetCount(id) > 0) ? id : "";
        RefreshHeldVisual();
    }

    /// 关键修复：**先清理旧的/残留，再显示**。这样不会把刚生成的手上模型删掉
    public void RefreshHeldVisual()
    {
        if (!heldDisplay) return;

        // 先清理旧的（包括历史残留的 HELD_ 子物体）
        heldDisplay.Clear();
        heldDisplay.PurgeHeldVisuals();

        // 然后根据背包数量决定是否显示一个“外观”
        if (HasActive && ActiveItemSO != null)
        {
            // 0 秒 = 一直显示，直到背包数量变 0 或切换激活
            heldDisplay.Show(ActiveItemSO, 0f);
        }
        // else：没有就不显示（Clear 已经处理）
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
