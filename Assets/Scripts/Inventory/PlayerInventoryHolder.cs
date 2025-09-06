// Assets/Scripts/Inventory/PlayerInventoryHolder.cs
using UnityEngine;
using System;

public class PlayerInventoryHolder : MonoBehaviour
{
    [Header("Database & Capacity")]
    public ItemDatabaseSO itemDB;
    public int capacity = 24;

    public event Action OnInventoryChanged;

    public Inventory Inventory { get; set; }

    void Awake()
    {
        if (Inventory == null) Inventory = new Inventory(Mathf.Max(1, capacity));
        // 让背包知道每种物品的最大堆叠
        Inventory.GetMaxStackForId = (id) =>
        {
            if (itemDB == null) return 99;
            var def = itemDB.Get(id);
            return def ? Mathf.Max(1, def.maxStack) : 99;
        };
    }

    public int AddItem(string id, int count = 1)
    {
        int added = Inventory.AddItem(id, count);
        if (added > 0) OnInventoryChanged?.Invoke();
        return added;
    }

    // 允许其他组件在合法场合通知“背包变了”（例如读档后）
    public void RaiseInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }


    public int RemoveItem(string id, int count = 1)
    {
        int removed = Inventory.RemoveItem(id, count);
        if (removed > 0) OnInventoryChanged?.Invoke();
        return removed;
    }

    public int GetCount(string id) => Inventory.GetItemCount(id);
}
