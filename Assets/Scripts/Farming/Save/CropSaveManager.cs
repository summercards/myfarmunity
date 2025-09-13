using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CropSaveManager : MonoBehaviour
{
    [Serializable]
    private class CropSaveRecord
    {
        public string entryId;
        public Vector3 position;
        public Quaternion rotation;
        public int stageIndex;
        public float stageTimer;
        public bool mature;
        public float produceTimer;
        public int storedYield;
    }

    [Serializable]
    private class CropSaveFile
    {
        public string savedUtc;
        public List<CropSaveRecord> crops = new List<CropSaveRecord>();
    }

    public static CropSaveManager Instance { get; private set; }

    [Header("Data")]
    public SeedPlantDataSO plantDB;
    public string fileName = "crops.json";

    [Header("Auto Save / Load")]
    public bool autoLoadOnStart = true;
    public bool saveOnPauseOrFocusLoss = true;

    [Tooltip("开启自动保存（仅在检测到变更时落盘）")]
    public bool autoSaveEnabled = true;
    [Tooltip("自动保存间隔（秒）")]
    public float autoSaveInterval = 10f;

    private readonly List<CropPersistence> _tracked = new List<CropPersistence>();
    private bool _dirty = false;
    private double _offlineSeconds = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SavePaths.EnsureDir();
        if (autoLoadOnStart) Load();
        if (autoSaveEnabled) InvokeRepeating(nameof(AutoSaveTick), autoSaveInterval, autoSaveInterval);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        CancelInvoke(nameof(AutoSaveTick));
    }

    void AutoSaveTick()
    {
        if (!autoSaveEnabled) return;
        if (_dirty) Save();
    }

    public void Register(CropPersistence c)
    {
        if (c != null && !_tracked.Contains(c))
        {
            _tracked.Add(c);
            _dirty = true;
        }
    }

    public void Unregister(CropPersistence c)
    {
        if (c != null && _tracked.Remove(c))
            _dirty = true;
    }

    string PathForWrite() => SavePaths.FileInSlot(fileName);
    string PathForRead() => SavePaths.FindExistingOrCurrent(fileName);

    [ContextMenu("Save Now")]
    public void Save()
    {
        var file = new CropSaveFile { savedUtc = System.DateTime.UtcNow.ToString("o") };

        // 登记队列 + 场景所有（含未激活）合并去重
        var all = new List<CropPersistence>(_tracked);
        foreach (var extra in GameObject.FindObjectsOfType<CropPersistence>(true))
            if (extra != null && !all.Contains(extra)) all.Add(extra);

        foreach (var c in all)
        {
            if (c == null) continue;
            var plant = c.GetComponent<CropPlant>();
            if (plant == null) continue;

            // ★ 兜底 entryId：优先持久化组件，其次向作物要 plantItemId
            string entryId = !string.IsNullOrEmpty(c.entryId)
                ? c.entryId
                : (plant != null ? plant.GetPlantItemIdForSave() : null);

            if (string.IsNullOrEmpty(entryId)) continue; // 仍然拿不到就跳过

            var st = plant.GetSaveState();
            file.crops.Add(new CropSaveRecord
            {
                entryId = entryId,
                position = c.transform.position,
                rotation = c.transform.rotation,
                stageIndex = st.stageIndex,
                stageTimer = st.stageTimer,
                mature = st.mature,
                produceTimer = st.produceTimer,
                storedYield = st.storedYield
            });
        }

        var path = PathForWrite();
        File.WriteAllText(path, JsonUtility.ToJson(file, true));
        _dirty = false;

#if UNITY_EDITOR
        Debug.Log($"[CropSave] Saved {file.crops.Count} crops -> {path}");
#endif
    }

    [ContextMenu("Load Now")]
    public void Load()
    {
        var path = PathForRead();
        if (!File.Exists(path))
        {
#if UNITY_EDITOR
            Debug.Log("[CropSave] No save file yet.");
#endif
            return;
        }

        var file = JsonUtility.FromJson<CropSaveFile>(File.ReadAllText(path));
        if (file == null) return;

        _offlineSeconds = 0;
        if (!string.IsNullOrEmpty(file.savedUtc)
            && DateTime.TryParse(file.savedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var saved))
        {
            _offlineSeconds = (DateTime.UtcNow - saved.ToUniversalTime()).TotalSeconds;
        }

        // 只清理我们登记过的（不会影响建造、背包、金币等）
        for (int i = _tracked.Count - 1; i >= 0; i--)
        {
            var c = _tracked[i];
            if (c != null) GameObject.Destroy(c.gameObject);
        }
        _tracked.Clear();

        int loaded = 0;
        if (file.crops != null)
        {
            foreach (var r in file.crops)
            {
                var entry = plantDB ? plantDB.GetByPlantItemId(r.entryId) : null;
                if (entry == null || entry.cropPrefab == null) continue;

                var go = Instantiate(entry.cropPrefab, r.position, r.rotation);
                var crop = go.GetComponent<CropPlant>(); if (!crop) crop = go.AddComponent<CropPlant>();
                crop.Init(entry);

                // ★ 读档后补挂持久化组件，写回 entryId，保证后续保存不再丢
                var cp = go.GetComponent<CropPersistence>() ?? go.AddComponent<CropPersistence>();
                cp.entryId = r.entryId;

                crop.ApplySaveState(new CropPlant.GrowthState
                {
                    stageIndex = r.stageIndex,
                    stageTimer = r.stageTimer,
                    mature = r.mature,
                    produceTimer = r.produceTimer,
                    storedYield = r.storedYield
                });

                if (_offlineSeconds > 0) crop.AdvanceBy((float)_offlineSeconds);
                loaded++;
            }
        }
#if UNITY_EDITOR
        Debug.Log($"[CropSave] Loaded {loaded} crops. Offline +{_offlineSeconds:F1}s from {path}");
#endif

        _dirty = false;
    }

    void OnApplicationQuit() => Save();
    void OnApplicationPause(bool pause) { if (saveOnPauseOrFocusLoss && pause) Save(); }
    void OnApplicationFocus(bool hasFocus) { if (saveOnPauseOrFocusLoss && !hasFocus) Save(); }
}
