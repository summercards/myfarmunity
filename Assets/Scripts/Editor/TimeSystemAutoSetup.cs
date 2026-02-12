// Assets/Scripts/Editor/TimeSystemAutoSetup.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 时间系统自动配置编辑器
/// 自动创建所有必要的组件和配置
/// </summary>
public class TimeSystemAutoSetup : EditorWindow
{
    [MenuItem("Tools/时间系统/自动配置")]
    public static void ShowWindow()
    {
        GetWindow<TimeSystemAutoSetup>("时间系统自动配置");
    }

    void OnGUI()
    {
        GUILayout.Label("时间系统自动配置", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        GUILayout.Label("此工具将自动创建以下组件：");
        EditorGUILayout.HelpBox(
            "1. GameTimeSystem 资源\n" +
            "2. GameManager GameObject\n" +
            "3. SaveManager GameObject\n" +
            "4. TimeManager GameObject (含 TimeController)\n" +
            "5. Directional Light (含 DayNightCycle)",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("执行自动配置", GUILayout.Height(40)))
        {
            AutoSetup();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("重置配置 (删除后重新创建)", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("确认重置", "这将删除所有时间系统组件并重新创建。是否继续？", "确定", "取消"))
            {
                ResetSetup();
            }
        }
    }

    /// <summary>
    /// 自动配置
    /// </summary>
    private static void AutoSetup()
    {
        Debug.Log("=== 开始自动配置时间系统 ===");

        // 1. 创建或获取 GameTimeSystem 资源
        GameTimeSystem timeSystem = CreateOrGetTimeSystem();

        // 2. 创建 GameManager
        CreateGameManager();

        // 3. 创建 SaveManager
        CreateSaveManager();

        // 4. 创建 TimeManager (TimeController)
        CreateTimeManager(timeSystem);

        // 5. 创建 Directional Light 和 DayNightCycle
        CreateDayNightLight(timeSystem);

        // 6. 创建示例 UI
        CreateTimeUI(timeSystem);

        // 7. 创建示例农场系统
        CreateExampleFarmSystem();

        Debug.Log("=== 自动配置完成！ ===");
        Debug.Log("请查看场景中的组件，并根据需要进行调整。");
    }

    /// <summary>
    /// 重置配置
    /// </summary>
    private static void ResetSetup()
    {
        Debug.Log("=== 重置时间系统配置 ===");

        // 删除 GameObject
        DeleteGameObject("GameManager");
        DeleteGameObject("SaveManager");
        DeleteGameObject("TimeManager");
        DeleteGameObject("TimeUI");

        // 删除 Directional Light 上的 DayNightCycle
        GameObject[] lights = GameObject.FindObjectsOfType<GameObject>();
        foreach (var light in lights)
        {
            DayNightCycle dayNightCycle = light.GetComponent<DayNightCycle>();
            if (dayNightCycle != null)
            {
                DestroyImmediate(dayNightCycle);
                Debug.Log($"已删除 {light.name} 上的 DayNightCycle 组件");
            }
        }

        Debug.Log("=== 重置完成，请重新运行自动配置 ===");
    }

    private static void DeleteGameObject(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj != null)
        {
            DestroyImmediate(obj);
            Debug.Log($"已删除 GameObject: {name}");
        }
    }

    /// <summary>
    /// 创建或获取 GameTimeSystem 资源
    /// </summary>
    private static GameTimeSystem CreateOrGetTimeSystem()
    {
        // 检查 Resources 文件夹中是否已存在
        GameTimeSystem existingAsset = Resources.Load<GameTimeSystem>("DefaultTimeSystem");
        if (existingAsset != null)
        {
            Debug.Log("已找到 DefaultTimeSystem 资源");
            return existingAsset;
        }

        // 创建 Resources 文件夹
        string resourcesPath = "Assets/Resources";
        if (!Directory.Exists(resourcesPath))
        {
            Directory.CreateDirectory(resourcesPath);
        }

        // 创建 GameTimeSystem 资源
        GameTimeSystem timeSystem = ScriptableObject.CreateInstance<GameTimeSystem>();
        AssetDatabase.CreateAsset(timeSystem, "Assets/Resources/DefaultTimeSystem.asset");
        AssetDatabase.SaveAssets();

        // 配置默认值
        timeSystem.startHour = 6f;
        timeSystem.startDay = 1;
        timeSystem.startMonth = 1;
        timeSystem.startYear = 2024;
        timeSystem.startSeason = Season.Spring;
        timeSystem.daysPerSeason = 28;
        timeSystem.realSecondsPerGameMinute = 1f;
        timeSystem.currentWeather = WeatherType.Sunny;
        timeSystem.weatherChangeInterval = 6f;
        EditorUtility.SetDirty(timeSystem);
        AssetDatabase.SaveAssets();

        Debug.Log("已创建 DefaultTimeSystem 资源");
        return timeSystem;
    }

    /// <summary>
    /// 创建 GameManager
    /// </summary>
    private static void CreateGameManager()
    {
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager == null)
        {
            gameManager = new GameObject("GameManager");
        }

        GameManager manager = gameManager.GetComponent<GameManager>();
        if (manager == null)
        {
            manager = gameManager.AddComponent<GameManager>();
        }

        // 配置
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("autoLoadLatestSave").boolValue = true;
        so.FindProperty("defaultSaveName").stringValue = "autosave";
        so.FindProperty("initializeTimeSystemIfNoSave").boolValue = true;
        so.FindProperty("showStartupLog").boolValue = true;
        so.ApplyModifiedProperties();

        Debug.Log("已配置 GameManager");
    }

    /// <summary>
    /// 创建 SaveManager
    /// </summary>
    private static void CreateSaveManager()
    {
        GameObject saveManager = GameObject.Find("SaveManager");
        if (saveManager == null)
        {
            saveManager = new GameObject("SaveManager");
        }

        SaveManager manager = saveManager.GetComponent<SaveManager>();
        if (manager == null)
        {
            manager = saveManager.AddComponent<SaveManager>();
        }

        // 配置
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("maxSaveSlots").intValue = 10;
        so.FindProperty("saveFolder").stringValue = "saves";
        so.FindProperty("autoSaveEnabled").boolValue = true;
        so.FindProperty("autoSaveInterval").floatValue = 300f; // 5分钟
        so.ApplyModifiedProperties();

        Debug.Log("已配置 SaveManager");
    }

    /// <summary>
    /// 创建 TimeManager (TimeController)
    /// </summary>
    private static void CreateTimeManager(GameTimeSystem timeSystem)
    {
        GameObject timeManager = GameObject.Find("TimeManager");
        if (timeManager == null)
        {
            timeManager = new GameObject("TimeManager");
        }

        TimeController controller = timeManager.GetComponent<TimeController>();
        if (controller == null)
        {
            controller = timeManager.AddComponent<TimeController>();
        }

        // 配置
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("timeSystem").objectReferenceValue = timeSystem;
        so.FindProperty("autoInitialize").boolValue = true;
        so.FindProperty("autoStart").boolValue = true;
        so.FindProperty("allowTimeControl").boolValue = true;
        so.FindProperty("showDebugInfo").boolValue = true;
        so.ApplyModifiedProperties();

        Debug.Log("已配置 TimeManager");
    }

    /// <summary>
    /// 创建 Directional Light 和 DayNightCycle
    /// </summary>
    private static void CreateDayNightLight(GameTimeSystem timeSystem)
    {
        // 查找或创建 Directional Light
        Light directionalLight = Object.FindObjectOfType<Light>();
        if (directionalLight == null || directionalLight.type != LightType.Directional)
        {
            GameObject lightObj = new GameObject("Directional Light");
            directionalLight = lightObj.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Debug.Log("已创建 Directional Light");
        }

        // 添加 DayNightCycle
        DayNightCycle dayNightCycle = directionalLight.GetComponent<DayNightCycle>();
        if (dayNightCycle == null)
        {
            dayNightCycle = directionalLight.gameObject.AddComponent<DayNightCycle>();
        }

        // 配置
        SerializedObject so = new SerializedObject(dayNightCycle);
        so.FindProperty("timeSystem").objectReferenceValue = timeSystem;
        so.FindProperty("directionalLight").objectReferenceValue = directionalLight;
        so.FindProperty("sunriseHour").floatValue = 5f;
        so.FindProperty("noonHour").floatValue = 12f;
        so.FindProperty("sunsetHour").floatValue = 20f;
        so.FindProperty("maxLightIntensity").floatValue = 1.2f;
        so.FindProperty("minLightIntensity").floatValue = 0.1f;
        so.FindProperty("useFog").boolValue = true;
        so.ApplyModifiedProperties();

        Debug.Log("已配置 DayNightCycle");
    }

    /// <summary>
    /// 创建时间 UI
    /// </summary>
    private static void CreateTimeUI(GameTimeSystem timeSystem)
    {
        GameObject timeUI = GameObject.Find("TimeUI");
        if (timeUI == null)
        {
            timeUI = new GameObject("TimeUI");
        }

        TimeUI ui = timeUI.GetComponent<TimeUI>();
        if (ui == null)
        {
            ui = timeUI.AddComponent<TimeUI>();
        }

        // 配置
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("timeSystem").objectReferenceValue = timeSystem;
        so.FindProperty("showTime").boolValue = true;
        so.FindProperty("showDate").boolValue = true;
        so.FindProperty("showSeason").boolValue = true;
        so.FindProperty("showWeather").boolValue = true;
        so.FindProperty("showIcons").boolValue = false;
        so.FindProperty("use24HourFormat").boolValue = true;
        so.FindProperty("showSeconds").boolValue = false;
        so.FindProperty("updateInterval").floatValue = 0.5f;
        so.ApplyModifiedProperties();

        Debug.Log("已配置 TimeUI");
    }

    /// <summary>
    /// 创建示例农场系统
    /// </summary>
    private static void CreateExampleFarmSystem()
    {
        GameObject farmSystem = GameObject.Find("ExampleFarmSystem");
        if (farmSystem == null)
        {
            farmSystem = new GameObject("ExampleFarmSystem");
        }

        ExampleFarmSystem system = farmSystem.GetComponent<ExampleFarmSystem>();
        if (system == null)
        {
            system = farmSystem.AddComponent<ExampleFarmSystem>();
        }

        Debug.Log("已配置 ExampleFarmSystem");
    }
}

/// <summary>
/// 快速配置按钮
/// </summary>
public class QuickSetupButtons
{
    [MenuItem("Tools/时间系统/测试 - 快速保存")]
    public static void QuickSave()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame("autosave");
            Debug.Log("快速保存完成");
        }
        else
        {
            Debug.LogError("SaveManager 未初始化，请先运行自动配置");
        }
    }

    [MenuItem("Tools/时间系统/测试 - 快速加载")]
    public static void QuickLoad()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame("autosave");
            Debug.Log("快速加载完成");
        }
        else
        {
            Debug.LogError("SaveManager 未初始化，请先运行自动配置");
        }
    }

    [MenuItem("Tools/时间系统/测试 - 新游戏")]
    public static void NewGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.NewGame();
            Debug.Log("新游戏已开始");
        }
        else
        {
            Debug.LogError("GameManager 未初始化，请先运行自动配置");
        }
    }

    [MenuItem("Tools/时间系统/测试 - 查看存档列表")]
    public static void ViewSaves()
    {
        if (SaveManager.Instance != null)
        {
            var saves = SaveManager.Instance.GetSaveList();
            Debug.Log($"=== 存档列表 (共 {saves.Count} 个) ===");
            foreach (var save in saves)
            {
                Debug.Log($"存档: {save.saveName}");
                Debug.Log($"  时间: {save.saveTime}");
                Debug.Log($"  日期: {save.GetDisplayDate()}");
                Debug.Log($"  游戏时间: {save.GetDisplayTime()}");
                Debug.Log($"  季节: {save.season}");
                Debug.Log($"  天气: {save.weather}");
                Debug.Log($"  游玩时间: {save.GetPlayTimeString()}");
                Debug.Log($"---");
            }
        }
        else
        {
            Debug.LogError("SaveManager 未初始化，请先运行自动配置");
        }
    }

    [MenuItem("Tools/时间系统/测试 - 清空所有存档")]
    public static void ClearAllSaves()
    {
        if (SaveManager.Instance != null)
        {
            if (EditorUtility.DisplayDialog("确认清空", "这将删除所有存档！是否继续？", "确定", "取消"))
            {
                var saves = SaveManager.Instance.GetSaveList();
                int count = saves.Count;
                foreach (var save in saves)
                {
                    SaveManager.Instance.DeleteSave(save.saveName);
                }
                Debug.Log($"已清空 {count} 个存档");
            }
        }
        else
        {
            Debug.LogError("SaveManager 未初始化，请先运行自动配置");
        }
    }

    [MenuItem("Tools/时间系统/测试 - 打开存档文件夹")]
    public static void OpenSaveFolder()
    {
        string savePath = Path.Combine(Application.persistentDataPath, "saves");
        if (Directory.Exists(savePath))
        {
            EditorUtility.RevealInFinder(savePath);
        }
        else
        {
            Debug.Log("存档文件夹不存在: " + savePath);
        }
    }
}
#endif
