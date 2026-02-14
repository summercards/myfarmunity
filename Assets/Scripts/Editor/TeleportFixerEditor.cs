// Assets/Scripts/Editor/TeleportFixerEditor.cs
using UnityEngine;
using UnityEditor;

/// <summary>
/// 传送问题一键修复工具
/// </summary>
public class TeleportFixerEditor : EditorWindow
{
    private static TeleportFixerEditor window;

    [MenuItem("Tools/传送系统/修复传送问题")]
    public static void ShowWindow()
    {
        window = GetWindow<TeleportFixerEditor>("传送系统修复工具");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("传送系统修复工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "此工具帮助你修复传送后Player不见的问题。\n" +
            "请按照以下步骤操作：",
            MessageType.Info
        );

        EditorGUILayout.Space();

        GUILayout.Label("步骤1：修复当前场景", EditorStyles.boldLabel);

        if (GUILayout.Button("在当前场景添加修复组件", GUILayout.Height(30)))
        {
            AddFixerToCurrentScene();
        }

        if (GUILayout.Button("添加场景初始化器", GUILayout.Height(30)))
        {
            AddSceneInitializerToCurrentScene();
        }

        EditorGUILayout.Space();

        GUILayout.Label("步骤2：检查场景配置", EditorStyles.boldLabel);

        if (GUILayout.Button("检查SpawnPoint配置", GUILayout.Height(30)))
        {
            CheckSpawnPointConfiguration();
        }

        if (GUILayout.Button("检查Player配置", GUILayout.Height(30)))
        {
            CheckPlayerConfiguration();
        }

        if (GUILayout.Button("检查传送门配置", GUILayout.Height(30)))
        {
            CheckPortalConfiguration();
        }

        EditorGUILayout.Space();

        GUILayout.Label("步骤3：生成缺失对象", EditorStyles.boldLabel);

        if (GUILayout.Button("生成默认SpawnPoint", GUILayout.Height(30)))
        {
            GenerateDefaultSpawnPoint();
        }

        if (GUILayout.Button("生成Player出生点", GUILayout.Height(30)))
        {
            GeneratePlayerSpawnPoint();
        }

        EditorGUILayout.Space();

        GUILayout.Label("快速修复", EditorStyles.boldLabel);

        if (GUILayout.Button("一键修复所有问题", GUILayout.Height(40)))
        {
            QuickFixAll();
        }

        EditorGUILayout.Space();

        GUILayout.Label("说明", EditorStyles.label);
        EditorGUILayout.HelpBox(
            "修复步骤：\n" +
            "1. 在每个场景中添加修复组件\n" +
            "2. 检查SpawnPoint和Player配置\n" +
            "3. 生成缺失的SpawnPoint\n" +
            "4. 测试传送功能\n\n" +
            "修复组件说明：\n" +
            "- TeleportFixer: 自动修复传送后的Player问题\n" +
            "- SceneInitializer: 确保场景启动时Player正确生成",
            MessageType.None
        );
    }

    /// <summary>
    /// 在当前场景添加修复组件
    /// </summary>
    private void AddFixerToCurrentScene()
    {
        // 检查是否已有TeleportFixer
        TeleportFixer[] existingFixers = FindObjectsOfType<TeleportFixer>();
        if (existingFixers.Length > 0)
        {
            EditorUtility.DisplayDialog("提示", "当前场景中已有 TeleportFixer 组件", "确定");
            return;
        }

        // 创建TeleportFixer对象
        GameObject fixerGO = new GameObject("TeleportFixer");
        TeleportFixer fixer = fixerGO.AddComponent<TeleportFixer>();
        fixer.enableAutoFix = true;
        fixer.debugMode = true;

        // 放到合适的位置
        fixerGO.transform.position = Vector3.zero;

        Debug.Log("[TeleportFixerEditor] 已在当前场景添加 TeleportFixer");
        EditorUtility.DisplayDialog("成功", "已成功添加 TeleportFixer 到当前场景！", "确定");

        Selection.activeGameObject = fixerGO;
    }

    /// <summary>
    /// 添加场景初始化器
    /// </summary>
    private void AddSceneInitializerToCurrentScene()
    {
        // 检查是否已有SceneInitializer
        SceneInitializer[] existingInitializers = FindObjectsOfType<SceneInitializer>();
        if (existingInitializers.Length > 0)
        {
            EditorUtility.DisplayDialog("提示", "当前场景中已有 SceneInitializer 组件", "确定");
            return;
        }

        // 创建SceneInitializer对象
        GameObject initGO = new GameObject("SceneInitializer");
        SceneInitializer init = initGO.AddComponent<SceneInitializer>();
        init.debugMode = true;
        init.enableAutoFix = true;

        // 尝试查找Player Prefab
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        if (playerPrefab != null)
        {
            init.playerPrefab = playerPrefab;
            Debug.Log("[TeleportFixerEditor] 已设置 Player Prefab");
        }
        else
        {
            Debug.LogWarning("[TeleportFixerEditor] 未找到 Player Prefab，请手动设置");
        }

        // 查找默认出生点
        SpawnPoint[] spawnPoints = FindObjectsOfType<SpawnPoint>();
        if (spawnPoints.Length > 0)
        {
            init.defaultSpawnPoint = spawnPoints[0].transform;
            Debug.Log($"[TeleportFixerEditor] 已设置默认出生点: {spawnPoints[0].name}");
        }

        Debug.Log("[TeleportFixerEditor] 已在当前场景添加 SceneInitializer");
        EditorUtility.DisplayDialog("成功", "已成功添加 SceneInitializer 到当前场景！", "确定");

        Selection.activeGameObject = initGO;
    }

    /// <summary>
    /// 检查SpawnPoint配置
    /// </summary>
    private void CheckSpawnPointConfiguration()
    {
        SpawnPoint[] spawnPoints = FindObjectsOfType<SpawnPoint>(true);

        if (spawnPoints.Length == 0)
        {
            EditorUtility.DisplayDialog("检查结果", "场景中没有找到 SpawnPoint 对象！\n\n建议生成一个默认的 SpawnPoint。", "确定");
            return;
        }

        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine($"场景中有 {spawnPoints.Length} 个 SpawnPoint：\n");

        foreach (var spawnPoint in spawnPoints)
        {
            bool isActive = spawnPoint.gameObject.activeInHierarchy;
            string status = isActive ? "✓" : "✗";
            string spawnID = string.IsNullOrEmpty(spawnPoint.spawnID) ? "(未设置)" : spawnPoint.spawnID;

            report.AppendLine($"{status} {spawnPoint.gameObject.name}");
            report.AppendLine($"   SpawnID: {spawnID}");
            report.AppendLine($"   位置: {spawnPoint.transform.position}");
            report.AppendLine($"   启用状态: {isActive}");
            report.AppendLine();
        }

        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("SpawnPoint 检查结果", report.ToString(), "确定");
    }

    /// <summary>
    /// 检查Player配置
    /// </summary>
    private void CheckPlayerConfiguration()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0)
        {
            EditorUtility.DisplayDialog("检查结果", "场景中没有找到 Tag=Player 的对象！\n\n可能原因：\n1. Player 还未生成\n2. Player 的 Tag 设置不正确\n3. Player 对象被销毁", "确定");
            return;
        }

        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine($"场景中有 {players.Length} 个 Player：\n");

        foreach (var player in players)
        {
            bool isActive = player.activeInHierarchy;
            string status = isActive ? "✓" : "✗";

            report.AppendLine($"{status} {player.name}");
            report.AppendLine($"   位置: {player.transform.position}");
            report.AppendLine($"   启用状态: {isActive}");
            report.AppendLine($"   组件数量: {player.GetComponents<Component>().Length}");

            // 检查关键组件
            CharacterController cc = player.GetComponent<CharacterController>();
            TPSInput input = player.GetComponent<TPSInput>();

            report.AppendLine($"   CharacterController: {(cc != null ? "✓" : "✗")}");
            report.AppendLine($"   TPSInput: {(input != null ? "✓" : "✗")}");
            report.AppendLine();
        }

        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Player 检查结果", report.ToString(), "确定");
    }

    /// <summary>
    /// 检查传送门配置
    /// </summary>
    private void CheckPortalConfiguration()
    {
        Portal[] portals = FindObjectsOfType<Portal>(true);

        if (portals.Length == 0)
        {
            EditorUtility.DisplayDialog("检查结果", "场景中没有找到 Portal 对象！", "确定");
            return;
        }

        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine($"场景中有 {portals.Length} 个传送门：\n");

        foreach (var portal in portals)
        {
            bool isActive = portal.gameObject.activeInHierarchy;
            string status = isActive ? "✓" : "✗";
            string targetScene = string.IsNullOrEmpty(portal.targetScene) ? "(未设置)" : portal.targetScene;
            string targetSpawnID = string.IsNullOrEmpty(portal.targetSpawnID) ? "(未设置)" : portal.targetSpawnID;

            report.AppendLine($"{status} {portal.gameObject.name}");
            report.AppendLine($"   目标场景: {targetScene}");
            report.AppendLine($"   目标SpawnID: {targetSpawnID}");
            report.AppendLine($"   位置: {portal.transform.position}");
            report.AppendLine($"   启用状态: {isActive}");
            report.AppendLine();
        }

        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("传送门检查结果", report.ToString(), "确定");
    }

    /// <summary>
    /// 生成默认SpawnPoint
    /// </summary>
    private void GenerateDefaultSpawnPoint()
    {
        // 检查是否已有SpawnPoint
        SpawnPoint[] existingPoints = FindObjectsOfType<SpawnPoint>();
        if (existingPoints.Length > 0)
        {
            if (!EditorUtility.DisplayDialog("确认", "场景中已有 SpawnPoint，是否要生成新的？", "生成", "取消"))
            {
                return;
            }
        }

        // 获取当前场景名
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // 创建SpawnPoint
        GameObject spawnPointGO = new GameObject($"SpawnPoint_{sceneName}_Entry");
        spawnPointGO.transform.position = new Vector3(0, 1.5f, 0);

        SpawnPoint spawnPoint = spawnPointGO.AddComponent<SpawnPoint>();
        spawnPoint.spawnID = $"{sceneName}_Entry";

        Debug.Log($"[TeleportFixerEditor] 已生成 SpawnPoint: {spawnPoint.spawnID}");
        EditorUtility.DisplayDialog("成功", $"已成功生成 SpawnPoint：{spawnPoint.spawnID}\n\n位置：(0, 1.5, 0)", "确定");

        Selection.activeGameObject = spawnPointGO;
    }

    /// <summary>
    /// 生成Player出生点
    /// </summary>
    private void GeneratePlayerSpawnPoint()
    {
        // 创建出生点对象
        GameObject spawnPointGO = new GameObject("PlayerSpawnPoint");
        spawnPointGO.transform.position = new Vector3(0, 1.5f, 0);

        SpawnPoint spawnPoint = spawnPointGO.AddComponent<SpawnPoint>();
        spawnPoint.spawnID = "Default_Player_Spawn";

        Debug.Log("[TeleportFixerEditor] 已生成 Player 出生点");
        EditorUtility.DisplayDialog("成功", "已成功生成 Player 出生点！\n\n位置：(0, 1.5, 0)\nSpawnID: Default_Player_Spawn", "确定");

        Selection.activeGameObject = spawnPointGO;
    }

    /// <summary>
    /// 一键修复所有问题
    /// </summary>
    private void QuickFixAll()
    {
        if (!EditorUtility.DisplayDialog("确认", "即将执行以下操作：\n\n1. 添加 TeleportFixer 组件\n2. 添加 SceneInitializer 组件\n3. 检查 SpawnPoint 配置\n4. 生成默认 SpawnPoint（如果需要）\n5. 检查 Player 配置\n\n是否继续？", "继续", "取消"))
        {
            return;
        }

        Debug.Log("[TeleportFixerEditor] 开始一键修复...");

        // 1. 添加 TeleportFixer
        TeleportFixer[] existingFixers = FindObjectsOfType<TeleportFixer>();
        if (existingFixers.Length == 0)
        {
            GameObject fixerGO = new GameObject("TeleportFixer");
            TeleportFixer fixer = fixerGO.AddComponent<TeleportFixer>();
            fixer.enableAutoFix = true;
            fixer.debugMode = true;
            Debug.Log("[TeleportFixerEditor] ✓ 已添加 TeleportFixer");
        }
        else
        {
            Debug.Log("[TeleportFixerEditor] ✓ TeleportFixer 已存在，跳过");
        }

        // 2. 添加 SceneInitializer
        SceneInitializer[] existingInitializers = FindObjectsOfType<SceneInitializer>();
        if (existingInitializers.Length == 0)
        {
            GameObject initGO = new GameObject("SceneInitializer");
            SceneInitializer init = initGO.AddComponent<SceneInitializer>();
            init.debugMode = true;
            init.enableAutoFix = true;

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab != null)
            {
                init.playerPrefab = playerPrefab;
            }

            Debug.Log("[TeleportFixerEditor] ✓ 已添加 SceneInitializer");
        }
        else
        {
            Debug.Log("[TeleportFixerEditor] ✓ SceneInitializer 已存在，跳过");
        }

        // 3. 检查和生成 SpawnPoint
        SpawnPoint[] existingPoints = FindObjectsOfType<SpawnPoint>();
        if (existingPoints.Length == 0)
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            GameObject spawnPointGO = new GameObject($"SpawnPoint_{sceneName}_Entry");
            spawnPointGO.transform.position = new Vector3(0, 1.5f, 0);

            SpawnPoint spawnPoint = spawnPointGO.AddComponent<SpawnPoint>();
            spawnPoint.spawnID = $"{sceneName}_Entry";

            Debug.Log($"[TeleportFixerEditor] ✓ 已生成 SpawnPoint: {spawnPoint.spawnID}");
        }
        else
        {
            Debug.Log($"[TeleportFixerEditor] ✓ 已有 {existingPoints.Length} 个 SpawnPoint，跳过生成");
        }

        // 4. 检查 Player
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length > 0)
        {
            Debug.Log($"[TeleportFixerEditor] ✓ 找到 {players.Length} 个 Player");
        }
        else
        {
            Debug.LogWarning("[TeleportFixerEditor] ⚠ 未找到 Player，可能需要手动生成");
        }

        // 5. 检查传送门
        Portal[] portals = FindObjectsOfType<Portal>(true);
        Debug.Log($"[TeleportFixerEditor] ✓ 找到 {portals.Length} 个传送门");

        Debug.Log("[TeleportFixerEditor] 一键修复完成！");

        EditorUtility.DisplayDialog("修复完成",
            "一键修复已完成！\n\n" +
            "已添加的组件：\n" +
            "- TeleportFixer（自动修复传送问题）\n" +
            "- SceneInitializer（确保Player正确生成）\n" +
            "- SpawnPoint（如果需要）\n\n" +
            "请保存场景并测试传送功能！",
            "确定");
    }
}
