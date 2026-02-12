// Assets/Scripts/RuntimeSceneSetup.cs
using UnityEngine;

/// <summary>
/// 运行时场景自动配置脚本
/// 将此脚本添加到场景中，运行一次后自动删除
/// 会创建所有时间系统所需的组件
/// </summary>
public class RuntimeSceneSetup : MonoBehaviour
{
    [Header("配置选项")]
    [Tooltip("是否在运行时自动配置")]
    public bool autoSetup = true;

    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = true;

    [Tooltip("配置完成后是否自动删除此脚本")]
    public bool deleteAfterSetup = true;

    private bool hasSetup = false;

    void Start()
    {
        if (autoSetup && !hasSetup)
        {
            SetupScene();
        }
    }

    /// <summary>
    /// 配置场景
    /// </summary>
    [ContextMenu("立即配置场景")]
    public void SetupScene()
    {
        if (hasSetup)
        {
            if (showDebugInfo) Debug.Log("[RuntimeSetup] 场景已配置过，跳过");
            return;
        }

        if (showDebugInfo) Debug.Log("=== 开始运行时场景配置 ===");

        // 1. 创建或获取 GameTimeSystem 资源
        GameTimeSystem timeSystem = GetOrCreateTimeSystem();

        // 2. 创建 GameManager
        CreateGameManager();

        // 3. 创建 SaveManager
        CreateSaveManager(timeSystem);

        // 4. 创建 TimeManager (TimeController)
        CreateTimeManager(timeSystem);

        // 5. 创建 Directional Light 和 DayNightCycle
        CreateDayNightLight(timeSystem);

        // 6. 创建 TimeUI（可选）
        CreateTimeUI(timeSystem);

        // 7. 创建示例农场系统
        CreateExampleFarmSystem();

        hasSetup = true;

        if (showDebugInfo)
        {
            Debug.Log("=== 场景配置完成！ ===");
            Debug.Log("已创建以下组件：");
            Debug.Log("  - GameManager");
            Debug.Log("  - SaveManager");
            Debug.Log("  - TimeManager");
            Debug.Log("  - Directional Light (DayNightCycle)");
            Debug.Log("  - TimeUI");
            Debug.Log("  - ExampleFarmSystem");
            Debug.Log("\n游戏启动后将自动加载存档。");
        }

        // 删除此脚本
        if (deleteAfterSetup)
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// 获取或创建 GameTimeSystem 资源
    /// </summary>
    private GameTimeSystem GetOrCreateTimeSystem()
    {
        // 首先尝试加载资源
        GameTimeSystem timeSystem = Resources.Load<GameTimeSystem>("DefaultTimeSystem");

        if (timeSystem != null)
        {
            if (showDebugInfo) Debug.Log("[RuntimeSetup] 已找到 DefaultTimeSystem 资源");
            return timeSystem;
        }

        // 如果没有，在场景中查找 ScriptableObject 实例
        GameTimeSystem[] instances = Resources.FindObjectsOfTypeAll<GameTimeSystem>();
        if (instances.Length > 0)
        {
            if (showDebugInfo) Debug.Log("[RuntimeSetup] 已找到 GameTimeSystem 实例");
            return instances[0];
        }

        // 创建临时实例
        if (showDebugInfo) Debug.LogWarning("[RuntimeSetup] 未找到 GameTimeSystem，创建临时实例");
        timeSystem = ScriptableObject.CreateInstance<GameTimeSystem>();
        timeSystem.startHour = 6f;
        timeSystem.startDay = 1;
        timeSystem.startMonth = 1;
        timeSystem.startYear = 2024;
        timeSystem.startSeason = Season.Spring;
        timeSystem.realSecondsPerGameMinute = 1f;

        return timeSystem;
    }

    /// <summary>
    /// 创建 GameManager
    /// </summary>
    private void CreateGameManager()
    {
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager == null)
        {
            gameManager = new GameObject("GameManager");
            if (showDebugInfo) Debug.Log("[RuntimeSetup] 创建了 GameManager");
        }

        GameManager manager = gameManager.GetComponent<GameManager>();
        if (manager == null)
        {
            manager = gameManager.AddComponent<GameManager>();
            if (showDebugInfo) Debug.Log("[RuntimeSetup] 添加了 GameManager 组件");
        }

        // 配置（直接赋值，因为字段是 public）
        manager.autoLoadLatestSave = true;
        manager.defaultSaveName = "autosave";
        manager.initializeTimeSystemIfNoSave = true;
        manager.showStartupLog = showDebugInfo;
    }

    /// <summary>
    /// 创建 SaveManager
    /// </summary>
    private void CreateSaveManager(GameTimeSystem timeSystem)
    {
        GameObject saveManager = GameObject.Find("SaveManager");
        if (saveManager == null)
        {
            saveManager = new GameObject("SaveManager");
            if (showDebugInfo) Debug.Log("[RuntimeSetup] 创建了 SaveManager");
        }

        SaveManager manager = saveManager.GetComponent<SaveManager>();
        if (manager == null)
        {
            manager = saveManager.AddComponent<SaveManager>();
            if (showDebugInfo) Debug.Log("[RuntimeSetup] 添加了 SaveManager 组件");
        }

        // 配置
        manager.timeSystem = timeSystem; // 修复：赋值时间系统引用
        manager.maxSaveSlots = 10;
        manager.saveFolder = "saves";
        manager.autoSaveEnabled = true;
        manager.autoSaveInterval = 300f;
    }

    /// <summary>
    /// 创建 TimeManager (TimeController)
    /// </summary>
    private void CreateTimeManager(GameTimeSystem timeSystem)
    {
        GameObject timeManager = GameObject.Find("TimeManager");
        if (timeManager == null)
        {
            timeManager = new GameObject("TimeManager");
            if (showDebugInfo) Debug.Log("[RuntimeSetup] 创建了 TimeManager");
        }

        TimeController controller = timeManager.GetComponent<TimeController>();
        if (controller == null)
        {
            controller = timeManager.AddComponent<TimeController>();
            if (showDebugInfo) Debug.Log("[RuntimeSetup] 添加了 TimeController 组件");
        }

        // 配置
        controller.timeSystem = timeSystem;
        controller.autoInitialize = true;
        controller.autoStart = true;
        controller.allowTimeControl = true;
        controller.showDebugInfo = showDebugInfo;
    }

    /// <summary>
    /// 创建 Directional Light 和 DayNightCycle
    /// </summary>
    private void CreateDayNightLight(GameTimeSystem timeSystem)
    {
        Light directionalLight = Object.FindObjectOfType<Light>();

        // 检查是否已有合适的 Directional Light
        if (directionalLight != null && directionalLight.type == LightType.Directional)
        {
            if (showDebugInfo) Debug.Log("[RuntimeSetup] 找到现有的 Directional Light");
        }
        else
        {
            // 创建新的 Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            directionalLight = lightObj.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            if (showDebugInfo) Debug.Log("[RuntimeSetup] 创建了 Directional Light");
        }

        // 添加或获取 DayNightCycle
        DayNightCycle dayNightCycle = directionalLight.GetComponent<DayNightCycle>();
        if (dayNightCycle == null)
        {
            dayNightCycle = directionalLight.gameObject.AddComponent<DayNightCycle>();
            if (showDebugInfo) Debug.Log("[RuntimeSetup] 添加了 DayNightCycle 组件");
        }

        // 配置
        dayNightCycle.timeSystem = timeSystem;
        dayNightCycle.directionalLight = directionalLight;
        dayNightCycle.sunriseHour = 5f;
        dayNightCycle.noonHour = 12f;
        dayNightCycle.sunsetHour = 20f;
        dayNightCycle.maxLightIntensity = 1.2f;
        dayNightCycle.minLightIntensity = 0.1f;
        dayNightCycle.useFog = true;
    }

    /// <summary>
    /// 创建时间 UI
    /// </summary>
    private void CreateTimeUI(GameTimeSystem timeSystem)
    {
        GameObject timeUI = GameObject.Find("TimeUI");
        if (timeUI == null)
        {
            timeUI = new GameObject("TimeUI");
            if (showDebugInfo) Debug.Log("[RuntimeSetup] 创建了 TimeUI");
        }

        TimeUI ui = timeUI.GetComponent<TimeUI>();
        if (ui == null)
        {
            ui = timeUI.AddComponent<TimeUI>();
            if (showDebugInfo) Debug.Log("[RuntimeSetup] 添加了 TimeUI 组件");
        }

        // 配置
        ui.timeSystem = timeSystem;
        ui.showTime = true;
        ui.showDate = true;
        ui.showSeason = true;
        ui.showWeather = true;
        ui.showIcons = false; // 需要图标时改为 true
        ui.use24HourFormat = true;
        ui.showSeconds = false;
        ui.updateInterval = 0.5f;
    }

    /// <summary>
    /// 创建示例农场系统
    /// </summary>
    private void CreateExampleFarmSystem()
    {
        GameObject farmSystem = GameObject.Find("ExampleFarmSystem");
        if (farmSystem == null)
        {
            farmSystem = new GameObject("ExampleFarmSystem");
            if (showDebugInfo) Debug.Log("[RuntimeSetup] 创建了 ExampleFarmSystem");
        }

        ExampleFarmSystem system = farmSystem.GetComponent<ExampleFarmSystem>();
        if (system == null)
        {
            system = farmSystem.AddComponent<ExampleFarmSystem>();
            if (showDebugInfo) Debug.Log("[RuntimeSetup] 添加了 ExampleFarmSystem 组件");
        }
    }
}
