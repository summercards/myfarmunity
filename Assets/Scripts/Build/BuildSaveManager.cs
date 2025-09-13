using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DisallowMultipleComponent]
public class BuildSaveManager : MonoBehaviour
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

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SavePaths.EnsureDir();
    }

    void Start() { if (autoLoadOnStart) Load(); }

    public void Register(PlacedObject po) { if (po && !_live.Contains(po)) _live.Add(po); }
    public void Unregister(PlacedObject po) { if (po) _live.Remove(po); }

    string PathForWrite() => SavePaths.FileInSlot(fileName);
    string PathForRead() => SavePaths.FindExistingOrCurrent(fileName);

    [ContextMenu("Save Now")]
    public void Save()
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

        if (clearBeforeLoad)
        {
            for (int i = _live.Count - 1; i >= 0; i--)
                if (_live[i]) Destroy(_live[i].gameObject);
            _live.Clear();
        }

        var data = JsonUtility.FromJson<FileData>(File.ReadAllText(path));
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
        Debug.Log($"[BuildSave] Loaded {loaded} objects from {path}");
#endif
    }

    void OnApplicationQuit() { if (saveOnQuitOrPause) Save(); }
    void OnApplicationPause(bool pause) { if (saveOnQuitOrPause && pause) Save(); }
    void OnApplicationFocus(bool focus) { if (saveOnQuitOrPause && !focus) Save(); }
}
