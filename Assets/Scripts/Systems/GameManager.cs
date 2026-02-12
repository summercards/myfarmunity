// Assets/Scripts/Systems/GameManager.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// 游戏启动管理器
/// 负责游戏初始化和存档加载
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;

    [Header("加载配置")]
    [Tooltip("游戏启动时是否自动加载最新存档")]
    public bool autoLoadLatestSave = true;

    [Tooltip("自动加载的存档名称（留空则加载最新存档）")]
    public string defaultSaveName = "autosave";

    [Header("时间系统配置")]
    [Tooltip("如果不存在存档，是否初始化时间系统")]
    public bool initializeTimeSystemIfNoSave = true;

    [Header("调试选项")]
    [Tooltip("显示启动日志")]
    public bool showStartupLog = true;

    [Tooltip("最大等待时间（秒）")]
    public float maxWaitTime = 10f;

    private bool isInitialized = false;
    private bool isInitializing = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            if (showStartupLog) Debug.Log("[GameManager] 游戏管理器已初始化");
        }
        else if (instance != this)
        {
            if (showStartupLog) Debug.Log("[GameManager] 已有游戏管理器实例，销毁此对象");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 使用协程等待组件初始化
        StartCoroutine(WaitForComponentsAndInitialize());
    }

    /// <summary>
    /// 等待所有组件初始化后再初始化游戏
    /// </summary>
    private IEnumerator WaitForComponentsAndInitialize()
    {
        if (isInitialized || isInitializing) yield break;

        isInitializing = true;
        float waitTime = 0f;

        if (showStartupLog) Debug.Log("[GameManager] 等待组件初始化...");

        // 等待 SaveManager
        while (SaveManager.Instance == null && waitTime < maxWaitTime)
        {
            yield return new WaitForSeconds(0.2f);
            waitTime += 0.2f;
        }

        if (SaveManager.Instance == null)
        {
            if (showStartupLog) Debug.LogError("[GameManager] SaveManager 不存在！请确保场景中有 SaveManager");
            isInitializing = false;
            yield break;
        }

        if (showStartupLog) Debug.Log("[GameManager] SaveManager 已就绪");

        // 等待时间系统（等待 TimeController 初始化完成）
        waitTime = 0f;
        while (TimeSystemAccessor.TimeSystem == null && waitTime < maxWaitTime)
        {
            yield return new WaitForSeconds(0.2f);
            waitTime += 0.2f;
        }

        if (TimeSystemAccessor.TimeSystem == null)
        {
            if (showStartupLog)
            {
                Debug.LogWarning("[GameManager] GameTimeSystem 未找到，尝试延迟初始化...");
                // 尝试直接从 Resources 加载
                GameTimeSystem system = Resources.Load<GameTimeSystem>("DefaultTimeSystem");
                if (system != null)
                {
                    if (showStartupLog) Debug.Log("[GameManager] 从 Resources 加载了 DefaultTimeSystem");
                }
                else
                {
                    Debug.LogError("[GameManager] GameTimeSystem 不存在！请确保场景中有 TimeController 且已配置 GameTimeSystem");
                    isInitializing = false;
                    yield break;
                }
            }
            else
            {
                isInitializing = false;
                yield break;
            }
        }

        if (showStartupLog) Debug.Log("[GameManager] GameTimeSystem 已就绪");

        // 所有组件就绪，开始初始化游戏
        yield return StartCoroutine(InitializeGame());
    }

    /// <summary>
    /// 初始化游戏
    /// </summary>
    private IEnumerator InitializeGame()
    {
        if (showStartupLog) Debug.Log("[GameManager] 开始初始化游戏...");

        yield return new WaitForSeconds(0.5f); // 额外等待确保所有 Start 执行完成

        // 尝试加载存档
        if (autoLoadLatestSave)
        {
            LoadLatestSave();
        }
        else if (!string.IsNullOrEmpty(defaultSaveName))
        {
            LoadSpecificSave(defaultSaveName);
        }
        else
        {
            // 不自动加载，确保时间系统已初始化
            if (initializeTimeSystemIfNoSave)
            {
                InitializeTimeSystem();
            }
        }

        isInitialized = true;
        isInitializing = false;

        if (showStartupLog) Debug.Log("[GameManager] 游戏初始化完成");
    }

    /// <summary>
    /// 加载最新存档
    /// </summary>
    private void LoadLatestSave()
    {
        var saveList = SaveManager.Instance.GetSaveList();

        if (saveList.Count > 0)
        {
            string latestSave = saveList[0].saveName;
            LoadSpecificSave(latestSave);
        }
        else
        {
            if (showStartupLog) Debug.Log("[GameManager] 没有找到存档，将初始化新的游戏");
            InitializeTimeSystem();
        }
    }

    /// <summary>
    /// 加载指定存档
    /// </summary>
    private void LoadSpecificSave(string saveName)
    {
        if (SaveManager.Instance.SaveExists(saveName))
        {
            if (showStartupLog) Debug.Log($"[GameManager] 正在加载存档: {saveName}");
            SaveManager.Instance.LoadGame(saveName);
        }
        else
        {
            if (showStartupLog) Debug.LogWarning($"[GameManager] 存档不存在: {saveName}，将初始化新的游戏");
            InitializeTimeSystem();
        }
    }

    /// <summary>
    /// 初始化时间系统（新游戏）
    /// </summary>
    private void InitializeTimeSystem()
    {
        // 检查时间系统是否已初始化
        if (TimeSystemAccessor.TimeSystem != null && TimeSystemAccessor.Hour > 0)
        {
            if (showStartupLog) Debug.Log("[GameManager] 时间系统已初始化，跳过");
            return;
        }

        if (showStartupLog) Debug.Log("[GameManager] 初始化时间系统（新游戏）");
        TimeSystemAccessor.TimeSystem.Initialize();
    }

    /// <summary>
    /// 手动加载存档
    /// </summary>
    public void LoadGame(string saveName)
    {
        LoadSpecificSave(saveName);
    }

    /// <summary>
    /// 开始新游戏
    /// </summary>
    public void NewGame()
    {
        if (showStartupLog) Debug.Log("[GameManager] 开始新游戏");
        InitializeTimeSystem();
    }

    /// <summary>
    /// 保存游戏
    /// </summary>
    public void SaveGame(string saveName = "autosave")
    {
        if (showStartupLog) Debug.Log($"[GameManager] 保存游戏: {saveName}");
        SaveManager.Instance.SaveGame(saveName);
    }

    /// <summary>
    /// 重新加载场景（用于测试）
    /// </summary>
    [ContextMenu("重新加载场景")]
    public void ReloadScene()
    {
        SaveGame("autosave");
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
