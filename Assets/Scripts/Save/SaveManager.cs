// Assets/Scripts/Save/SaveManager.cs
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 统一存档管理器
/// 负责游戏数据的保存和加载，以时间系统为核心协调所有时间相关的子系统
/// </summary>
public class SaveManager : MonoBehaviour
{
    private static SaveManager instance;
    public static SaveManager Instance => instance;

    [Header("时间系统引用")]
    public GameTimeSystem timeSystem;

    [Header("子系统引用 - 可选")]
    public MonoBehaviour farmSystem;      // 农场系统
    public MonoBehaviour shopSystem;      // 商店系统
    public MonoBehaviour npcSystem;       // NPC系统
    public MonoBehaviour questSystem;     // 任务系统
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

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSavePath();

            // 自动查找时间系统（如果未手动配置）
            if (timeSystem == null)
            {
                timeSystem = Resources.Load<GameTimeSystem>("DefaultTimeSystem");
                if (timeSystem == null)
                {
                    GameTimeSystem[] instances = Resources.FindObjectsOfTypeAll<GameTimeSystem>();
                    if (instances.Length > 0)
                    {
                        timeSystem = instances[0];
                    }
                }
                if (timeSystem != null)
                {
                    Debug.Log($"[SaveManager] 自动找到时间系统: {timeSystem.name}");
                }
                else
                {
                    Debug.LogWarning("[SaveManager] 未找到时间系统，请在 Inspector 中手动配置");
                }
            }

            Debug.Log("[SaveManager] 存档管理器已初始化");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 再次检查时间系统（确保场景加载后有引用）
        if (timeSystem == null)
        {
            timeSystem = Resources.Load<GameTimeSystem>("DefaultTimeSystem");
            if (timeSystem == null)
            {
                GameTimeSystem[] instances = Resources.FindObjectsOfTypeAll<GameTimeSystem>();
                if (instances.Length > 0)
                {
                    timeSystem = instances[0];
                }
            }
        }

        // 订阅游戏内每小时事件
        if (timeSystem != null)
        {
            timeSystem.onHourChanged.AddListener(OnGameHourChanged);
            Debug.Log("[SaveManager] 已启用每游戏小时自动保存");
        }
    }

    void OnDestroy()
    {
        if (timeSystem != null)
        {
            timeSystem.onHourChanged.RemoveListener(OnGameHourChanged);
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
            GameSaveData saveData = new GameSaveData();

            // 1. 保存时间数据（核心）
            saveData.timeData = timeSystem.GetSaveData();

            // 2. 保存子系统数据
            saveData.farmData = GetSubSystemSaveData<IFarmSaveable>(farmSystem);
            saveData.shopData = GetSubSystemSaveData<IShopSaveable>(shopSystem);
            saveData.npcData = GetSubSystemSaveData<INPCSaveable>(npcSystem);
            saveData.questData = GetSubSystemSaveData<IQuestSaveable>(questSystem);
            saveData.inventoryData = GetSubSystemSaveData<IInventorySaveable>(inventorySystem);
            saveData.playerData = GetSubSystemSaveData<IPlayerSaveable>(playerSystem);

            // 3. 设置元数据
            saveData.saveVersion = 1;
            saveData.saveTime = DateTime.Now;
            saveData.saveName = saveName;
            saveData.playTimeSeconds = PlayerPrefs.GetInt("PlayTime", 0);

            // 4. 序列化并保存
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(GetSaveFilePath(saveName), json);

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
        if (timeSystem == null)
        {
            Debug.LogError("[SaveManager] 时间系统未设置，无法保存！");
            yield break;
        }

        if (isLoading)
        {
            Debug.LogWarning("[SaveManager] 正在加载中，无法保存");
            yield break;
        }

        if (isSaving)
        {
            Debug.LogWarning("[SaveManager] 正在保存中，跳过本次保存");
            yield break;
        }

        isSaving = true;

        // 关键：暂停时间系统，避免保存期间时间继续流逝
        bool wasPaused = timeSystem.IsPaused;
        timeSystem.Pause();

        GameSaveData saveData = null;
        string json = null;
        Exception saveException = null;
        string filePath = GetSaveFilePath(saveName);

        // 状态机：0=准备, 1=时间数据, 2=农场数据, 3=商店数据, 4=NPC数据,
        // 5=任务数据, 6=背包数据, 7=玩家数据, 8=元数据, 9=序列化, 10=写入文件, 11=完成
        int step = 0;

        while (step <= 11)
        {
            try
            {
                switch (step)
                {
                    case 0: // 准备数据
                        saveData = new GameSaveData();
                        break;

                    case 1: // 保存时间数据
                        saveData.timeData = timeSystem.GetSaveData();
                        break;

                    case 2: // 保存农场数据
                        saveData.farmData = GetSubSystemSaveData<IFarmSaveable>(farmSystem);
                        break;

                    case 3: // 保存商店数据
                        saveData.shopData = GetSubSystemSaveData<IShopSaveable>(shopSystem);
                        break;

                    case 4: // 保存NPC数据
                        saveData.npcData = GetSubSystemSaveData<INPCSaveable>(npcSystem);
                        break;

                    case 5: // 保存任务数据
                        saveData.questData = GetSubSystemSaveData<IQuestSaveable>(questSystem);
                        break;

                    case 6: // 保存背包数据
                        saveData.inventoryData = GetSubSystemSaveData<IInventorySaveable>(inventorySystem);
                        break;

                    case 7: // 保存玩家数据
                        saveData.playerData = GetSubSystemSaveData<IPlayerSaveable>(playerSystem);
                        break;

                    case 8: // 设置元数据
                        saveData.saveVersion = 1;
                        saveData.saveTime = DateTime.Now;
                        saveData.saveName = saveName;
                        saveData.playTimeSeconds = PlayerPrefs.GetInt("PlayTime", 0);
                        break;

                    case 9: // 序列化
                        json = JsonUtility.ToJson(saveData, true);
                        break;

                    case 10: // 写入文件
                        File.WriteAllText(filePath, json);
                        break;

                    case 11: // 完成
                        Debug.Log($"[SaveManager] 异步保存完成: {saveName}");
                        break;
                }
            }
            catch (Exception e)
            {
                saveException = e;
                Debug.LogError($"[SaveManager] 异步保存失败 (步骤 {step}): {e.Message}");
                step = 12; // 跳到结束
            }

            step++;

            if (step <= 11)
            {
                yield return null;
            }
        }

        // 只在成功时触发保存完成事件
        if (saveException == null)
        {
            OnGameSaved?.Invoke(saveName);
        }

        // 恢复时间系统状态
        if (!wasPaused)
        {
            timeSystem.Resume();
        }

        isSaving = false;
        saveCoroutine = null;
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
            LoadSubSystemSaveData<IFarmSaveable>(farmSystem, saveData.farmData);
            LoadSubSystemSaveData<IShopSaveable>(shopSystem, saveData.shopData);
            LoadSubSystemSaveData<INPCSaveable>(npcSystem, saveData.npcData);
            LoadSubSystemSaveData<IQuestSaveable>(questSystem, saveData.questData);
            LoadSubSystemSaveData<IInventorySaveable>(inventorySystem, saveData.inventoryData);
            LoadSubSystemSaveData<IPlayerSaveable>(playerSystem, saveData.playerData);

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

    /// <summary>
    /// 获取子系统的存档数据
    /// </summary>
    private string GetSubSystemSaveData<T>(MonoBehaviour system) where T : class
    {
        if (system is T saveable)
        {
            var method = saveable.GetType().GetMethod("GetSaveData");
            if (method != null)
            {
                var data = method.Invoke(saveable, null);
                return JsonUtility.ToJson(data);
            }
        }
        return null;
    }

    /// <summary>
    /// 加载子系统的存档数据
    /// </summary>
    private void LoadSubSystemSaveData<T>(MonoBehaviour system, string jsonData) where T : class
    {
        if (system is T saveable && !string.IsNullOrEmpty(jsonData))
        {
            var method = saveable.GetType().GetMethod("LoadSaveData", new[] { typeof(string), typeof(GameTimeSystem) });
            if (method != null)
            {
                method.Invoke(saveable, new object[] { jsonData, timeSystem });
            }
        }
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
