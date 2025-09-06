// Assets/Scripts/Inventory/ActiveItemController.cs
using UnityEngine;
using System;

[RequireComponent(typeof(PlayerInventoryHolder))]
[DisallowMultipleComponent]
public class ActiveItemController : MonoBehaviour
{
    [Header("Refs")]
    public HeldItemDisplay heldDisplay;
    PlayerInventoryHolder _inv;

    [Header("State (ReadOnly)")]
    [SerializeField] string _activeId = "";

    public string ActiveId => _activeId;
    public bool HasActive => !string.IsNullOrEmpty(_activeId) && _inv.GetCount(_activeId) > 0;
    public ItemSO ActiveItemSO => _inv && _inv.itemDB ? _inv.itemDB.Get(_activeId) : null;

    public event Action<string> OnActiveChanged;

    void Reset() { heldDisplay = GetComponent<HeldItemDisplay>(); }
    void Awake() { _inv = GetComponent<PlayerInventoryHolder>(); }

    void OnEnable()
    {
        if (_inv) _inv.OnInventoryChanged += HandleInventoryChanged;
        RefreshVisual(fromIdChange: false);
    }
    void OnDisable()
    {
        if (_inv) _inv.OnInventoryChanged -= HandleInventoryChanged;
    }

    void HandleInventoryChanged()
    {
        if (!HasActive)
        {
            var first = FindFirstNonEmptyId();
            InternalSetActive(first, prefer: false);
        }
        else
        {
            RefreshVisual(fromIdChange: false);
        }
    }

    public void SetActive(string preferId, bool prefer = true)
    {
        if (string.IsNullOrEmpty(preferId)) return;
        if (_inv.GetCount(preferId) <= 0) return;
        InternalSetActive(preferId, prefer: true);
    }

    // 兼容旧代码：无参版本，等价于“我也不确定哪个ID变了”
    public void OnInventoryChanged()
    {
        OnInventoryChanged(string.Empty);
    }

    public void OnInventoryChanged(string affectedId)
    {
        if (string.IsNullOrEmpty(affectedId)) { HandleInventoryChanged(); return; }

        if (_activeId == affectedId)
        {
            if (_inv.GetCount(_activeId) <= 0)
            {
                var next = FindFirstNonEmptyId();
                InternalSetActive(next, prefer: false);
            }
            else
            {
                RefreshVisual(fromIdChange: false);
            }
        }
        else
        {
            if (!HasActive && _inv.GetCount(affectedId) > 0)
                InternalSetActive(affectedId, prefer: false);
        }
    }

    void InternalSetActive(string id, bool prefer)
    {
        id = id ?? "";
        if (_activeId == id) { RefreshVisual(fromIdChange: false); return; }
        _activeId = id;
        RefreshVisual(fromIdChange: true);
        OnActiveChanged?.Invoke(_activeId);
    }

    void RefreshVisual(bool fromIdChange)
    {
        if (!heldDisplay) return;
        if (HasActive) heldDisplay.Show(ActiveItemSO, seconds: 0f);
        else heldDisplay.Clear();
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
