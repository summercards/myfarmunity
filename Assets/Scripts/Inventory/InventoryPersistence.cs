// Assets/Scripts/Inventory/InventoryPersistence.cs
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 轻量存档：把 Inventory 的每个非空槽位(index,id,count,durability) 存到 PlayerPrefs
/// - 仅用于演示/单机，本地开发；上线请替换为更可靠的存档方案。
/// </summary>
[DisallowMultipleComponent]
public class InventoryPersistence : MonoBehaviour
{
    public PlayerInventoryHolder holder;
    [Tooltip("PlayerPrefs 键名；同一玩家/存档位要唯一")]
    public string saveKey = "save.inventory";

    [Header("Lifecycle")]
    public bool loadOnAwake = true;
    public bool saveOnChange = true;
    public bool saveOnQuit = true;

    [Serializable] class SlotSave { public int index; public string id; public int count; public int durability; }
    [Serializable] class SaveData { public int capacity; public List<SlotSave> slots = new(); }

    void Reset() { holder = GetComponent<PlayerInventoryHolder>(); }

    void Awake()
    {
        if (!holder) holder = GetComponent<PlayerInventoryHolder>();
        if (loadOnAwake) Load();
        if (saveOnChange && holder) holder.OnInventoryChanged += Save;
    }

    void OnDestroy()
    {
        if (saveOnChange && holder) holder.OnInventoryChanged -= Save;
    }

    void OnApplicationQuit()
    {
        if (saveOnQuit) Save();
    }

    public void Save()
    {
        if (!holder || holder.Inventory == null || holder.Inventory.slots == null) return;

        var data = new SaveData { capacity = holder.Inventory.slots.Length };
        for (int i = 0; i < holder.Inventory.slots.Length; i++)
        {
            var s = holder.Inventory.slots[i];
            if (s == null || string.IsNullOrEmpty(s.id) || s.count <= 0) continue;
            data.slots.Add(new SlotSave { index = i, id = s.id, count = s.count, durability = s.durability });
        }
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
#if UNITY_EDITOR
        Debug.Log($"[InventoryPersistence] Saved {data.slots.Count} stacks to {saveKey}");
#endif
    }

    public void Load()
    {
        if (!holder) return;
        string json = PlayerPrefs.GetString(saveKey, "");
        if (string.IsNullOrEmpty(json)) return;

        var data = JsonUtility.FromJson<SaveData>(json);
        if (data == null) return;

        // 重建背包
        holder.Inventory = new Inventory(data.capacity);
        foreach (var s in data.slots)
        {
            if (s.index >= 0 && s.index < holder.Inventory.slots.Length)
                holder.Inventory.slots[s.index] = new ItemStack(s.id, s.count, s.durability);
        }
        holder.RaiseInventoryChanged();
#if UNITY_EDITOR
        Debug.Log($"[InventoryPersistence] Loaded {data.slots.Count} stacks from {saveKey}");
#endif
    }

    [ContextMenu("Clear Save")]
    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
        Debug.Log($"[InventoryPersistence] Cleared save key {saveKey}");
    }
}
