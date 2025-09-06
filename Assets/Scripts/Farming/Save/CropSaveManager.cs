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
        // ★ 新增：生长状态
        public int stageIndex;
        public float stageTimer;
        public bool mature;
    }
    [Serializable]
    private class CropSaveFile
    {
        public List<CropSaveRecord> crops = new List<CropSaveRecord>();
    }

    public static CropSaveManager Instance { get; private set; }

    [Header("Data")]
    public SeedPlantDataSO plantDB;

    [Tooltip("统一写到 SavePaths 的当前存档槽目录下")]
    public string fileName = "crops.json";

    private readonly List<CropPersistence> _tracked = new List<CropPersistence>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SavePaths.EnsureDir();
        Load();
    }

    public void Register(CropPersistence c) { if (c != null && !_tracked.Contains(c)) _tracked.Add(c); }
    public void Unregister(CropPersistence c) { if (c != null) _tracked.Remove(c); }

    string GetPath() => SavePaths.FileInSlot(fileName);

    public void Save()
    {
        var file = new CropSaveFile();

        foreach (var c in _tracked)
        {
            if (!c || string.IsNullOrEmpty(c.entryId)) continue;

            // 找到作物并读取生长状态
            var plant = c.GetComponent<CropPlant>();
            CropPlant.GrowthState gs = plant ? plant.GetSaveState() : default;

            file.crops.Add(new CropSaveRecord
            {
                entryId = c.entryId,
                position = c.transform.position,
                rotation = c.transform.rotation,
                stageIndex = gs.stageIndex,
                stageTimer = gs.stageTimer,
                mature = gs.mature
            });
        }

        File.WriteAllText(GetPath(), JsonUtility.ToJson(file, true));
#if UNITY_EDITOR
        Debug.Log($"[CropSave] Saved {file.crops.Count} crops to {GetPath()}");
#endif
    }

    public void Load()
    {
        var path = GetPath();
        if (!File.Exists(path)) return;

        // 清场，避免重复
        foreach (var c in FindObjectsOfType<CropPersistence>())
            if (c) Destroy(c.gameObject);

        var file = JsonUtility.FromJson<CropSaveFile>(File.ReadAllText(path));
        if (file?.crops == null) return;

        int ok = 0;
        foreach (var r in file.crops)
        {
            var entry = plantDB ? plantDB.GetByPlantItemId(r.entryId) : null;
            if (entry == null || entry.cropPrefab == null) continue;

            var go = Instantiate(entry.cropPrefab, r.position, r.rotation);

            var crop = go.GetComponent<CropPlant>(); if (!crop) crop = go.AddComponent<CropPlant>();
            crop.Init(entry);

            // ★ 核心：恢复生长状态
            crop.ApplySaveState(new CropPlant.GrowthState
            {
                stageIndex = r.stageIndex,
                stageTimer = r.stageTimer,
                mature = r.mature
            });

            var cp = go.GetComponent<CropPersistence>(); if (!cp) cp = go.AddComponent<CropPersistence>();
            cp.entryId = r.entryId;

            ok++;
        }
#if UNITY_EDITOR
        Debug.Log($"[CropSave] Loaded {ok} crops from {path}");
#endif
    }

    void OnApplicationQuit() => Save();

#if UNITY_EDITOR
    [ContextMenu("Save Now")] void EditorSaveNow() => Save();
    [ContextMenu("Load Now")] void EditorLoadNow() => Load();
#endif
}
