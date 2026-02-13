// Assets/Scripts/Portal/PortalQuickSetup.cs
using UnityEngine;
using UnityEditor;

/// <summary>
/// 传送门快速设置 - 场景配置菜单
/// </summary>
public class PortalQuickSetup : EditorWindow
{
    [MenuItem("Tools/传送门/场景传送快速设置")]
    public static void ShowWindow()
    {
        GetWindow<PortalQuickSetup>("传送门场景设置");
    }

    private void OnGUI()
    {
        GUILayout.Label("传送门场景快速设置", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "此工具帮助您快速设置场景之间的传送系统。\n" +
            "请在场景中创建传送门并设置目标场景和SpawnID。",
            MessageType.Info
        );

        EditorGUILayout.Space();

        GUILayout.Label("快速操作", EditorStyles.boldLabel);

        if (GUILayout.Button("打开传送门编辑器", GUILayout.Height(30)))
        {
            PortalEditor.ShowWindow();
            Close();
        }

        if (GUILayout.Button("在 main 场景创建传送门到 game 场景"))
        {
            CreatePortalFromMainToGame();
        }

        if (GUILayout.Button("在 game 场景创建传送门到 main 场景"))
        {
            CreatePortalFromGameToMain();
        }

        EditorGUILayout.Space();

        GUILayout.Label("说明", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "传送门工作原理:\n" +
            "1. 在场景中放置传送门对象\n" +
            "2. 设置传送门的目标场景名称\n" +
            "3. 设置目标SpawnID（对应目标场景中SpawnPoint的spawnID）\n" +
            "4. 在目标场景中创建对应的SpawnPoint\n" +
            "5. 玩家进入传送门后自动传送\n\n" +
            "提示:\n" +
            "- 确保玩家对象有 'Player' 标签\n" +
            "- 确保目标场景已添加到 Build Settings\n" +
            "- 每个传送门都需要一个对应的SpawnPoint",
            MessageType.None
        );
    }

    /// <summary>
    /// 在 main 场景创建传送门到 game 场景
    /// </summary>
    private static void CreatePortalFromMainToGame()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (currentScene != "main")
        {
            if (!EditorUtility.DisplayDialog("确认",
                $"当前场景是 '{currentScene}'，是否继续创建到 'game' 场景的传送门？",
                "继续", "取消"))
            {
                return;
            }
        }

        CreatePortal("传送门_to_game", "game", "game_Entry", new Vector3(-3, 0, 0));
    }

    /// <summary>
    /// 在 game 场景创建传送门到 main 场景
    /// </summary>
    private static void CreatePortalFromGameToMain()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (currentScene != "game")
        {
            if (!EditorUtility.DisplayDialog("确认",
                $"当前场景是 '{currentScene}'，是否继续创建到 'main' 场景的传送门？",
                "继续", "取消"))
            {
                return;
            }
        }

        CreatePortal("传送门_to_main", "main", "main_Entry", new Vector3(0, 0, 0));
    }

    /// <summary>
    /// 创建传送门
    /// </summary>
    private static void CreatePortal(string name, string targetScene, string targetSpawnID, Vector3 position)
    {
        GameObject portalObj = new GameObject(name);
        portalObj.transform.position = position;

        // 添加碰撞体
        SphereCollider collider = portalObj.AddComponent<SphereCollider>();
        collider.radius = 1f;
        collider.isTrigger = true;

        // 添加传送门脚本
        Portal portal = portalObj.AddComponent<Portal>();
        portal.targetScene = targetScene;
        portal.targetSpawnID = targetSpawnID;

        // 添加视觉效果生成器
        PortalVisualGenerator visualGen = portalObj.AddComponent<PortalVisualGenerator>();
        visualGen.portalColor = new Color(0f, 0.8f, 1f, 0.5f);
        visualGen.portalSize = 2f;

        // 立即生成视觉效果
        visualGen.GenerateVisuals();

        // 选中新建的传送门
        Selection.activeGameObject = portalObj;

        Debug.Log($"[PortalQuickSetup] 已创建传送门: {name} -> {targetScene} (SpawnID: {targetSpawnID})");

        // 提示用户检查 Build Settings
        if (!IsSceneInBuildSettings(targetScene))
        {
            EditorUtility.DisplayDialog("提示",
                $"目标场景 '{targetScene}' 尚未添加到 Build Settings！\n" +
                "请确保将其添加到 File > Build Settings。",
                "确定");
        }
    }

    /// <summary>
    /// 检查场景是否在 Build Settings 中
    /// </summary>
    private static bool IsSceneInBuildSettings(string sceneName)
    {
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (System.IO.Path.GetFileNameWithoutExtension(scene.path) == sceneName)
            {
                return true;
            }
        }
        return false;
    }
}
