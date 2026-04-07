using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FarmGame.Core;

public class CropSaveManager : MonoBehaviour, ISaveParticipant, ICropSaveable
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
    public bool useStandaloneFilePersistenceWhenNoSaveManager = true;

    [Tooltip("启用自动保存（仅在检测到变更时落盘）")]
    public bool autoSaveEnabled = true;
    [Tooltip("自动保存间隔（秒）")]
    public float autoSaveInterval = 10f;

    private readonly List<CropPersistence> _tracked = new List<CropPersistence>();
    private bool _dirty = false;
    private double _offlineSeconds = 0;
    private bool _localLoadAttempted = false;

    public SaveSection Section => SaveSection.Crop;
    public UnityEngine.Object Owner => this;
    public string ParticipantName => GetType().Name;
    private bool UseCentralSave => RuntimeRefs.SaveService != null;

    void Awake()
    {
        if (!RuntimeService.TryClaimSingleton(this, Instance, nameof(CropSaveManager), false))
        {
            return;
        }

        Instance = this;
        SavePaths.EnsureDir();
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

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        CancelInvoke(nameof(AutoSaveTick));
    }

    void OnDisable()
    {
        RuntimeRefs.SaveServiceChanged -= HandleSaveServiceChanged;
        UnregisterFromSaveService();
        CancelInvoke(nameof(AutoSaveTick));
    }

    void AutoSaveTick()
    {
        if (!autoSaveEnabled || !ShouldUseLocalFilePersistence()) return;
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
        var file = BuildCropSaveSnapshot();

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
        ApplyCropSaveSnapshot(file);
    }

    private CropSaveFile BuildCropSaveSnapshot()
    {
        var file = new CropSaveFile { savedUtc = DateTime.UtcNow.ToString("o") };

        foreach (var c in _tracked)
        {
            if (c == null) continue;
            var plant = c.GetComponent<CropPlant>();
            if (plant == null) continue;

            string entryId = !string.IsNullOrEmpty(c.entryId)
                ? c.entryId
                : (plant != null ? plant.GetPlantItemIdForSave() : null);

            if (string.IsNullOrEmpty(entryId)) continue;

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

        return file;
    }

    private void ApplyCropSaveSnapshot(CropSaveFile file)
    {
        if (file == null) return;

        _offlineSeconds = 0;
        if (!string.IsNullOrEmpty(file.savedUtc)
            && DateTime.TryParse(file.savedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var saved))
        {
            _offlineSeconds = (DateTime.UtcNow - saved.ToUniversalTime()).TotalSeconds;
        }

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
                var crop = go.GetComponent<CropPlant>();
                if (!crop) crop = go.AddComponent<CropPlant>();
                crop.Init(entry);

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
        Debug.Log($"[CropSave] Loaded {loaded} crops. Offline +{_offlineSeconds:F1}s");
#endif

        _dirty = false;
    }

    public object CaptureSaveData()
    {
        return BuildCropSaveSnapshot();
    }

    public void RestoreSaveData(string jsonData, GameTimeSystem timeSystem)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            return;
        }

        ApplyCropSaveSnapshot(JsonUtility.FromJson<CropSaveFile>(jsonData));
    }

    public object GetSaveData()
    {
        return CaptureSaveData();
    }

    public void LoadSaveData(string jsonData, GameTimeSystem timeSystem)
    {
        RestoreSaveData(jsonData, timeSystem);
    }

    private bool ShouldUseLocalFilePersistence()
    {
        return useStandaloneFilePersistenceWhenNoSaveManager && !UseCentralSave;
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
        if (!_localLoadAttempted && autoLoadOnStart && ShouldUseLocalFilePersistence())
        {
            _localLoadAttempted = true;
            Load();
        }

        CancelInvoke(nameof(AutoSaveTick));
        if (autoSaveEnabled && ShouldUseLocalFilePersistence())
        {
            InvokeRepeating(nameof(AutoSaveTick), autoSaveInterval, autoSaveInterval);
        }
    }

    void OnApplicationQuit()
    {
        if (ShouldUseLocalFilePersistence()) Save();
    }

    void OnApplicationPause(bool pause)
    {
        if (saveOnPauseOrFocusLoss && pause && ShouldUseLocalFilePersistence()) Save();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (saveOnPauseOrFocusLoss && !hasFocus && ShouldUseLocalFilePersistence()) Save();
    }
}
