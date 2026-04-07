// Assets/Scripts/Time/TimeSystemSetupWizard.cs
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
/// <summary>
/// 时间系统自动配置向导
/// 一键完成时间系统的完整配置
/// </summary>
namespace FarmGame.Editor.TimeTools
{
    public class TimeSystemSetupWizard : EditorWindow
    {
        private GameTimeSystem timeSystemAsset;
        private GameObject timeManagerObject;
        private bool showDetails = true;
        private Vector2 scrollPosition;

    [MenuItem("Tools/Time System/Setup Wizard")]
    public static void ShowWindow()
    {
        GetWindow<TimeSystemSetupWizard>("时间系统配置向导");
    }

    void OnGUI()
    {
        GUILayout.Label("🕐 游戏时间系统 - 自动配置向导", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // === 第一步：创建时间系统资源 ===
        EditorGUILayout.LabelField("第一步：创建时间系统资源", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("创建 GameTimeSystem 资源文件用于存储时间配置", MessageType.Info);

        if (timeSystemAsset == null)
        {
            EditorGUILayout.HelpBox("还未创建时间系统资源", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"已创建: {timeSystemAsset.name}", MessageType.Info);
        }

        if (GUILayout.Button("创建时间系统资源", GUILayout.Height(30)))
        {
            CreateTimeSystemAsset();
        }

        EditorGUILayout.Space();

        // === 第二步：创建时间控制器 ===
        EditorGUILayout.LabelField("第二步：创建时间控制器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("创建 TimeController 对象来管理时间运行", MessageType.Info);

        if (timeManagerObject == null)
        {
            EditorGUILayout.HelpBox("还未创建时间管理器", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"已创建: {timeManagerObject.name}", MessageType.Info);
        }

        if (GUILayout.Button("创建时间管理器", GUILayout.Height(30)))
        {
            CreateTimeManager();
        }

        EditorGUILayout.Space();

        // === 第三步：配置昼夜循环 ===
        EditorGUILayout.LabelField("第三步：配置昼夜循环", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("添加 DayNightCycle 组件到场景", MessageType.Info);

        if (GUILayout.Button("配置昼夜循环", GUILayout.Height(30)))
        {
            SetupDayNightCycle();
        }

        EditorGUILayout.Space();

        // === 第四步：创建时间UI ===
        EditorGUILayout.LabelField("第四步：创建时间UI", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("创建并配置时间显示界面", MessageType.Info);

        if (GUILayout.Button("创建时间UI", GUILayout.Height(30)))
        {
            CreateTimeUI();
        }

        EditorGUILayout.Space();

        // === 一键配置 ===
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("⚡ 快速配置", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("点击下方按钮一键完成所有配置", MessageType.None);

        if (GUILayout.Button("🚀 一键完整配置", GUILayout.Height(40)))
        {
            SetupEverything();
        }

        EditorGUILayout.Space();

        // === 详细信息 ===
        showDetails = EditorGUILayout.Foldout(showDetails, "显示详细信息");
        if (showDetails)
        {
            EditorGUILayout.LabelField("配置步骤说明:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. 创建 GameTimeSystem 资源");
            EditorGUILayout.LabelField("2. 创建 TimeManager 对象并添加 TimeController");
            EditorGUILayout.LabelField("3. 添加 DayNightCycle 到 Directional Light");
            EditorGUILayout.LabelField("4. 创建 Canvas 和 TimeUI 组件");

            EditorGUILayout.Space();

            if (timeSystemAsset != null)
            {
                EditorGUILayout.LabelField("时间系统配置:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"时间速度: {timeSystemAsset.realSecondsPerGameMinute}秒/分钟");
                EditorGUILayout.LabelField($"开始时间: {timeSystemAsset.startHour}:00");
                EditorGUILayout.LabelField($"开始日期: {timeSystemAsset.startYear}年{timeSystemAsset.startMonth}月{timeSystemAsset.startDay}日");
                EditorGUILayout.LabelField($"季节: {timeSystemAsset.startSeason}");
            }
        }

        EditorGUILayout.EndScrollView();

        // === 底部操作 ===
        EditorGUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("打开资源文件夹"))
        {
            EditorUtility.RevealInFinder("Assets");
        }

        if (GUILayout.Button("刷新场景"))
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 创建时间系统资源
    /// </summary>
    private void CreateTimeSystemAsset()
    {
        // 检查是否已存在
        var existingAsset = AssetDatabase.FindAssets("DefaultTimeSystem t:GameTimeSystem");
        if (existingAsset.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(existingAsset[0]);
            timeSystemAsset = AssetDatabase.LoadAssetAtPath<GameTimeSystem>(path);
            Debug.Log("[时间系统配置] 找到已存在的时间系统资源");
            EditorUtility.DisplayDialog("提示", "已找到时间系统资源", "确定");
            return;
        }

        // 创建新资源
        string folderPath = "Assets/Resources/Game";
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Game"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Game");
        }

        timeSystemAsset = ScriptableObject.CreateInstance<GameTimeSystem>();

        // 配置默认值
        timeSystemAsset.realSecondsPerGameMinute = 1f;
        timeSystemAsset.startHour = 6f;
        timeSystemAsset.startDay = 1;
        timeSystemAsset.startMonth = 1;
        timeSystemAsset.startYear = 2024;
        timeSystemAsset.startSeason = Season.Spring;
        timeSystemAsset.currentWeather = WeatherType.Sunny;

        // 保存资源
        string assetPath = $"{folderPath}/DefaultTimeSystem.asset";
        AssetDatabase.CreateAsset(timeSystemAsset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = timeSystemAsset;

        Debug.Log("[时间系统配置] 已创建时间系统资源: " + assetPath);
        EditorUtility.DisplayDialog("成功", "时间系统资源已创建！", "确定");
    }

    /// <summary>
    /// 创建时间管理器
    /// </summary>
    private void CreateTimeManager()
    {
        // 检查是否已存在
        timeManagerObject = GameObject.Find("TimeManager");
        if (timeManagerObject != null)
        {
            Debug.Log("[时间系统配置] TimeManager 已存在");
            EditorUtility.DisplayDialog("提示", "TimeManager 已存在", "确定");
            return;
        }

        // 创建对象
        timeManagerObject = new GameObject("TimeManager");

        // 添加 TimeController
        TimeController controller = timeManagerObject.AddComponent<TimeController>();
        controller.allowTimeControl = true;
        controller.showDebugInfo = true;
        controller.autoInitialize = true;
        controller.autoStart = true;

        // 连接时间系统资源
        if (timeSystemAsset != null)
        {
            controller.timeSystem = timeSystemAsset;
        }

        Undo.RegisterCreatedObjectUndo(timeManagerObject, "Create TimeManager");

        Debug.Log("[时间系统配置] 已创建 TimeManager");
        EditorUtility.DisplayDialog("成功", "时间管理器已创建！", "确定");
    }

    /// <summary>
    /// 配置昼夜循环
    /// </summary>
    private void SetupDayNightCycle()
    {
        // 查找 Directional Light
        Light directionalLight = FindObjectOfType<Light>();
        if (directionalLight == null || directionalLight.type != LightType.Directional)
        {
            // 创建新的 Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            directionalLight = lightObj.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            Undo.RegisterCreatedObjectUndo(lightObj, "Create Directional Light");
        }

        // 添加 DayNightCycle 组件
        DayNightCycle dayNightCycle = directionalLight.gameObject.GetComponent<DayNightCycle>();
        if (dayNightCycle == null)
        {
            dayNightCycle = directionalLight.gameObject.AddComponent<DayNightCycle>();
        }

        // 配置组件
        dayNightCycle.directionalLight = directionalLight;
        if (timeSystemAsset != null)
        {
            dayNightCycle.timeSystem = timeSystemAsset;
        }
        dayNightCycle.useFog = true;

        Undo.RegisterCompleteObjectUndo(dayNightCycle, "Setup DayNightCycle");

        Debug.Log("[时间系统配置] 已配置昼夜循环");
        EditorUtility.DisplayDialog("成功", "昼夜循环已配置！", "确定");
    }

    /// <summary>
    /// 创建时间UI
    /// </summary>
    private void CreateTimeUI()
    {
        // 检查是否已有 Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            // 创建 Canvas
            GameObject canvasObj = new GameObject("TimeCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // 配置 Canvas Scaler
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // 创建 EventSystem
            GameObject eventSystemObj = GameObject.Find("EventSystem");
            if (eventSystemObj == null)
            {
                eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        }

        // 创建时间面板
        GameObject timePanel = new GameObject("TimePanel");
        timePanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = timePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-10, -10);
        panelRect.sizeDelta = new Vector2(200, 150);

        Image panelImage = timePanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.5f);

        // 创建时间文本
        GameObject timeTextObj = CreateTextElement(timePanel.transform, "TimeText", new Vector2(10, -10), new Vector2(180, 30), 28);
        timeTextObj.GetComponent<TMPro.TextMeshProUGUI>().text = "06:00";

        // 创建日期文本
        GameObject dateTextObj = CreateTextElement(timePanel.transform, "DateText", new Vector2(10, -45), new Vector2(180, 25), 18);
        dateTextObj.GetComponent<TMPro.TextMeshProUGUI>().text = "2024年1月1日";

        // 创建季节文本
        GameObject seasonTextObj = CreateTextElement(timePanel.transform, "SeasonText", new Vector2(10, -75), new Vector2(180, 20), 16);
        seasonTextObj.GetComponent<TMPro.TextMeshProUGUI>().text = "春季";

        // 创建天气文本
        GameObject weatherTextObj = CreateTextElement(timePanel.transform, "WeatherText", new Vector2(10, -100), new Vector2(180, 20), 16);
        weatherTextObj.GetComponent<TMPro.TextMeshProUGUI>().text = "晴天";

        // 添加 TimeUI 组件
        TimeUI timeUI = canvas.gameObject.AddComponent<TimeUI>();
        timeUI.timeSystem = timeSystemAsset;
        timeUI.timeText = timeTextObj.GetComponent<TMPro.TextMeshProUGUI>();
        timeUI.dateText = dateTextObj.GetComponent<TMPro.TextMeshProUGUI>();
        timeUI.seasonText = seasonTextObj.GetComponent<TMPro.TextMeshProUGUI>();
        timeUI.weatherText = weatherTextObj.GetComponent<TMPro.TextMeshProUGUI>();
        timeUI.showTime = true;
        timeUI.showDate = true;
        timeUI.showSeason = true;
        timeUI.showWeather = true;

        Undo.RegisterCreatedObjectUndo(timePanel, "Create TimePanel");
        Undo.RegisterCreatedObjectUndo(timeUI, "Add TimeUI");

        Debug.Log("[时间系统配置] 已创建时间UI");
        EditorUtility.DisplayDialog("成功", "时间UI已创建！", "确定");
    }

    /// <summary>
    /// 创建文本元素
    /// </summary>
    private GameObject CreateTextElement(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var textComp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        textComp.fontSize = fontSize;
        textComp.alignment = TMPro.TextAlignmentOptions.Left;
        textComp.color = Color.white;
        textComp.enableAutoSizing = false;

        return textObj;
    }

    /// <summary>
    /// 一键完整配置
    /// </summary>
    private void SetupEverything()
    {
        EditorUtility.DisplayProgressBar("时间系统配置", "正在配置...", 0);

        try
        {
            // 步骤1
            EditorUtility.DisplayProgressBar("时间系统配置", "创建时间系统资源...", 0.2f);
            if (timeSystemAsset == null)
            {
                CreateTimeSystemAsset();
            }

            // 步骤2
            EditorUtility.DisplayProgressBar("时间系统配置", "创建时间管理器...", 0.4f);
            if (timeManagerObject == null)
            {
                CreateTimeManager();
            }

            // 步骤3
            EditorUtility.DisplayProgressBar("时间系统配置", "配置昼夜循环...", 0.6f);
            SetupDayNightCycle();

            // 步骤4
            EditorUtility.DisplayProgressBar("时间系统配置", "创建时间UI...", 0.8f);
            CreateTimeUI();

            // 完成
            EditorUtility.DisplayProgressBar("时间系统配置", "完成！", 1f);
            EditorUtility.ClearProgressBar();

            // 选择 TimeManager
            Selection.activeGameObject = timeManagerObject;

            // 显示成功信息
            string message = "时间系统配置完成！\n\n" +
                           "✅ GameTimeSystem 资源已创建\n" +
                           "✅ TimeManager 已创建\n" +
                           "✅ 昼夜循环已配置\n" +
                           "✅ 时间UI已创建\n\n" +
                           "点击 Play 按钮测试系统。\n" +
                           "使用快捷键控制时间：\n" +
                           "P: 暂停/恢复\n" +
                           "→: 前进1小时\n" +
                           "M: 跳到早晨";

            EditorUtility.DisplayDialog("配置完成", message, "确定");

            Debug.Log("[时间系统配置] 一键配置完成！");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[时间系统配置] 配置失败: " + e.Message);
            EditorUtility.DisplayDialog("错误", "配置失败: " + e.Message, "确定");
        }
    }

    void OnEnable()
    {
        // 自动查找已存在的资源
        if (timeSystemAsset == null)
        {
            var existingAsset = AssetDatabase.FindAssets("DefaultTimeSystem t:GameTimeSystem");
            if (existingAsset.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(existingAsset[0]);
                timeSystemAsset = AssetDatabase.LoadAssetAtPath<GameTimeSystem>(path);
            }
        }

        if (timeManagerObject == null)
        {
            timeManagerObject = GameObject.Find("TimeManager");
        }
    }
    }
}
#endif
