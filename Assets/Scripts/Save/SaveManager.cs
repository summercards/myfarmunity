// Assets/Scripts/Save/SaveManager.cs
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FarmGame.Core;

/// <summary>
/// 统一存档管理器
/// 负责游戏数据的保存和加载，以时间系统为核心协调所有时间相关的子系统
/// </summary>
public class SaveManager : MonoBehaviour, ISaveService
{
    private static SaveManager instance;
    public static SaveManager Instance => instance;

    private readonly Dictionary<SaveSection, List<ISaveParticipant>> participantsBySection = new Dictionary<SaveSection, List<ISaveParticipant>>();

    [Header("时间系统引用")]
    public GameTimeSystem timeSystem;

    [Header("子系统引用 - 可选")]
    public MonoBehaviour farmSystem;      // 农场系统
    public MonoBehaviour shopSystem;      // 商店系统
    public MonoBehaviour npcSystem;       // NPC系统
    public MonoBehaviour questSystem;     // 任务系统
    public MonoBehaviour cropSystem;      // 作物系统
    public MonoBehaviour buildSystem;     // 建造系统
    public MonoBehaviour inventorySystem; // 背包系统
    public MonoBehaviour playerSystem;    // 玩家系统

    [Header("存档配置")]
    public int maxSaveSlots = 10;
    public string saveFolder = "saves";
    public bool autoSaveEnabled = true;
    public float autoSaveInterval = 300f; // 5分钟自动保存

    private string savePath;
    private float autoSaveTimer;
    private bool isLoading = false;
    private Coroutine saveCoroutine;
    private bool isSaving = false;
    private bool hasStarted;

    private sealed class LegacySaveParticipant<TSaveable> : ISaveParticipant where TSaveable : class
    {
        private readonly MonoBehaviour owner;
        private readonly TSaveable saveable;
        private readonly Func<TSaveable, object> capture;
        private readonly Action<TSaveable, string, GameTimeSystem> restore;

        public LegacySaveParticipant(
            SaveSection section,
            MonoBehaviour owner,
            TSaveable saveable,
            Func<TSaveable, object> capture,
            Action<TSaveable, string, GameTimeSystem> restore)
        {
            Section = section;
            this.owner = owner;
            this.saveable = saveable;
            this.capture = capture;
            this.restore = restore;
        }

        public SaveSection Section { get; }
        public UnityEngine.Object Owner => owner;
        public string ParticipantName => owner != null ? owner.GetType().Name : typeof(TSaveable).Name;

        public object CaptureSaveData()
        {
            return capture != null ? capture(saveable) : null;
        }

        public void RestoreSaveData(string jsonData, GameTimeSystem timeSystem)
        {
            restore?.Invoke(saveable, jsonData, timeSystem);
        }
    }

    [Serializable]
    private sealed class SectionPayloadCollection
    {
        public int formatVersion = 1;
        public List<SectionPayloadEntry> entries = new List<SectionPayloadEntry>();
    }

    [Serializable]
    private sealed class SectionPayloadEntry
    {
        public string participantKey;
        public string participantName;
        public string jsonData;
    }

    void Awake()
    {
        if (!RuntimeService.TryClaimSingleton(this, instance, nameof(SaveManager)))
        {
            return;
        }

        instance = this;
        RuntimeRefs.TimeSystemChanged += HandleTimeSystemChanged;
        RuntimeRefs.RegisterSaveService(this);
        InitializeSavePath();
        ResolveTimeSystem(logWhenMissing: true);
        EnsureConfiguredParticipantsRegistered();
        Debug.Log("[SaveManager] 存档管理器已初始化");
    }

    void Start()
    {
        hasStarted = true;
        ResolveTimeSystem(logWhenMissing: false);
        EnsureConfiguredParticipantsRegistered();
        EnsureHourChangedSubscription();
    }

    void OnDestroy()
    {
        RuntimeRefs.TimeSystemChanged -= HandleTimeSystemChanged;
        UnsubscribeFromTimeSystem();

        participantsBySection.Clear();
        RuntimeRefs.UnregisterSaveService(this);

        if (instance == this)
        {
            instance = null;
        }
    }

    void Update()
    {
        // 禁用基于现实时间的自动保存，改用游戏内时间
        // if (!autoSaveEnabled || isLoading) return;
        // autoSaveTimer += Time.deltaTime; ...
    }

    /// <summary>
    /// 游戏内每小时触发一次
    /// </summary>
    private void OnGameHourChanged()
    {
        if (autoSaveEnabled && !isLoading && !isSaving)
        {
            // 只有当时间系统初始化完成后才保存（避免刚启动时保存）
            if (timeSystem.CurrentTime > 0.1f)
            {
                // 使用协程异步保存，避免主线程卡顿
                Debug.Log($"[SaveManager] 游戏时间 {timeSystem.TimeString}，开始异步自动保存...");
                AutoSaveAsync();
            }
        }
    }

    /// <summary>
    /// 游戏退出时自动保存
    /// </summary>
    void OnApplicationQuit()
    {
        if (autoSaveEnabled && !isLoading && !isSaving)
        {
            Debug.Log("[SaveManager] 游戏退出，正在执行自动保存...");
            // 退出时使用同步保存，确保数据不丢失
            AutoSave();
        }
    }

    /// <summary>
    /// 初始化存档路径
    /// </summary>
    private void InitializeSavePath()
    {
        savePath = Path.Combine(Application.persistentDataPath, saveFolder);
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            Debug.Log($"[SaveManager] 创建存档目录: {savePath}");
        }
    }

    private void ResolveTimeSystem(bool logWhenMissing)
    {
        if (timeSystem == null)
        {
            timeSystem = RuntimeRefs.TimeSystem;
        }

        if (timeSystem == null)
        {
            timeSystem = Resources.Load<GameTimeSystem>("DefaultTimeSystem");
        }

        if (timeSystem != null)
        {
            AttachTimeSystem(timeSystem, logResolved: true);
        }
        else if (logWhenMissing)
        {
            Debug.LogWarning("[SaveManager] 未找到时间系统，请在 Inspector 中手动配置");
        }
    }

    private void HandleTimeSystemChanged(GameTimeSystem changedSystem)
    {
        if (changedSystem == null || changedSystem == timeSystem)
        {
            return;
        }

        AttachTimeSystem(changedSystem, logResolved: true);
    }

    private void AttachTimeSystem(GameTimeSystem nextTimeSystem, bool logResolved)
    {
        if (nextTimeSystem == null)
        {
            return;
        }

        if (timeSystem != null && timeSystem != nextTimeSystem)
        {
            timeSystem.onHourChanged.RemoveListener(OnGameHourChanged);
        }

        timeSystem = nextTimeSystem;
        RuntimeRefs.RegisterTimeSystem(timeSystem);
        EnsureHourChangedSubscription();

        if (logResolved)
        {
            Debug.Log($"[SaveManager] 自动找到时间系统: {timeSystem.name}");
        }
    }

    private void EnsureHourChangedSubscription()
    {
        if (!hasStarted || timeSystem == null)
        {
            return;
        }

        timeSystem.onHourChanged.RemoveListener(OnGameHourChanged);
        timeSystem.onHourChanged.AddListener(OnGameHourChanged);
        Debug.Log("[SaveManager] 已启用每游戏小时自动保存");
    }

    private void UnsubscribeFromTimeSystem()
    {
        if (timeSystem == null)
        {
            return;
        }

        timeSystem.onHourChanged.RemoveListener(OnGameHourChanged);
    }

    private void EnsureConfiguredParticipantsRegistered()
    {
        RegisterConfiguredParticipant(farmSystem, SaveSection.Farm);
        RegisterConfiguredParticipant(shopSystem, SaveSection.Shop);
        RegisterConfiguredParticipant(npcSystem, SaveSection.NPC);
        RegisterConfiguredParticipant(questSystem, SaveSection.Quest);
        RegisterConfiguredParticipant(cropSystem, SaveSection.Crop);
        RegisterConfiguredParticipant(buildSystem, SaveSection.Build);
        RegisterConfiguredParticipant(inventorySystem, SaveSection.Inventory);
        RegisterConfiguredParticipant(playerSystem, SaveSection.Player);
    }

    private void RegisterConfiguredParticipant(MonoBehaviour system, SaveSection section)
    {
        if (system == null)
        {
            return;
        }

        if (system is ISaveParticipant participant)
        {
            RegisterParticipant(participant);
            return;
        }

        switch (section)
        {
            case SaveSection.Farm:
                if (system is IFarmSaveable farmSaveable)
                {
                    RegisterParticipant(new LegacySaveParticipant<IFarmSaveable>(
                        section,
                        system,
                        farmSaveable,
                        saveable => saveable.GetSaveData(),
                        (saveable, jsonData, currentTimeSystem) => saveable.LoadSaveData(jsonData, currentTimeSystem)));
                }
                break;
            case SaveSection.Shop:
                if (system is IShopSaveable shopSaveable)
                {
                    RegisterParticipant(new LegacySaveParticipant<IShopSaveable>(
                        section,
                        system,
                        shopSaveable,
                        saveable => saveable.GetSaveData(),
                        (saveable, jsonData, currentTimeSystem) => saveable.LoadSaveData(jsonData, currentTimeSystem)));
                }
                break;
            case SaveSection.NPC:
                if (system is INPCSaveable npcSaveable)
                {
                    RegisterParticipant(new LegacySaveParticipant<INPCSaveable>(
                        section,
                        system,
                        npcSaveable,
                        saveable => saveable.GetSaveData(),
                        (saveable, jsonData, currentTimeSystem) => saveable.LoadSaveData(jsonData, currentTimeSystem)));
                }
                break;
            case SaveSection.Quest:
                if (system is IQuestSaveable questSaveable)
                {
                    RegisterParticipant(new LegacySaveParticipant<IQuestSaveable>(
                        section,
                        system,
                        questSaveable,
                        saveable => saveable.GetSaveData(),
                        (saveable, jsonData, currentTimeSystem) => saveable.LoadSaveData(jsonData, currentTimeSystem)));
                }
                break;
            case SaveSection.Inventory:
                if (system is IInventorySaveable inventorySaveable)
                {
                    RegisterParticipant(new LegacySaveParticipant<IInventorySaveable>(
                        section,
                        system,
                        inventorySaveable,
                        saveable => saveable.GetSaveData(),
                        (saveable, jsonData, currentTimeSystem) => saveable.LoadSaveData(jsonData, currentTimeSystem)));
                }
                break;
            case SaveSection.Crop:
                if (system is ICropSaveable cropSaveable)
                {
                    RegisterParticipant(new LegacySaveParticipant<ICropSaveable>(
                        section,
                        system,
                        cropSaveable,
                        saveable => saveable.GetSaveData(),
                        (saveable, jsonData, currentTimeSystem) => saveable.LoadSaveData(jsonData, currentTimeSystem)));
                }
                break;
            case SaveSection.Build:
                if (system is IBuildSaveable buildSaveable)
                {
                    RegisterParticipant(new LegacySaveParticipant<IBuildSaveable>(
                        section,
                        system,
                        buildSaveable,
                        saveable => saveable.GetSaveData(),
                        (saveable, jsonData, currentTimeSystem) => saveable.LoadSaveData(jsonData, currentTimeSystem)));
                }
                break;
            case SaveSection.Player:
                if (system is IPlayerSaveable playerSaveable)
                {
                    RegisterParticipant(new LegacySaveParticipant<IPlayerSaveable>(
                        section,
                        system,
                        playerSaveable,
                        saveable => saveable.GetSaveData(),
                        (saveable, jsonData, currentTimeSystem) => saveable.LoadSaveData(jsonData, currentTimeSystem)));
                }
                break;
        }
    }

    public void RegisterParticipant(ISaveParticipant participant)
    {
        if (participant == null)
        {
            return;
        }

        if (participant.Owner == null)
        {
            Debug.LogWarning($"[SaveManager] 跳过无效存档参与者: {participant.ParticipantName}");
            return;
        }

        if (!participantsBySection.TryGetValue(participant.Section, out List<ISaveParticipant> sectionParticipants))
        {
            sectionParticipants = new List<ISaveParticipant>();
            participantsBySection[participant.Section] = sectionParticipants;
        }

        string participantKey = BuildParticipantKey(participant);
        for (int i = 0; i < sectionParticipants.Count; i++)
        {
            ISaveParticipant existing = sectionParticipants[i];
            if (existing == null || existing.Owner == null)
            {
                sectionParticipants.RemoveAt(i);
                i--;
                continue;
            }

            if (ReferenceEquals(existing, participant))
            {
                return;
            }

            if (string.Equals(BuildParticipantKey(existing), participantKey, StringComparison.Ordinal))
            {
                Debug.Log($"[SaveManager] 替换 {participant.Section} 存档参与者: {GetParticipantName(existing)} -> {participant.ParticipantName}");
                sectionParticipants[i] = participant;
                return;
            }
        }

        sectionParticipants.Add(participant);
    }

    public void UnregisterParticipant(ISaveParticipant participant)
    {
        if (participant == null)
        {
            return;
        }

        if (!participantsBySection.TryGetValue(participant.Section, out List<ISaveParticipant> sectionParticipants))
        {
            return;
        }

        string participantKey = BuildParticipantKey(participant);
        for (int i = sectionParticipants.Count - 1; i >= 0; i--)
        {
            ISaveParticipant existing = sectionParticipants[i];
            if (existing == null || existing.Owner == null)
            {
                sectionParticipants.RemoveAt(i);
                continue;
            }

            if (ReferenceEquals(existing, participant) ||
                string.Equals(BuildParticipantKey(existing), participantKey, StringComparison.Ordinal))
            {
                sectionParticipants.RemoveAt(i);
            }
        }

        if (sectionParticipants.Count == 0)
        {
            participantsBySection.Remove(participant.Section);
        }
    }

    private void CleanupParticipants()
    {
        var invalidSections = new List<SaveSection>();
        foreach (var kvp in participantsBySection)
        {
            List<ISaveParticipant> sectionParticipants = kvp.Value;
            if (sectionParticipants == null || sectionParticipants.Count == 0)
            {
                invalidSections.Add(kvp.Key);
                continue;
            }

            for (int i = sectionParticipants.Count - 1; i >= 0; i--)
            {
                ISaveParticipant participant = sectionParticipants[i];
                if (participant == null || participant.Owner == null)
                {
                    sectionParticipants.RemoveAt(i);
                }
            }

            if (sectionParticipants.Count == 0)
            {
                invalidSections.Add(kvp.Key);
            }
        }

        foreach (SaveSection section in invalidSections)
        {
            participantsBySection.Remove(section);
        }
    }

    private string GetParticipantName(ISaveParticipant participant)
    {
        return participant == null ? "Unknown" : participant.ParticipantName;
    }

    /// <summary>
    /// 自动保存（同步）
    /// </summary>
    public void AutoSave()
    {
        SaveGame("autosave");
        Debug.Log("[SaveManager] 自动保存完成");
    }

    /// <summary>
    /// 自动保存（异步，推荐用于自动保存）
    /// </summary>
    public void AutoSaveAsync()
    {
        if (saveCoroutine != null)
        {
            StopCoroutine(saveCoroutine);
        }
        saveCoroutine = StartCoroutine(SaveGameAsync("autosave"));
    }

    /// <summary>
    /// 保存游戏（同步版本 - 用于手动保存或退出时保存）
    /// </summary>
    public void SaveGame(string saveName = "autosave")
    {
        if (timeSystem == null)
        {
            Debug.LogError("[SaveManager] 时间系统未设置，无法保存！");
            return;
        }

        if (isLoading)
        {
            Debug.LogWarning("[SaveManager] 正在加载中，无法保存");
            return;
        }

        try
        {
            EnsureConfiguredParticipantsRegistered();
            CleanupParticipants();

            GameSaveData saveData = new GameSaveData();

            // 1. 保存时间数据（核心）
            saveData.timeData = timeSystem.GetSaveData();

            // 2. 保存子系统数据
            saveData.farmData = CaptureSectionData(SaveSection.Farm);
            saveData.shopData = CaptureSectionData(SaveSection.Shop);
            saveData.npcData = CaptureSectionData(SaveSection.NPC);
            saveData.questData = CaptureSectionData(SaveSection.Quest);
            saveData.cropData = CaptureSectionData(SaveSection.Crop);
            saveData.buildData = CaptureSectionData(SaveSection.Build);
            saveData.inventoryData = CaptureSectionData(SaveSection.Inventory);
            saveData.playerData = CaptureSectionData(SaveSection.Player);

            // 3. 设置元数据
            saveData.saveVersion = 1;
            saveData.saveTime = DateTime.Now;
            saveData.saveName = saveName;
            saveData.playTimeSeconds = PlayerPrefs.GetInt("PlayTime", 0);

            // 4. 序列化并保存
            string json = JsonUtility.ToJson(saveData, true);
            WriteAllTextSafe(GetSaveFilePath(saveName), json);

            Debug.Log($"[SaveManager] 游戏已保存: {saveName}");
            OnGameSaved?.Invoke(saveName);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 保存失败: {e.Message}");
        }
    }

    /// <summary>
    /// 异步保存游戏（协程版本 - 避免主线程卡顿）
    /// </summary>
    public IEnumerator SaveGameAsync(string saveName = "autosave")
    {
        // 验证保存条件
        if (!CanSave(out bool needsBreak))
        {
            yield break;
        }

        isSaving = true;
        bool wasPaused = timeSystem.IsPaused;
        timeSystem.Pause();

        // 执行保存
        GameSaveData saveData = ExecuteSaveSteps(saveName);

        // 写入文件
        if (saveData != null)
        {
            string json = JsonUtility.ToJson(saveData, true);
            WriteAllTextSafe(GetSaveFilePath(saveName), json);
            OnGameSaved?.Invoke(saveName);
            Debug.Log($"[SaveManager] 异步保存完成: {saveName}");
        }

        // 恢复时间系统
        if (!wasPaused) timeSystem.Resume();

        isSaving = false;
        saveCoroutine = null;
    }

    /// <summary>
    /// 验证是否可以保存
    /// </summary>
    private bool CanSave(out bool needsBreak)
    {
        needsBreak = false;
        if (timeSystem == null)
        {
            Debug.LogError("[SaveManager] 时间系统未设置，无法保存！");
            return false;
        }
        if (isLoading)
        {
            Debug.LogWarning("[SaveManager] 正在加载中，无法保存");
            return false;
        }
        if (isSaving)
        {
            Debug.LogWarning("[SaveManager] 正在保存中，跳过本次保存");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 执行保存步骤（分帧执行避免卡顿）
    /// </summary>
    private GameSaveData ExecuteSaveSteps(string saveName)
    {
        EnsureConfiguredParticipantsRegistered();
        CleanupParticipants();

        var saveData = new GameSaveData();
        var steps = new Action[]
        {
            () => saveData.timeData = timeSystem.GetSaveData(),
            () => saveData.farmData = CaptureSectionData(SaveSection.Farm),
            () => saveData.shopData = CaptureSectionData(SaveSection.Shop),
            () => saveData.npcData = CaptureSectionData(SaveSection.NPC),
            () => saveData.questData = CaptureSectionData(SaveSection.Quest),
            () => saveData.cropData = CaptureSectionData(SaveSection.Crop),
            () => saveData.buildData = CaptureSectionData(SaveSection.Build),
            () => saveData.inventoryData = CaptureSectionData(SaveSection.Inventory),
            () => saveData.playerData = CaptureSectionData(SaveSection.Player),
            () => {
                saveData.saveVersion = 1;
                saveData.saveTime = DateTime.Now;
                saveData.saveName = saveName;
                saveData.playTimeSeconds = PlayerPrefs.GetInt("PlayTime", 0);
            }
        };

        foreach (var step in steps)
        {
            try { step(); }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 保存步骤失败: {e.Message}");
                return null;
            }
        }

        return saveData;
    }

    /// <summary>
    /// 加载游戏
    /// </summary>
    public void LoadGame(string saveName = "autosave")
    {
        if (timeSystem == null)
        {
            Debug.LogError("[SaveManager] 时间系统未设置，无法加载！");
            return;
        }

        if (isLoading)
        {
            Debug.LogWarning("[SaveManager] 已经在加载中");
            return;
        }

        string filePath = GetSaveFilePath(saveName);
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[SaveManager] 存档不存在: {saveName}");
            return;
        }

        try
        {
            isLoading = true;
            EnsureConfiguredParticipantsRegistered();
            CleanupParticipants();

            // 1. 反序列化
            string json = File.ReadAllText(filePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

            // 2. 验证存档版本
            if (saveData.saveVersion != 1)
            {
                Debug.LogWarning($"[SaveManager] 存档版本不匹配: {saveData.saveVersion}");
            }

            Debug.Log($"[SaveManager] 正在加载存档: {saveName} (保存于 {saveData.saveTime})");

            // 3. 加载时间数据（核心步骤 - 会触发 onLoadComplete 事件）
            timeSystem.LoadSaveData(saveData.timeData);

            // 4. 加载子系统数据（通过事件触发，子系统需要订阅 onLoadComplete）
            RestoreSectionData(SaveSection.Farm, saveData.farmData);
            RestoreSectionData(SaveSection.Shop, saveData.shopData);
            RestoreSectionData(SaveSection.NPC, saveData.npcData);
            RestoreSectionData(SaveSection.Quest, saveData.questData);
            RestoreSectionData(SaveSection.Crop, saveData.cropData);
            RestoreSectionData(SaveSection.Build, saveData.buildData);
            RestoreSectionData(SaveSection.Inventory, saveData.inventoryData);
            RestoreSectionData(SaveSection.Player, saveData.playerData);

            // 5. 恢复播放时间
            PlayerPrefs.SetInt("PlayTime", saveData.playTimeSeconds);

            Debug.Log($"[SaveManager] 游戏已加载: {saveName}");
            OnGameLoaded?.Invoke(saveName);

            isLoading = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 加载失败: {e.Message}");
            isLoading = false;
        }
    }

    private string CaptureSectionData(SaveSection section)
    {
        List<ISaveParticipant> sectionParticipants = GetActiveParticipants(section);
        if (sectionParticipants == null || sectionParticipants.Count == 0)
        {
            return null;
        }

        if (sectionParticipants.Count == 1)
        {
            return CaptureParticipantData(sectionParticipants[0]);
        }

        SectionPayloadCollection payloadCollection = new SectionPayloadCollection();
        foreach (ISaveParticipant participant in sectionParticipants)
        {
            string jsonData = CaptureParticipantData(participant);
            if (string.IsNullOrEmpty(jsonData))
            {
                continue;
            }

            payloadCollection.entries.Add(new SectionPayloadEntry
            {
                participantKey = BuildParticipantKey(participant),
                participantName = participant.ParticipantName,
                jsonData = jsonData
            });
        }

        if (payloadCollection.entries.Count == 0)
        {
            return null;
        }

        if (payloadCollection.entries.Count == 1)
        {
            return payloadCollection.entries[0].jsonData;
        }

        return JsonUtility.ToJson(payloadCollection);
    }

    private void RestoreSectionData(SaveSection section, string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            return;
        }

        List<ISaveParticipant> sectionParticipants = GetActiveParticipants(section);
        if (sectionParticipants == null || sectionParticipants.Count == 0)
        {
            return;
        }

        SectionPayloadCollection payloadCollection = JsonUtility.FromJson<SectionPayloadCollection>(jsonData);
        if (payloadCollection != null &&
            payloadCollection.formatVersion == 1 &&
            payloadCollection.entries != null &&
            payloadCollection.entries.Count > 0)
        {
            foreach (SectionPayloadEntry entry in payloadCollection.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.jsonData))
                {
                    continue;
                }

                ISaveParticipant matchedParticipant = FindParticipantForEntry(sectionParticipants, entry);
                if (matchedParticipant == null)
                {
                    Debug.LogWarning($"[SaveManager] 未找到 {section} 的匹配存档参与者: {entry.participantName}");
                    continue;
                }

                matchedParticipant.RestoreSaveData(entry.jsonData, timeSystem);
            }
            return;
        }

        if (sectionParticipants.Count > 1)
        {
            Debug.LogWarning($"[SaveManager] {section} 存档为旧格式，存在多个参与者时将仅恢复第一个: {sectionParticipants[0].ParticipantName}");
        }

        sectionParticipants[0].RestoreSaveData(jsonData, timeSystem);
    }

    private List<ISaveParticipant> GetActiveParticipants(SaveSection section)
    {
        if (!participantsBySection.TryGetValue(section, out List<ISaveParticipant> sectionParticipants) ||
            sectionParticipants == null ||
            sectionParticipants.Count == 0)
        {
            return null;
        }

        for (int i = sectionParticipants.Count - 1; i >= 0; i--)
        {
            ISaveParticipant participant = sectionParticipants[i];
            if (participant == null || participant.Owner == null)
            {
                sectionParticipants.RemoveAt(i);
            }
        }

        if (sectionParticipants.Count == 0)
        {
            participantsBySection.Remove(section);
            return null;
        }

        return sectionParticipants;
    }

    private string CaptureParticipantData(ISaveParticipant participant)
    {
        if (participant == null || participant.Owner == null)
        {
            return null;
        }

        object data = participant.CaptureSaveData();
        if (data == null)
        {
            Debug.LogWarning($"[SaveManager] {participant.ParticipantName}.CaptureSaveData() 返回 null");
            return null;
        }

        return data is string json ? json : JsonUtility.ToJson(data);
    }

    private ISaveParticipant FindParticipantForEntry(List<ISaveParticipant> sectionParticipants, SectionPayloadEntry entry)
    {
        if (sectionParticipants == null || sectionParticipants.Count == 0 || entry == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(entry.participantKey))
        {
            foreach (ISaveParticipant participant in sectionParticipants)
            {
                if (participant != null &&
                    string.Equals(BuildParticipantKey(participant), entry.participantKey, StringComparison.Ordinal))
                {
                    return participant;
                }
            }
        }

        if (!string.IsNullOrEmpty(entry.participantName))
        {
            ISaveParticipant nameMatch = null;
            int matchCount = 0;
            foreach (ISaveParticipant participant in sectionParticipants)
            {
                if (participant != null &&
                    string.Equals(participant.ParticipantName, entry.participantName, StringComparison.Ordinal))
                {
                    nameMatch = participant;
                    matchCount++;
                }
            }

            if (matchCount == 1)
            {
                return nameMatch;
            }
        }

        return sectionParticipants.Count == 1 ? sectionParticipants[0] : null;
    }

    private string BuildParticipantKey(ISaveParticipant participant)
    {
        if (participant == null)
        {
            return string.Empty;
        }

        string ownerKey = GetOwnerKey(participant.Owner);
        return $"{participant.ParticipantName}::{ownerKey}";
    }

    private static string GetOwnerKey(UnityEngine.Object owner)
    {
        if (owner == null)
        {
            return "null";
        }

        if (owner is Component component)
        {
            return $"{component.GetType().Name}@{GetTransformPath(component.transform)}";
        }

        if (owner is GameObject gameObject)
        {
            return $"GameObject@{GetTransformPath(gameObject.transform)}";
        }

        return $"{owner.GetType().Name}@{owner.name}";
    }

    private static string GetTransformPath(Transform transform)
    {
        if (transform == null)
        {
            return "null";
        }

        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        return path;
    }

    /// <summary>
    /// 删除存档
    /// </summary>
    public bool DeleteSave(string saveName)
    {
        string filePath = GetSaveFilePath(saveName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"[SaveManager] 存档已删除: {saveName}");
            OnSaveDeleted?.Invoke(saveName);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取存档列表
    /// </summary>
    public List<SaveInfo> GetSaveList()
    {
        List<SaveInfo> saveList = new List<SaveInfo>();

        if (!Directory.Exists(savePath))
            return saveList;

        string[] files = Directory.GetFiles(savePath, "*.json");

        foreach (string file in files)
        {
            try
            {
                string json = File.ReadAllText(file);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

                SaveInfo info = new SaveInfo
                {
                    saveName = data.saveName,
                    saveTime = data.saveTime,
                    playTimeSeconds = data.playTimeSeconds,
                    year = data.timeData.currentYear,
                    month = data.timeData.currentMonth,
                    day = data.timeData.currentDay,
                    hour = data.timeData.currentHour,
                    minute = data.timeData.currentMinute,
                    season = (Season)data.timeData.currentSeason,
                    weather = (WeatherType)data.timeData.currentWeather
                };

                saveList.Add(info);
            }
            catch
            {
                Debug.LogWarning($"[SaveManager] 读取存档信息失败: {Path.GetFileName(file)}");
            }
        }

        return saveList.OrderByDescending(s => s.saveTime).ToList();
    }

    /// <summary>
    /// 存档是否存在
    /// </summary>
    public bool SaveExists(string saveName)
    {
        return File.Exists(GetSaveFilePath(saveName));
    }

    /// <summary>
    /// 获取存档文件路径
    /// </summary>
    private string GetSaveFilePath(string saveName)
    {
        return Path.Combine(savePath, $"{saveName}.json");
    }

    /// <summary>
    /// 安全写入文件（使用 using 确保资源释放）
    /// </summary>
    private void WriteAllTextSafe(string path, string content)
    {
        try
        {
            using (var writer = new StreamWriter(path, false))
            {
                writer.Write(content);
            }
            Debug.Log($"[SaveManager] 文件写入成功: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 文件写入失败: {e.Message}");
            throw;
        }
    }

    // === 事件 ===

    public event Action<string> OnGameSaved;
    public event Action<string> OnGameLoaded;
    public event Action<string> OnSaveDeleted;
}

// === 存档数据结构 ===

/// <summary>
/// 主存档数据
/// </summary>
[Serializable]
public class GameSaveData
{
    // === 核心时间数据 ===
    public TimeSaveData timeData;

    // === 时间相关子系统数据 ===
    public string farmData;      // 农田数据（含作物生长时间）
    public string shopData;      // 商店数据（含刷新时间）
    public string npcData;       // NPC数据（含行程/作息）
    public string questData;     // 任务数据（含截止时间）
    public string cropData;      // 作物数据
    public string buildData;     // 建造数据

    // === 其他数据 ===
    public string inventoryData;
    public string playerData;

    // === 存档元数据 ===
    public int saveVersion;
    public DateTime saveTime;
    public string saveName;
    public int playTimeSeconds;
}

/// <summary>
/// 存档信息（用于UI显示）
/// </summary>
[Serializable]
public struct SaveInfo
{
    public string saveName;
    public DateTime saveTime;
    public int playTimeSeconds;

    // 时间信息
    public int year;
    public int month;
    public int day;
    public int hour;
    public int minute;
    public Season season;
    public WeatherType weather;

    public string GetDisplayTime()
    {
        return $"{hour:D2}:{minute:D2}";
    }

    public string GetDisplayDate()
    {
        return $"{year}年{month}月{day}日";
    }

    public string GetPlayTimeString()
    {
        TimeSpan ts = TimeSpan.FromSeconds(playTimeSeconds);
        return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}

// === 子系统存档接口 ===

public enum SaveSection
{
    Farm,
    Shop,
    NPC,
    Quest,
    Crop,
    Build,
    Inventory,
    Player
}

public interface ISaveParticipant
{
    SaveSection Section { get; }
    UnityEngine.Object Owner { get; }
    string ParticipantName { get; }
    object CaptureSaveData();
    void RestoreSaveData(string jsonData, GameTimeSystem timeSystem);
}

public interface ISaveService
{
    void RegisterParticipant(ISaveParticipant participant);
    void UnregisterParticipant(ISaveParticipant participant);
}

/// <summary>
/// 可存档的农场系统
/// </summary>
public interface IFarmSaveable
{
    object GetSaveData();
    void LoadSaveData(string jsonData, GameTimeSystem timeSystem);
}

/// <summary>
/// 可存档的商店系统
/// </summary>
public interface IShopSaveable
{
    object GetSaveData();
    void LoadSaveData(string jsonData, GameTimeSystem timeSystem);
}

/// <summary>
/// 可存档的NPC系统
/// </summary>
public interface INPCSaveable
{
    object GetSaveData();
    void LoadSaveData(string jsonData, GameTimeSystem timeSystem);
}

/// <summary>
/// 可存档的任务系统
/// </summary>
public interface IQuestSaveable
{
    object GetSaveData();
    void LoadSaveData(string jsonData, GameTimeSystem timeSystem);
}

/// <summary>
/// 可存档的背包系统
/// </summary>
public interface IInventorySaveable
{
    object GetSaveData();
    void LoadSaveData(string jsonData, GameTimeSystem timeSystem);
}

/// <summary>
/// 可存档的玩家系统
/// </summary>
public interface IPlayerSaveable
{
    object GetSaveData();
    void LoadSaveData(string jsonData, GameTimeSystem timeSystem);
}

/// <summary>
/// 可存档的作物系统
/// </summary>
public interface ICropSaveable
{
    object GetSaveData();
    void LoadSaveData(string jsonData, GameTimeSystem timeSystem);
}

/// <summary>
/// 可存档的建造系统
/// </summary>
public interface IBuildSaveable
{
    object GetSaveData();
    void LoadSaveData(string jsonData, GameTimeSystem timeSystem);
}
