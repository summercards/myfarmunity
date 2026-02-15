// Assets/Scripts/Editor/TimeUICreator.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TimeUI 编辑器工具
/// 用于快速创建时间显示 UI
/// </summary>
public class TimeUICreator : EditorWindow
{
    [MenuItem("Tools/Create TimeUI Prefab")]
    public static void CreateTimeUIPrefab()
    {
        // 创建根对象
        GameObject root = new GameObject("TimeUI_Panel");
        root.layer = LayerMask.NameToLayer("UI");

        // 添加 RectTransform
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1, 1);
        rootRect.anchorMax = new Vector2(1, 1);
        rootRect.pivot = new Vector2(1, 1);
        rootRect.anchoredPosition = new Vector2(-20, -20);
        rootRect.sizeDelta = new Vector2(250, 150);

        // 添加背景 Image
        Image bgImage = root.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);

        // 添加垂直布局组
        VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 12, 12);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // 添加 ContentSizeFitter
        ContentSizeFitter fitter = root.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 创建时间文本
        GameObject timeObj = CreateTextObject("TimeText", root, "00:00", 28, FontStyles.Bold, 38);
        TMP_Text timeText = timeObj.GetComponent<TMP_Text>();
        timeText.color = Color.white;

        // 分隔线
        GameObject divider = new GameObject("Divider");
        divider.layer = LayerMask.NameToLayer("UI");
        divider.transform.SetParent(root.transform, false);
        RectTransform dividerRect = divider.AddComponent<RectTransform>();
        dividerRect.sizeDelta = new Vector2(0, 2);
        LayoutElement dividerLayout = divider.AddComponent<LayoutElement>();
        dividerLayout.preferredHeight = 2;
        dividerLayout.flexibleWidth = 1;
        Image dividerImg = divider.AddComponent<Image>();
        dividerImg.color = new Color(1, 1, 1, 0.3f);

        // 创建日期文本
        GameObject dateObj = CreateTextObject("DateText", root, "2024年1月1日", 18, FontStyles.Normal, 26);
        TMP_Text dateText = dateObj.GetComponent<TMP_Text>();
        dateText.color = new Color(0.9f, 0.9f, 0.9f);

        // 创建季节文本
        GameObject seasonObj = CreateTextObject("SeasonText", root, "春季", 16, FontStyles.Normal, 24);
        TMP_Text seasonText = seasonObj.GetComponent<TMP_Text>();
        seasonText.color = new Color(0.5f, 0.8f, 0.5f);

        // 创建时间段文本
        GameObject timeOfDayObj = CreateTextObject("TimeOfDayText", root, "早晨", 16, FontStyles.Normal, 24);
        TMP_Text timeOfDayText = timeOfDayObj.GetComponent<TMP_Text>();
        timeOfDayText.color = new Color(1f, 0.8f, 0.5f);

        // 创建天气文本
        GameObject weatherObj = CreateTextObject("WeatherText", root, "晴天", 16, FontStyles.Normal, 24);
        TMP_Text weatherText = weatherObj.GetComponent<TMP_Text>();
        weatherText.color = new Color(0.5f, 0.7f, 1f);

        // 添加 TimeUI 组件
        TimeUI timeUI = root.AddComponent<TimeUI>();
        timeUI.timeText = timeText;
        timeUI.dateText = dateText;
        timeUI.seasonText = seasonText;
        timeUI.timeOfDayText = timeOfDayText;
        timeUI.weatherText = weatherText;

        // 尝试查找时间系统
        GameTimeSystem timeSystem = Resources.Load<GameTimeSystem>("DefaultTimeSystem");
        if (timeSystem != null)
        {
            SerializedObject so = new SerializedObject(timeUI);
            so.FindProperty("timeSystem").objectReferenceValue = timeSystem;
            so.ApplyModifiedProperties();
        }

        // 保存为预制体
        string prefabPath = "Assets/Prefabs/UI/TimeUI.prefab";

        // 确保目录存在
        string directory = System.IO.Path.GetDirectoryName(prefabPath);
        if (!AssetDatabase.IsValidFolder(directory))
        {
            string parent = "Assets";
            string[] folders = directory.Split('/');
            for (int i = 1; i < folders.Length; i++)
            {
                string currentPath = parent + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    AssetDatabase.CreateFolder(parent, folders[i]);
                }
                parent = currentPath;
            }
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

        // 销毁场景中的临时对象
        DestroyImmediate(root);

        // 选中新创建的预制体
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log($"[TimeUICreator] TimeUI 预制体已创建: {prefabPath}");
        EditorUtility.DisplayDialog("TimeUI 创建成功",
            $"TimeUI 预制体已创建在:\n{prefabPath}\n\n" +
            "使用方法:\n" +
            "1. 将预制体拖入场景中的 Canvas 下\n" +
            "2. 确保 Canvas 有 CanvasScaler 组件\n" +
            "3. 运行游戏即可看到时间显示",
            "确定");
    }

    private static GameObject CreateTextObject(string name, GameObject parent, string defaultText, int fontSize, FontStyles fontStyle, int preferredHeight = 0)
    {
        GameObject obj = new GameObject(name);
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent.transform, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, preferredHeight > 0 ? preferredHeight : fontSize + 12);

        TMP_Text text = obj.AddComponent<TextMeshProUGUI>();
        text.text = defaultText;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAlignmentOptions.Left;
        text.margin = new Vector4(5, 2, 5, 2);

        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight > 0 ? preferredHeight : fontSize + 12;
        layout.minHeight = fontSize + 8;

        return obj;
    }

    /// <summary>
    /// 在选中的 Canvas 下创建 TimeUI
    /// </summary>
    [MenuItem("GameObject/UI/TimeUI")]
    public static void CreateTimeUIInScene()
    {
        Canvas canvas = Selection.activeGameObject?.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = Object.FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            // 创建新的 Canvas
            GameObject canvasObj = new GameObject("Canvas");
            canvasObj.layer = LayerMask.NameToLayer("UI");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 创建 TimeUI
        GameObject timeUIObj = new GameObject("TimeUI");
        timeUIObj.layer = LayerMask.NameToLayer("UI");
        timeUIObj.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = timeUIObj.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1, 1);
        rootRect.anchorMax = new Vector2(1, 1);
        rootRect.pivot = new Vector2(1, 1);
        rootRect.anchoredPosition = new Vector2(-20, -20);
        rootRect.sizeDelta = new Vector2(250, 150);

        Image bgImage = timeUIObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);

        VerticalLayoutGroup layout = timeUIObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 12, 12);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = timeUIObj.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject timeObj = CreateTextObject("TimeText", timeUIObj, "00:00", 28, FontStyles.Bold, 38);
        TMP_Text timeText = timeObj.GetComponent<TMP_Text>();
        timeText.color = Color.white;

        // 分隔线
        GameObject divider = new GameObject("Divider");
        divider.layer = LayerMask.NameToLayer("UI");
        divider.transform.SetParent(timeUIObj.transform, false);
        RectTransform dividerRect = divider.AddComponent<RectTransform>();
        dividerRect.sizeDelta = new Vector2(0, 2);
        LayoutElement dividerLayout = divider.AddComponent<LayoutElement>();
        dividerLayout.preferredHeight = 2;
        dividerLayout.flexibleWidth = 1;
        Image dividerImg = divider.AddComponent<Image>();
        dividerImg.color = new Color(1, 1, 1, 0.3f);

        GameObject dateObj = CreateTextObject("DateText", timeUIObj, "2024年1月1日", 18, FontStyles.Normal, 26);
        TMP_Text dateText = dateObj.GetComponent<TMP_Text>();
        dateText.color = new Color(0.9f, 0.9f, 0.9f);

        GameObject seasonObj = CreateTextObject("SeasonText", timeUIObj, "春季", 16, FontStyles.Normal, 24);
        TMP_Text seasonText = seasonObj.GetComponent<TMP_Text>();
        seasonText.color = new Color(0.5f, 0.8f, 0.5f);

        GameObject timeOfDayObj = CreateTextObject("TimeOfDayText", timeUIObj, "早晨", 16, FontStyles.Normal, 24);
        TMP_Text timeOfDayText = timeOfDayObj.GetComponent<TMP_Text>();
        timeOfDayText.color = new Color(1f, 0.8f, 0.5f);

        GameObject weatherObj = CreateTextObject("WeatherText", timeUIObj, "晴天", 16, FontStyles.Normal, 24);
        TMP_Text weatherText = weatherObj.GetComponent<TMP_Text>();
        weatherText.color = new Color(0.5f, 0.7f, 1f);

        TimeUI timeUI = timeUIObj.AddComponent<TimeUI>();
        timeUI.timeText = timeText;
        timeUI.dateText = dateText;
        timeUI.seasonText = seasonText;
        timeUI.timeOfDayText = timeOfDayText;
        timeUI.weatherText = weatherText;

        // 尝试查找时间系统
        GameTimeSystem timeSystem = Resources.Load<GameTimeSystem>("DefaultTimeSystem");
        if (timeSystem != null)
        {
            SerializedObject so = new SerializedObject(timeUI);
            so.FindProperty("timeSystem").objectReferenceValue = timeSystem;
            so.ApplyModifiedProperties();
        }

        Selection.activeGameObject = timeUIObj;
        Undo.RegisterCreatedObjectUndo(timeUIObj, "Create TimeUI");

        Debug.Log("[TimeUICreator] TimeUI 已创建在 Canvas 下");
    }
}
#endif
