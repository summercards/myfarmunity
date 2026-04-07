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
    private bool _missingInventoryWarned;

    public string ActiveId => _activeId;
    public bool HasActive => _inv != null && !string.IsNullOrEmpty(_activeId) && _inv.GetCount(_activeId) > 0;
    public ItemSO ActiveItemSO => _inv && _inv.itemDB ? _inv.itemDB.Get(_activeId) : null;

    public event Action<string> OnActiveChanged;

    void Reset() { heldDisplay = GetComponent<HeldItemDisplay>(); }
    void Awake()
    {
        EnsureInventoryRef();
        RuntimeRefs.RegisterActiveItemController(this);
    }

    void OnEnable()
    {
        EnsureInventoryRef();
        if (_inv) _inv.OnInventoryChanged += HandleInventoryChanged;
        RefreshVisual(fromIdChange: false);
    }
    void OnDisable()
    {
        if (_inv) _inv.OnInventoryChanged -= HandleInventoryChanged;
    }

    void OnDestroy()
    {
        RuntimeRefs.UnregisterActiveItemController(this);
    }

    void HandleInventoryChanged()
    {
        if (!EnsureInventoryRef())
        {
            return;
        }

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
        if (!EnsureInventoryRef()) return;
        if (_inv.GetCount(preferId) <= 0) return;
        InternalSetActive(preferId, prefer: true);
    }

    // ¼æÈÝ¾É´úÂë£ºÎÞ²Î°æ±¾£¬µÈ¼ÛÓÚ¡°ÎÒÒ²²»È·¶¨ÄÄ¸öID±äÁË¡±
    public void OnInventoryChanged()
    {
        OnInventoryChanged(string.Empty);
    }

    public void OnInventoryChanged(string affectedId)
    {
        if (!EnsureInventoryRef())
        {
            return;
        }

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
        if (_inv != null && _inv.Inventory != null && _inv.Inventory.slots != null)
        {
            foreach (var s in _inv.Inventory.slots)
                if (s != null && !string.IsNullOrEmpty(s.id) && s.count > 0)
                    return s.id;
        }
        return "";
    }

    private bool EnsureInventoryRef()
    {
        if (_inv != null)
        {
            return true;
        }

        _inv = GetComponent<PlayerInventoryHolder>();
        if (_inv == null && !_missingInventoryWarned)
        {
            Debug.LogWarning("[ActiveItemController] Missing PlayerInventoryHolder, skip active-item update.");
            _missingInventoryWarned = true;
        }

        return _inv != null;
    }
}
