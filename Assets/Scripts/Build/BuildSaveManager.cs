using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FarmGame.Core;

[DisallowMultipleComponent]
public class BuildSaveManager : MonoBehaviour, ISaveParticipant, IBuildSaveable
{
    [Serializable]
    private class Record
    {
        public string itemId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    [Serializable]
    private class FileData
    {
        public string savedUtc;
        public List<Record> objects = new List<Record>();
    }

    public static BuildSaveManager Instance { get; private set; }

    [Header("Data")]
    public BuildCatalogSO catalog;
    public string fileName = "builds.json";

    [Header("Behaviour")]
    public bool autoLoadOnStart = true;
    public bool saveOnQuitOrPause = true;
    public bool clearBeforeLoad = true;

    readonly List<PlacedObject> _live = new List<PlacedObject>();

    public SaveSection Section => SaveSection.Build;
    public UnityEngine.Object Owner => this;
    public string ParticipantName => GetType().Name;
    private bool UseCentralSave => RuntimeRefs.SaveService != null;

    void Awake()
    {
        if (!RuntimeService.TryClaimSingleton(this, Instance, nameof(BuildSaveManager), false))
        {
            return;
        }

        Instance = this;
        SavePaths.EnsureDir();
    }

    void OnEnable()
    {
        RuntimeRefs.SaveServiceChanged += HandleSaveServiceChanged;
        RegisterToSaveService();
    }

    void Start()
    {
        if (autoLoadOnStart && !UseCentralSave) Load();
    }

    void OnDisable()
    {
        RuntimeRefs.SaveServiceChanged -= HandleSaveServiceChanged;
        UnregisterFromSaveService();
    }

    public void Register(PlacedObject po) { if (po && !_live.Contains(po)) _live.Add(po); }
    public void Unregister(PlacedObject po) { if (po) _live.Remove(po); }

    string PathForWrite() => SavePaths.FileInSlot(fileName);
    string PathForRead() => SavePaths.FindExistingOrCurrent(fileName);

    [ContextMenu("Save Now")]
    public void Save()
    {
        var data = BuildFileDataSnapshot();

        var path = PathForWrite();
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
#if UNITY_EDITOR
        Debug.Log($"[BuildSave] Saved {data.objects.Count} objects -> {path}");
#endif
    }

    [ContextMenu("Load Now")]
    public void Load()
    {
        var path = PathForRead();
        if (!File.Exists(path))
        {
#if UNITY_EDITOR
            Debug.Log("[BuildSave] No save file yet.");
#endif
            return;
        }

        var data = JsonUtility.FromJson<FileData>(File.ReadAllText(path));
        ApplyFileData(data, clearBeforeLoad);
    }

    private FileData BuildFileDataSnapshot()
    {
        var data = new FileData { savedUtc = DateTime.UtcNow.ToString("o") };
        foreach (var po in _live)
        {
            if (!po) continue;
            data.objects.Add(new Record
            {
                itemId = po.itemId,
                position = po.transform.position,
                rotation = po.transform.rotation,
                scale = po.transform.localScale
            });
        }

        return data;
    }

    private void ApplyFileData(FileData data, bool clearCurrent)
    {
        if (clearCurrent)
        {
            for (int i = _live.Count - 1; i >= 0; i--)
                if (_live[i]) Destroy(_live[i].gameObject);
            _live.Clear();
        }

        if (data == null || data.objects == null) return;

        int loaded = 0;
        foreach (var r in data.objects)
        {
            var entry = catalog ? catalog.Get(r.itemId) : null;
            if (entry == null || entry.prefab == null) continue;

            var go = Instantiate(entry.prefab, r.position, r.rotation);
            go.transform.localScale = (r.scale.sqrMagnitude > 0.0001f) ? r.scale : entry.prefab.transform.localScale;

            var po = go.GetComponent<PlacedObject>() ?? go.AddComponent<PlacedObject>();
            po.itemId = r.itemId;

            loaded++;
        }
#if UNITY_EDITOR
        Debug.Log($"[BuildSave] Loaded {loaded} objects");
#endif
    }

    public object CaptureSaveData()
    {
        return BuildFileDataSnapshot();
    }

    public void RestoreSaveData(string jsonData, GameTimeSystem timeSystem)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            return;
        }

        ApplyFileData(JsonUtility.FromJson<FileData>(jsonData), clearBeforeLoad);
    }

    public object GetSaveData()
    {
        return CaptureSaveData();
    }

    public void LoadSaveData(string jsonData, GameTimeSystem timeSystem)
    {
        RestoreSaveData(jsonData, timeSystem);
    }

    void OnApplicationQuit() { if (saveOnQuitOrPause && !UseCentralSave) Save(); }
    void OnApplicationPause(bool pause) { if (saveOnQuitOrPause && pause && !UseCentralSave) Save(); }
    void OnApplicationFocus(bool focus) { if (saveOnQuitOrPause && !focus && !UseCentralSave) Save(); }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void HandleSaveServiceChanged(ISaveService _)
    {
        RegisterToSaveService();
    }

    private void RegisterToSaveService()
    {
        RuntimeRefs.SaveService?.RegisterParticipant(this);
    }

    private void UnregisterFromSaveService()
    {
        RuntimeRefs.SaveService?.UnregisterParticipant(this);
    }
}
