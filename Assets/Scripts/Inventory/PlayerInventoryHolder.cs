// Assets/Scripts/Inventory/PlayerInventoryHolder.cs
using UnityEngine;
using System;

public class PlayerInventoryHolder : MonoBehaviour
{
    [Header("Database & Capacity")]
    public ItemDatabaseSO itemDB;
    public int capacity = 24;

    public event Action OnInventoryChanged;

    public Inventory Inventory { get; private set; }

    void Awake()
    {
        if (!itemDB) Debug.LogWarning("[PlayerInventoryHolder] ItemDatabaseSO is not assigned.");
        Inventory = new Inventory(capacity, itemDB);
    }

    /// <summary> 往背包加物品，返回真正加入的数量 </summary>
    public int AddItem(string id, int count = 1)
    {
        int added = Inventory.AddItem(id, count);
        if (added > 0) OnInventoryChanged?.Invoke();
        return added;
    }

    public int RemoveItem(string id, int count = 1)
    {
        int removed = Inventory.RemoveItem(id, count);
        if (removed > 0) OnInventoryChanged?.Invoke();
        return removed;
    }

    public int GetCount(string id) => Inventory.GetItemCount(id);
}
