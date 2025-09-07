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
        public string savedUtc;                 // ���α����UTCʱ�䣨ISO 8601��
        public List<CropSaveRecord> crops = new List<CropSaveRecord>();
    }

    public static CropSaveManager Instance { get; private set; }

    [Header("Data")]
    public SeedPlantDataSO plantDB;

    [Tooltip("ͳһд�� SavePaths �ĵ�ǰ�浵��Ŀ¼��")]
    public string fileName = "crops.json";

    [Header("Options")]
    public bool saveOnPauseOrFocusLoss = true; // �˵���̨/ʧ��Ҳ����һ��

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

        // �峡�������ظ�
        foreach (var c in FindObjectsOfType<CropPersistence>())
            if (c) Destroy(c.gameObject);

        var file = JsonUtility.FromJson<CropSaveFile>(File.ReadAllText(path));
        if (file == null) return;

        // ��������������û�� savedUtc �Ͱ� 0��
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

                // �Ȼָ�����ʱ״̬
                crop.ApplySaveState(new CropPlant.GrowthState
                {
                    stageIndex = r.stageIndex,
                    stageTimer = r.stageTimer,
                    mature = r.mature
                });

                // ���ƽ���������
                if (offlineSeconds > 0) crop.AdvanceBy((float)offlineSeconds);

                // ��ݣ������´α���
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
