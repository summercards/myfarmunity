// Assets/Scripts/Time/TimeSystemQuickSetup.cs
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 时间系统快速配置 - 运行时版本
/// 可以在运行时快速设置时间系统
/// </summary>
public class TimeSystemQuickSetup : MonoBehaviour
{
    [Header("自动配置")]
    [Tooltip("在 Awake 时自动配置")]
    public bool autoSetupOnAwake = true;

    [Tooltip("如果组件已存在则跳过")]
    public bool skipIfExists = true;

    void Awake()
    {
        if (autoSetupOnAwake)
        {
            QuickSetup();
        }
    }

    /// <summary>
    /// 快速配置时间系统
    /// </summary>
    [ContextMenu("快速配置时间系统")]
    public void QuickSetup()
    {
        Debug.Log("[时间系统] 开始快速配置...");

        // 1. 创建或查找 GameTimeSystem
        GameTimeSystem timeSystem = SetupTimeSystem();

        // 2. 创建或查找 TimeController
        TimeController controller = SetupTimeController(timeSystem);

        // 3. 配置昼夜循环
        SetupDayNightCycle(timeSystem);

        // 4. 创建时间UI
        SetupTimeUI(timeSystem);

        Debug.Log("[时间系统] 快速配置完成！");
    }

    /// <summary>
    /// 设置时间系统
    /// </summary>
    private GameTimeSystem SetupTimeSystem()
    {
        // 尝试从 Resources 加载
        GameTimeSystem timeSystem = Resources.Load<GameTimeSystem>("Game/DefaultTimeSystem");

        if (timeSystem == null)
        {
            // 创建新的实例（运行时创建 ScriptableObject）
            timeSystem = ScriptableObject.CreateInstance<GameTimeSystem>();
            timeSystem.realSecondsPerGameMinute = 1f;
            timeSystem.startHour = 6f;
            timeSystem.startDay = 1;
            timeSystem.startMonth = 1;
            timeSystem.startYear = 2024;
            timeSystem.startSeason = Season.Spring;
            timeSystem.currentWeather = WeatherType.Sunny;
            timeSystem.Initialize();

            Debug.Log("[时间系统] 创建了临时时间系统实例");
        }
        else
        {
            timeSystem.Initialize();
            Debug.Log("[时间系统] 加载了时间系统资源");
        }

        return timeSystem;
    }

    /// <summary>
    /// 设置时间控制器
    /// </summary>
    private TimeController SetupTimeController(GameTimeSystem timeSystem)
    {
        // 查找现有的
        TimeController controller = FindObjectOfType<TimeController>();

        if (controller != null)
        {
            if (skipIfExists)
            {
                Debug.Log("[时间系统] TimeController 已存在，跳过创建");
                controller.timeSystem = timeSystem;
                return controller;
            }
            else
            {
                // 移除现有的
                DestroyImmediate(controller.GetComponent<TimeController>());
            }
        }

        // 创建新的
        GameObject timeManager = new GameObject("TimeManager");
        timeManager.transform.SetParent(transform);

        controller = timeManager.AddComponent<TimeController>();
        controller.timeSystem = timeSystem;
        controller.allowTimeControl = true;
        controller.showDebugInfo = false;
        controller.autoInitialize = true;
        controller.autoStart = true;

        Debug.Log("[时间系统] 创建了 TimeManager");

        return controller;
    }

    /// <summary>
    /// 设置昼夜循环
    /// </summary>
    private void SetupDayNightCycle(GameTimeSystem timeSystem)
    {
        // 查找 Directional Light
        Light directionalLight = FindObjectOfType<Light>();

        if (directionalLight == null)
        {
            Debug.LogWarning("[时间系统] 未找到 Directional Light，跳过昼夜循环配置");
            return;
        }

        // 检查是否已有 DayNightCycle
        DayNightCycle dayNightCycle = directionalLight.GetComponent<DayNightCycle>();

        if (dayNightCycle != null)
        {
            if (skipIfExists)
            {
                Debug.Log("[时间系统] DayNightCycle 已存在，跳过创建");
                dayNightCycle.timeSystem = timeSystem;
                return;
            }
        }

        // 添加组件
        dayNightCycle = directionalLight.gameObject.AddComponent<DayNightCycle>();
        dayNightCycle.timeSystem = timeSystem;
        dayNightCycle.directionalLight = directionalLight;
        dayNightCycle.useFog = true;

        Debug.Log("[时间系统] 配置了昼夜循环");
    }

    /// <summary>
    /// 设置时间UI
    /// </summary>
    private void SetupTimeUI(GameTimeSystem timeSystem)
    {
        // 查找现有的
        TimeUI existingUI = FindObjectOfType<TimeUI>();

        if (existingUI != null)
        {
            if (skipIfExists)
            {
                Debug.Log("[时间系统] TimeUI 已存在，跳过创建");
                existingUI.timeSystem = timeSystem;
                return;
            }
            else
            {
                DestroyImmediate(existingUI);
            }
        }

        // 查找或创建 Canvas
        Canvas canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("TimeCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            Debug.Log("[时间系统] 创建了 Canvas");
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

        // 创建文本
        var timeText = CreateText(timePanel.transform, "TimeText", new Vector2(10, -10), new Vector2(180, 30), 28, "06:00");
        var dateText = CreateText(timePanel.transform, "DateText", new Vector2(10, -45), new Vector2(180, 25), 18, "2024年1月1日");
        var seasonText = CreateText(timePanel.transform, "SeasonText", new Vector2(10, -75), new Vector2(180, 20), 16, "春季");
        var weatherText = CreateText(timePanel.transform, "WeatherText", new Vector2(10, -100), new Vector2(180, 20), 16, "晴天");

        // 添加 TimeUI 组件
        TimeUI timeUI = canvas.gameObject.AddComponent<TimeUI>();
        timeUI.timeSystem = timeSystem;
        timeUI.timeText = timeText;
        timeUI.dateText = dateText;
        timeUI.seasonText = seasonText;
        timeUI.weatherText = weatherText;
        timeUI.showTime = true;
        timeUI.showDate = true;
        timeUI.showSeason = true;
        timeUI.showWeather = true;

        Debug.Log("[时间系统] 创建了时间UI");
    }

    /// <summary>
    /// 创建文本元素
    /// </summary>
    private TMPro.TextMeshProUGUI CreateText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, string text)
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
        textComp.text = text;
        textComp.enableAutoSizing = false;

        return textComp;
    }

    /// <summary>
    /// 清除所有时间系统相关对象
    /// </summary>
    [ContextMenu("清除所有时间系统")]
    public void ClearTimeSystem()
    {
        Debug.Log("[时间系统] 清除所有时间系统对象...");

        // 清除 TimeManager
        TimeController controller = FindObjectOfType<TimeController>();
        if (controller != null)
        {
            DestroyImmediate(controller.gameObject);
        }

        // 清除 DayNightCycle
        DayNightCycle[] dayNightCycles = FindObjectsOfType<DayNightCycle>();
        foreach (var dnc in dayNightCycles)
        {
            DestroyImmediate(dnc);
        }

        // 清除 TimeUI
        TimeUI[] timeUIs = FindObjectsOfType<TimeUI>();
        foreach (var ui in timeUIs)
        {
            if (ui.gameObject.name.Contains("Canvas") || ui.gameObject.name.Contains("Panel"))
            {
                DestroyImmediate(ui.gameObject);
            }
            else
            {
                DestroyImmediate(ui);
            }
        }

        // 清除本组件创建的对象
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        Debug.Log("[时间系统] 清除完成");
    }
}

#if UNITY_EDITOR
/// <summary>
/// 编辑器扩展：添加菜单项
/// </summary>
[CustomEditor(typeof(TimeSystemQuickSetup))]
public class TimeSystemQuickSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        EditorGUILayout.HelpBox("点击下方按钮快速配置时间系统", MessageType.Info);

        if (GUILayout.Button("🚀 快速配置", GUILayout.Height(30)))
        {
            (target as TimeSystemQuickSetup).QuickSetup();
        }

        if (GUILayout.Button("🗑️ 清除所有时间系统", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("确认", "确定要清除所有时间系统对象吗？", "确定", "取消"))
            {
                (target as TimeSystemQuickSetup).ClearTimeSystem();
            }
        }

        GUILayout.Space(10);

        EditorGUILayout.HelpBox("注意：清除操作会删除所有相关对象，请谨慎操作！", MessageType.Warning);
    }
}
#endif
