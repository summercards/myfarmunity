// Assets/Scripts/Inventory/InventoryPersistence.cs
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 背包持久化组件：
/// - 有 SaveManager：由统一存档流程负责保存/恢复
/// - 无 SaveManager：回退到 PlayerPrefs，支持独立场景运行
/// </summary>
[DisallowMultipleComponent]
public class InventoryPersistence : MonoBehaviour, ISaveParticipant, IInventorySaveable
{
    public PlayerInventoryHolder holder;
    [Tooltip("PlayerPrefs 键名；同一玩家/存档位建议唯一")]
    public string saveKey = "save.inventory";

    [Header("Lifecycle")]
    public bool loadOnAwake = true;
    public bool saveOnChange = true;
    public bool saveOnQuit = true;
    public bool useStandalonePrefsWhenNoSaveManager = true;

    [Serializable] class SlotSave { public int index; public string id; public int count; public int durability; }
    [Serializable] class SaveData { public int capacity; public List<SlotSave> slots = new(); }

    public SaveSection Section => SaveSection.Inventory;
    public UnityEngine.Object Owner => this;
    public string ParticipantName => GetType().Name;
    private bool UseCentralSave => RuntimeRefs.SaveService != null;
    private bool _localLoadAttempted;

    void Reset() { holder = GetComponent<PlayerInventoryHolder>(); }

    void Awake()
    {
        if (!holder) holder = GetComponent<PlayerInventoryHolder>();
        if (saveOnChange && holder) holder.OnInventoryChanged += HandleInventoryChanged;
    }

    void Start()
    {
        InitializeLocalPersistenceIfNeeded();
    }

    void OnEnable()
    {
        RuntimeRefs.SaveServiceChanged += HandleSaveServiceChanged;
        RegisterToSaveService();
        InitializeLocalPersistenceIfNeeded();
    }

    void OnDisable()
    {
        RuntimeRefs.SaveServiceChanged -= HandleSaveServiceChanged;
        UnregisterFromSaveService();
    }

    void OnDestroy()
    {
        if (saveOnChange && holder) holder.OnInventoryChanged -= HandleInventoryChanged;
    }

    void OnApplicationQuit()
    {
        if (saveOnQuit && ShouldUseLocalPrefsPersistence()) Save();
    }

    private void HandleInventoryChanged()
    {
        if (ShouldUseLocalPrefsPersistence())
        {
            Save();
        }
    }

    public void Save()
    {
        var data = BuildSaveDataSnapshot();
        if (data == null) return;
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
        ApplySaveDataSnapshot(data);
#if UNITY_EDITOR
        Debug.Log($"[InventoryPersistence] Loaded {(data != null ? data.slots.Count : 0)} stacks from {saveKey}");
#endif
    }

    private SaveData BuildSaveDataSnapshot()
    {
        if (!holder || holder.Inventory == null || holder.Inventory.slots == null) return null;

        var data = new SaveData { capacity = holder.Inventory.slots.Length };
        for (int i = 0; i < holder.Inventory.slots.Length; i++)
        {
            var s = holder.Inventory.slots[i];
            if (s == null || string.IsNullOrEmpty(s.id) || s.count <= 0) continue;
            data.slots.Add(new SlotSave { index = i, id = s.id, count = s.count, durability = s.durability });
        }

        return data;
    }

    private void ApplySaveDataSnapshot(SaveData data)
    {
        if (data == null || !holder) return;

        holder.Inventory = new Inventory(data.capacity);
        holder.ApplyStackRuleResolver();
        foreach (var s in data.slots)
        {
            if (s.index >= 0 && s.index < holder.Inventory.slots.Length)
                holder.Inventory.slots[s.index] = new ItemStack(s.id, s.count, s.durability);
        }
        holder.RaiseInventoryChanged();
    }

    public object CaptureSaveData()
    {
        return BuildSaveDataSnapshot();
    }

    public void RestoreSaveData(string jsonData, GameTimeSystem timeSystem)
    {
        if (string.IsNullOrEmpty(jsonData)) return;
        ApplySaveDataSnapshot(JsonUtility.FromJson<SaveData>(jsonData));
    }

    public object GetSaveData()
    {
        return CaptureSaveData();
    }

    public void LoadSaveData(string jsonData, GameTimeSystem timeSystem)
    {
        RestoreSaveData(jsonData, timeSystem);
    }

    private bool ShouldUseLocalPrefsPersistence()
    {
        return useStandalonePrefsWhenNoSaveManager && !UseCentralSave;
    }

    private void HandleSaveServiceChanged(ISaveService _)
    {
        RegisterToSaveService();
        InitializeLocalPersistenceIfNeeded();
    }

    private void RegisterToSaveService()
    {
        RuntimeRefs.SaveService?.RegisterParticipant(this);
    }

    private void UnregisterFromSaveService()
    {
        RuntimeRefs.SaveService?.UnregisterParticipant(this);
    }

    private void InitializeLocalPersistenceIfNeeded()
    {
        if (_localLoadAttempted)
        {
            return;
        }

        if (loadOnAwake && ShouldUseLocalPrefsPersistence())
        {
            _localLoadAttempted = true;
            Load();
        }
    }

    [ContextMenu("Clear Save")]
    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
        Debug.Log($"[InventoryPersistence] Cleared save key {saveKey}");
    }
}
