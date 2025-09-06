// Assets/Scripts/Inventory/HotbarSelector.cs
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif
using System;
using System.Collections.Generic;

[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public class HotbarSelector : MonoBehaviour
{
    [Header("Refs")]
    public PlayerInventoryHolder inventory;
    public ActiveItemController active;

    [Header("Hotbar")]
    [Min(1)] public int hotbarSize = 8;
    public bool enableMouseWheel = true;

    [Header("UI (optional)")]
    public int currentIndex = 0;                  // 0-based
    public event Action<int, string> OnHotbarChanged;

    List<string> _distinct = new();

    void Reset()
    {
        inventory = GetComponent<PlayerInventoryHolder>();
        active = GetComponent<ActiveItemController>();
    }

    void Awake()
    {
        if (!inventory) inventory = GetComponent<PlayerInventoryHolder>();
        if (!active) active = GetComponent<ActiveItemController>();
        RebuildDistinct();
        if (_distinct.Count > 0)
        {
            currentIndex = Mathf.Clamp(currentIndex, 0, _distinct.Count - 1);
            ApplyIndex();
        }
    }

    void OnEnable()
    {
        if (inventory) inventory.OnInventoryChanged += HandleInventoryChanged;
    }
    void OnDisable()
    {
        if (inventory) inventory.OnInventoryChanged -= HandleInventoryChanged;
    }

    void HandleInventoryChanged()
    {
        var prevId = GetCurrentId();
        RebuildDistinct();
        if (!string.IsNullOrEmpty(prevId) && inventory.GetCount(prevId) > 0)
            currentIndex = Mathf.Clamp(_distinct.IndexOf(prevId), 0, Mathf.Max(0, _distinct.Count - 1));
        else
            currentIndex = 0;
        ApplyIndex();
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
        if (enableMouseWheel && Mouse.current != null)
        {
            var scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                if (scroll > 0) Step(-1);
                else Step(+1);
            }
        }
        if (Keyboard.current != null)
        {
            for (int n = 1; n <= Mathf.Min(hotbarSize, 9); n++)
            {
                var key = (Key)((int)Key.Digit1 + (n - 1));
                if (Keyboard.current[key].wasPressedThisFrame) { SetIndex(n - 1); break; }
            }
        }
#else
        if (enableMouseWheel)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f) Step(scroll > 0 ? -1 : +1);
        }
        for (int n = 1; n <= Mathf.Min(hotbarSize, 9); n++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + (n - 1))) { SetIndex(n - 1); break; }
#endif
    }

    void Step(int delta)
    {
        if (_distinct.Count == 0) return;
        currentIndex = (currentIndex + delta) % _distinct.Count;
        if (currentIndex < 0) currentIndex += _distinct.Count;
        ApplyIndex();
    }

    void SetIndex(int index)
    {
        if (_distinct.Count == 0) return;
        currentIndex = Mathf.Clamp(index, 0, _distinct.Count - 1);
        ApplyIndex();
    }

    void ApplyIndex()
    {
        string id = GetCurrentId();
        if (active && !string.IsNullOrEmpty(id))
            active.SetActive(id, prefer: true);
        OnHotbarChanged?.Invoke(currentIndex, id);
    }

    string GetCurrentId() => (_distinct.Count > 0 && currentIndex >= 0 && currentIndex < _distinct.Count) ? _distinct[currentIndex] : "";

    void RebuildDistinct()
    {
        _distinct.Clear();
        if (inventory == null || inventory.Inventory == null || inventory.Inventory.slots == null) return;

        foreach (var s in inventory.Inventory.slots)
        {
            if (s == null || string.IsNullOrEmpty(s.id) || s.count <= 0) continue;
            if (!_distinct.Contains(s.id)) _distinct.Add(s.id);
            if (_distinct.Count >= hotbarSize) break;
        }
    }
}
