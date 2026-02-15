// Assets/Scripts/Editor/TimeUIMigrator.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// TimeUI 迁移工具
/// 从 cloudcity 场景复制 TimeCanvas 到 game 场景或创建预制体
/// </summary>
public class TimeUIMigrator : EditorWindow
{
    private const string CLOUDCITY_SCENE_PATH = "Assets/Scenes/cloudcity.scene";
    private const string GAME_SCENE_PATH = "Assets/Scenes/game.unity";
    private const string PREFAB_PATH = "Assets/Prefabs/UI/TimeCanvas.prefab";

    [MenuItem("Tools/TimeUI/复制 TimeCanvas 到 game 场景")]
    public static void CopyTimeCanvasToGameScene()
    {
        // 保存当前场景
        string originalScenePath = SceneManager.GetActiveScene().path;
        bool originalSceneWasDirty = SceneManager.GetActiveScene().isDirty;

        // 1. 打开 cloudcity 场景
        Scene cloudcityScene = EditorSceneManager.OpenScene(CLOUDCITY_SCENE_PATH, OpenSceneMode.Single);

        // 2. 查找 TimeCanvas
        GameObject timeCanvas = null;
        GameObject[] rootObjects = cloudcityScene.GetRootGameObjects();
        foreach (var obj in rootObjects)
        {
            if (obj.name == "TimeCanvas")
            {
                timeCanvas = obj;
                break;
            }
        }

        if (timeCanvas == null)
        {
            Debug.LogError("[TimeUIMigrator] 在 cloudcity 场景中找不到 TimeCanvas！");
            return;
        }

        // 3. 保存为预制体
        EnsureFolderExists("Assets/Prefabs/UI");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(timeCanvas, PREFAB_PATH);
        Debug.Log($"[TimeUIMigrator] TimeCanvas 预制体已创建: {PREFAB_PATH}");

        // 4. 打开 game 场景（添加模式）
        Scene gameScene = EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);

        // 5. 检查是否已存在 TimeCanvas
        bool exists = false;
        GameObject existingTimeCanvas = null;
        rootObjects = gameScene.GetRootGameObjects();
        foreach (var obj in rootObjects)
        {
            if (obj.name == "TimeCanvas")
            {
                exists = true;
                existingTimeCanvas = obj;
                break;
            }
        }

        if (exists)
        {
            if (EditorUtility.DisplayDialog("TimeCanvas 已存在",
                "game 场景中已存在 TimeCanvas，是否替换？",
                "替换", "取消"))
            {
                DestroyImmediate(existingTimeCanvas);
            }
            else
            {
                Debug.Log("[TimeUIMigrator] 操作已取消");
                return;
            }
        }

        // 6. 实例化预制体到 game 场景
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        SceneManager.MoveGameObjectToScene(instance, gameScene);

        // 7. 配置 TimeUI 组件引用
        ConfigureTimeUIReferences(instance);

        // 8. 标记场景为脏
        EditorSceneManager.MarkSceneDirty(gameScene);

        // 9. 询问是否保存
        if (EditorUtility.DisplayDialog("完成",
            $"TimeCanvas 已添加到 game 场景\n\n是否保存场景？",
            "保存", "稍后保存"))
        {
            EditorSceneManager.SaveScene(gameScene);
        }

        // 选中新创建的对象
        Selection.activeGameObject = instance;

        Debug.Log("[TimeUIMigrator] TimeCanvas 复制完成！");
    }

    [MenuItem("Tools/TimeUI/创建 TimeCanvas 预制体")]
    public static void CreateTimeCanvasPrefab()
    {
        // 保存当前场景状态
        Scene currentScene = SceneManager.GetActiveScene();
        bool wasDirty = currentScene.isDirty;

        // 打开 cloudcity 场景
        Scene cloudcityScene = EditorSceneManager.OpenScene(CLOUDCITY_SCENE_PATH, OpenSceneMode.Single);

        // 查找 TimeCanvas
        GameObject timeCanvas = null;
        GameObject[] rootObjects = cloudcityScene.GetRootGameObjects();
        foreach (var obj in rootObjects)
        {
            if (obj.name == "TimeCanvas")
            {
                timeCanvas = obj;
                break;
            }
        }

        if (timeCanvas == null)
        {
            Debug.LogError("[TimeUIMigrator] 在 cloudcity 场景中找不到 TimeCanvas！");
            return;
        }

        // 确保目录存在
        EnsureFolderExists("Assets/Prefabs/UI");

        // 保存为预制体
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(timeCanvas, PREFAB_PATH);

        // 选中新创建的预制体
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log($"[TimeUIMigrator] TimeCanvas 预制体已创建: {PREFAB_PATH}");
        EditorUtility.DisplayDialog("成功",
            $"TimeCanvas 预制体已创建:\n{PREFAB_PATH}\n\n" +
            "你可以将此预制体拖入任意场景使用。",
            "确定");
    }

    [MenuItem("Tools/TimeUI/验证 TimeUI 配置")]
    public static void ValidateTimeUISetup()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("提示", "请先选择一个包含 TimeUI 组件的 GameObject", "确定");
            return;
        }

        TimeUI timeUI = selected.GetComponent<TimeUI>();
        if (timeUI == null)
        {
            EditorUtility.DisplayDialog("错误", "选中的对象没有 TimeUI 组件", "确定");
            return;
        }

        // 检查配置
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("TimeUI 配置验证结果:\n");

        // 检查时间系统
        sb.AppendLine(timeUI.timeSystem != null ? "✓ 时间系统: 已配置" : "✗ 时间系统: 未配置");

        // 检查文本组件
        sb.AppendLine(timeUI.timeText != null ? "✓ 时间文本: 已配置" : "✗ 时间文本: 未配置");
        sb.AppendLine(timeUI.dateText != null ? "✓ 日期文本: 已配置" : "✗ 日期文本: 未配置");
        sb.AppendLine(timeUI.seasonText != null ? "✓ 季节文本: 已配置" : "✗ 季节文本: 未配置");
        sb.AppendLine(timeUI.timeOfDayText != null ? "✓ 时段文本: 已配置" : "✗ 时段文本: 未配置");
        sb.AppendLine(timeUI.weatherText != null ? "✓ 天气文本: 已配置" : "✗ 天气文本: 未配置");

        // 检查子对象
        int childCount = selected.transform.childCount;
        sb.AppendLine($"\n子对象数量: {childCount}");

        // 检查 Canvas 组件
        Canvas canvas = selected.GetComponent<Canvas>();
        sb.AppendLine(canvas != null ? "✓ Canvas 组件: 存在" : "○ Canvas 组件: 不存在（如果是子对象则正常）");

        // 自动修复
        bool needsFix = timeUI.timeSystem == null || timeUI.timeText == null;

        if (needsFix)
        {
            sb.AppendLine("\n需要修复部分配置。");
            if (EditorUtility.DisplayDialog("验证结果", sb.ToString(), "自动修复", "关闭"))
            {
                ConfigureTimeUIReferences(selected);
                EditorUtility.SetDirty(selected);
                Debug.Log("[TimeUIMigrator] TimeUI 配置已自动修复");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("验证结果", sb.ToString(), "确定");
        }
    }

    /// <summary>
    /// 配置 TimeUI 组件的引用
    /// </summary>
    private static void ConfigureTimeUIReferences(GameObject timeCanvas)
    {
        TimeUI timeUI = timeCanvas.GetComponent<TimeUI>();
        if (timeUI == null) return;

        SerializedObject so = new SerializedObject(timeUI);

        // 查找时间系统
        GameTimeSystem timeSystem = Resources.Load<GameTimeSystem>("DefaultTimeSystem");
        if (timeSystem != null)
        {
            so.FindProperty("timeSystem").objectReferenceValue = timeSystem;
        }

        // 查找子对象中的文本组件
        Transform t = timeCanvas.transform;
        for (int i = 0; i < t.childCount; i++)
        {
            Transform child = t.GetChild(i);
            TMP_Text text = child.GetComponent<TMP_Text>();

            if (text != null)
            {
                string name = child.name.ToLower();
                if (name.Contains("time") && !name.Contains("date") && !name.Contains("day"))
                {
                    so.FindProperty("timeText").objectReferenceValue = text;
                }
                else if (name.Contains("date"))
                {
                    so.FindProperty("dateText").objectReferenceValue = text;
                }
                else if (name.Contains("season"))
                {
                    so.FindProperty("seasonText").objectReferenceValue = text;
                }
                else if (name.Contains("timeofday") || name.Contains("day"))
                {
                    so.FindProperty("timeOfDayText").objectReferenceValue = text;
                }
                else if (name.Contains("weather"))
                {
                    so.FindProperty("weatherText").objectReferenceValue = text;
                }
            }
        }

        so.ApplyModifiedProperties();
        Debug.Log("[TimeUIMigrator] TimeUI 引用已配置");
    }

    /// <summary>
    /// 确保文件夹存在
    /// </summary>
    private static void EnsureFolderExists(string path)
    {
        string[] folders = path.Split('/');
        string currentPath = folders[0];

        for (int i = 1; i < folders.Length; i++)
        {
            if (!AssetDatabase.IsValidFolder(currentPath + "/" + folders[i]))
            {
                AssetDatabase.CreateFolder(currentPath, folders[i]);
            }
            currentPath += "/" + folders[i];
        }
    }
}
#endif
