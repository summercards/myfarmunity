// Assets/Scripts/Inventory/PlayerInventoryHolder.cs
using UnityEngine;
using System;

/// <summary>
/// 玩家背包持有者 - 单例模式
/// 使用 DontDestroyOnLoad 确保在场景切换时背包数据不丢失
/// </summary>
public class PlayerInventoryHolder : MonoBehaviour
{
    private static PlayerInventoryHolder instance;
    public static PlayerInventoryHolder Instance => instance;

    [Header("Database & Capacity")]
    public ItemDatabaseSO itemDB;
    public int capacity = 24;

    public event Action OnInventoryChanged;

    public Inventory Inventory { get; set; }

    void Awake()
    {
        // 单例模式 + DontDestroyOnLoad
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化背包
            if (Inventory == null) Inventory = new Inventory(Mathf.Max(1, capacity));

            // 设置每种类目的堆叠上限
            Inventory.GetMaxStackForId = (id) =>
            {
                if (itemDB == null) return 99;
                var def = itemDB.Get(id);
                return def ? Mathf.Max(1, def.maxStack) : 99;
            };

            Debug.Log("[PlayerInventoryHolder] 背包系统已初始化（DontDestroyOnLoad）");
        }
        else
        {
            // 已有实例，销毁新创建的
            Debug.Log("[PlayerInventoryHolder] 检测到已存在的背包实例，销毁重复对象");
            Destroy(gameObject);
        }
    }

    public int AddItem(string id, int count = 1)
    {
        int added = Inventory.AddItem(id, count);
        if (added > 0) OnInventoryChanged?.Invoke();
        return added;
    }

    // 提供给外部系统调用，用于在游戏内部通知背包已发生变化（例如从商店购买后）
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
