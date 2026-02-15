using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 场景修复器系统菜单
/// 提供快速访问所有场景工具的菜单入口
/// </summary>
public static class SceneFixersMenu
{
    [MenuItem("Tools/Scene Fixers/--- 场景创建 ---", false)]
    private static void Separator1() { }

    [MenuItem("Tools/Scene Fixers/创建默认场景模板", false, 1)]
    private static void CreateDefaultTemplate()
    {
        // 确保文件夹存在
        string folderPath = "Assets/SceneTemplates";
        if (!System.IO.Directory.Exists(Application.dataPath + "/SceneTemplates"))
        {
            System.IO.Directory.CreateDirectory(Application.dataPath + "/SceneTemplates");
            AssetDatabase.Refresh();
        }

        // 使用相对路径（Assets 开头）
        string path = "Assets/SceneTemplates/DefaultGameSceneTemplate.asset";

        // 如果文件已存在，先删除
        if (AssetDatabase.LoadAssetAtPath<SceneTemplate>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
        }

        SceneTemplate template = ScriptableObject.CreateInstance<SceneTemplate>();

        // 默认配置
        template.sceneName = "NewGameScene";
        template.scenePath = "Assets/Scenes/";
        template.playerNamePrefix = "Player";

        // 必需组件全部启用
        template.createPlayer = true;
        template.addPlayerInventoryHolder = true;
        template.addActiveItemController = true;
        template.addPlayerStats = true;
        template.addPlayerBuilder = true;
        template.addCharacterController = true;

        // 管理器全部启用
        template.createGameManager = true;
        template.createBuildSaveManager = true;
        template.createSaveManager = true;
        template.createTimeController = true;
        template.createPortalManager = true;

        // 环境配置
        template.createMainCamera = true;
        template.createLighting = true;
        template.createGround = true;

        AssetDatabase.CreateAsset(template, path);
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = template;
        Debug.Log($"[SceneFixers] 已创建默认模板: {path}");
    }

    [MenuItem("Tools/Scene Fixers/打开场景模板文件夹", false, 2)]
    private static void OpenTemplatesFolder()
    {
        string folderPath = Application.dataPath + "/SceneTemplates";
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }
        EditorUtility.RevealInFinder(folderPath);
    }

    [MenuItem("Tools/Scene Fixers/--- 场景检查 ---", false, 10)]
    private static void Separator2() { }

    [MenuItem("Tools/Scene Fixers/检查当前场景配置", false, 11)]
    private static void CheckCurrentScene()
    {
        SceneChecker.CheckAndDisplay();
    }

    [MenuItem("Tools/Scene Fixers/列出场景缺失组件", false, 12)]
    private static void ListMissingComponents()
    {
        SceneChecker.ListMissingComponents();
    }
}

/// <summary>
/// 场景检查器
/// 检查当前场景是否符合标准配置
/// </summary>
public class SceneChecker
{
    public static void CheckAndDisplay()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Debug.Log($"[SceneChecker] ===== 检查场景: {scene.name} =====");

        var issues = new System.Text.StringBuilder();
        int okCount = 0;

        // 检查玩家对象
        var player = GameObject.FindObjectOfType<PlayerInventoryHolder>();
        if (player == null)
        {
            issues.AppendLine("❌ 缺少: PlayerInventoryHolder");
        }
        else
        {
            okCount++;
            if (player.GetComponent<ActiveItemController>() == null)
            {
                issues.AppendLine("❌ Player对象缺少: ActiveItemController");
            }
            else
            {
                okCount++;
            }

            if (player.GetComponent<PlayerStats>() == null)
            {
                issues.AppendLine("❌ Player对象缺少: PlayerStats");
            }
            else
            {
                okCount++;
            }

            if (player.GetComponent<PlayerBuilder>() == null)
            {
                issues.AppendLine("⚠️  Player对象缺少: PlayerBuilder (可选)");
            }
            else
            {
                okCount++;
                var builder = player.GetComponent<PlayerBuilder>();
                if (builder.catalog == null)
                {
                    issues.AppendLine("❌ PlayerBuilder.catalog 未配置");
                }
                else
                {
                    okCount++;
                }
            }
        }

        // 检查管理器
        var managers = GameObject.FindObjectsOfType<BuildSaveManager>();
        if (managers == null || managers.Length == 0)
        {
            issues.AppendLine("❌ 缺少: BuildSaveManager");
        }
        else
        {
            okCount++;
        }

        var saveManagers = GameObject.FindObjectsOfType<SaveManager>();
        if (saveManagers == null || saveManagers.Length == 0)
        {
            issues.AppendLine("❌ 缺少: SaveManager");
        }
        else
        {
            okCount++;
        }

        var portalManagers = GameObject.FindObjectsOfType<PortalManager>();
        if (portalManagers == null || portalManagers.Length == 0)
        {
            issues.AppendLine("❌ 缺少: PortalManager");
        }
        else
        {
            okCount++;
        }

        // 检查相机
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            issues.AppendLine("❌ 缺少: Main Camera (tag: MainCamera)");
        }
        else
        {
            okCount++;
            if (mainCamera.GetComponent<AudioListener>() == null)
            {
                issues.AppendLine("⚠️  相机缺少: AudioListener");
            }
            else
            {
                okCount++;
            }
        }

        // 输出结果
        if (issues.Length > 0)
        {
            Debug.LogWarning($"[SceneChecker] 发现 {issues.Length} 个问题:\n{issues}");
            EditorUtility.DisplayDialog("场景检查结果",
                $"发现 {issues.Length} 个问题\n\n查看Console获取详细信息",
                "确定");
        }
        else
        {
            Debug.Log($"[SceneChecker] ✓ 场景配置完美! ({okCount} 项检查通过)");
        }
    }

    public static void ListMissingComponents()
    {
        Debug.Log("[SceneChecker] ===== 场景组件清单 =====");

        var components = new System.Collections.Generic.Dictionary<string, bool>();

        // 检查所有组件类型
        components["PlayerInventoryHolder"] = GameObject.FindObjectOfType<PlayerInventoryHolder>() != null;
        components["ActiveItemController"] = GameObject.FindObjectOfType<ActiveItemController>() != null;
        components["PlayerStats"] = GameObject.FindObjectOfType<PlayerStats>() != null;
        components["PlayerBuilder"] = GameObject.FindObjectOfType<PlayerBuilder>() != null;
        components["BuildSaveManager"] = GameObject.FindObjectOfType<BuildSaveManager>() != null;
        components["SaveManager"] = GameObject.FindObjectOfType<SaveManager>() != null;
        components["PortalManager"] = GameObject.FindObjectOfType<PortalManager>() != null;
        components["GameManager"] = GameObject.FindObjectOfType<GameManager>() != null;
        components["MainCamera"] = Camera.main != null;
        components["AudioListener"] = GameObject.FindObjectOfType<AudioListener>() != null;

        Debug.Log("组件状态:");
        foreach (var kvp in components)
        {
            string status = kvp.Value ? "✓" : "✗";
            Debug.Log($"  {status} {kvp.Key}");
        }

        int missing = 0;
        foreach (var kvp in components)
        {
            if (!kvp.Value) missing++;
        }

        if (missing > 0)
        {
            Debug.LogWarning($"[SceneChecker] 共缺少 {missing} 个组件");
        }
        else
        {
            Debug.Log("[SceneChecker] ✓ 所有组件齐全");
        }
    }
}
#endif
