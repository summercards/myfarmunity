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
    private class SaveFile
    {
        public string savedUtc;
        public List<Record> objects = new List<Record>();
    }

    public static BuildSaveManager Instance { get; private set; }

    [Header("Catalog")]
    public BuildCatalogSO catalog;

    [Header("Options")]
    public string fileName = "builds.json";
    public bool autoLoadOnStart = true;
    public bool saveOnQuitOrPause = true;

    private readonly HashSet<PlacedObject> _live = new HashSet<PlacedObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 对齐你项目的保存工具
        SavePaths.EnsureDir();
    }

    void Start()
    {
        if (autoLoadOnStart) Load();
    }

    public void Register(PlacedObject po)
    {
        if (po) _live.Add(po);
    }

    public void Unregister(PlacedObject po)
    {
        if (po) _live.Remove(po);
    }

    string GetPath() => SavePaths.FileInSlot(fileName);

    [ContextMenu("Save Now")]
    public void Save()
    {
        var data = new SaveFile { savedUtc = DateTime.UtcNow.ToString("o") };

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

        var json = JsonUtility.ToJson(data, true);
        var path = GetPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);

#if UNITY_EDITOR
        Debug.Log($"[BuildSave] Saved {_live.Count} objects -> {path}");
#endif
    }

    [ContextMenu("Load Now")]
    public void Load()
    {
        // 清理旧对象
        foreach (var po in new List<PlacedObject>(_live))
        {
            if (po) Destroy(po.gameObject);
        }
        _live.Clear();

        var path = GetPath();
        if (!File.Exists(path))
        {
#if UNITY_EDITOR
            Debug.Log($"[BuildSave] No save at {path}");
#endif
            return;
        }

        var json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<SaveFile>(json);
        if (data == null || data.objects == null) return;

        int loaded = 0;
        foreach (var r in data.objects)
        {
            var entry = catalog ? catalog.Get(r.itemId) : null;
            if (entry == null || entry.prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[BuildSave] Missing catalog entry for {r.itemId}, skip");
#endif
                continue;
            }

            var parent = entry.optionalParentAtRuntime ? entry.optionalParentAtRuntime : null;
            var go = Instantiate(entry.prefab, r.position, r.rotation, parent);
            go.transform.localScale = r.scale;

            var po = go.GetComponent<PlacedObject>();
            if (!po) po = go.AddComponent<PlacedObject>();
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
