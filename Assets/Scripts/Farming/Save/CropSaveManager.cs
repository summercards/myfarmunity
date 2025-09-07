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
    }

    [Serializable]
    private class CropSaveFile
    {
        public string savedUtc;                 // 本次保存的UTC时间（ISO 8601）
        public List<CropSaveRecord> crops = new List<CropSaveRecord>();
    }

    public static CropSaveManager Instance { get; private set; }

    [Header("Data")]
    public SeedPlantDataSO plantDB;

    [Tooltip("统一写到 SavePaths 的当前存档槽目录下")]
    public string fileName = "crops.json";

    [Header("Options")]
    public bool saveOnPauseOrFocusLoss = true; // 退到后台/失焦也保存一次

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
        var file = new CropSaveFile
        {
            savedUtc = DateTime.UtcNow.ToString("o") // ISO 8601
        };

        foreach (var c in _tracked)
        {
            if (!c || string.IsNullOrEmpty(c.entryId)) continue;
            var plant = c.GetComponent<CropPlant>();
            var gs = plant ? plant.GetSaveState() : default;

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
        Debug.Log($"[CropSave] Saved {file.crops.Count} crops at {file.savedUtc} to {GetPath()}");
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
        if (file == null) return;

        // 计算离线秒数（没有 savedUtc 就按 0）
        double offlineSeconds = 0;
        if (!string.IsNullOrEmpty(file.savedUtc))
        {
            if (DateTime.TryParse(file.savedUtc, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var saved))
            {
                offlineSeconds = (DateTime.UtcNow - saved).TotalSeconds;
                if (offlineSeconds < 0) offlineSeconds = 0;
            }
        }

        int ok = 0;
        if (file.crops != null)
        {
            foreach (var r in file.crops)
            {
                var entry = plantDB ? plantDB.GetByPlantItemId(r.entryId) : null;
                if (entry == null || entry.cropPrefab == null) continue;

                var go = Instantiate(entry.cropPrefab, r.position, r.rotation);

                var crop = go.GetComponent<CropPlant>(); if (!crop) crop = go.AddComponent<CropPlant>();
                crop.Init(entry);

                // 先恢复保存时状态
                crop.ApplySaveState(new CropPlant.GrowthState
                {
                    stageIndex = r.stageIndex,
                    stageTimer = r.stageTimer,
                    mature = r.mature
                });

                // 再推进离线生长
                if (offlineSeconds > 0) crop.AdvanceBy((float)offlineSeconds);

                // 身份，用于下次保存
                var cp = go.GetComponent<CropPersistence>(); if (!cp) cp = go.AddComponent<CropPersistence>();
                cp.entryId = r.entryId;

                ok++;
            }
        }
#if UNITY_EDITOR
        Debug.Log($"[CropSave] Loaded {ok} crops. Offline advanced by {offlineSeconds:F1}s");
#endif
    }

    void OnApplicationQuit() => Save();

    void OnApplicationPause(bool pause)
    {
        if (saveOnPauseOrFocusLoss && pause) Save();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (saveOnPauseOrFocusLoss && !hasFocus) Save();
    }

#if UNITY_EDITOR
    [ContextMenu("Save Now")] void EditorSaveNow() => Save();
    [ContextMenu("Load Now")] void EditorLoadNow() => Load();
#endif
}
