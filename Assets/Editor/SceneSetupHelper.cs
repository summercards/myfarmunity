using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;

public class SceneSetupHelper
{
    [MenuItem("Tools/Setup Player From Scene To Game")]
    public static void SetupPlayerFromSceneToGame()
    {
        Debug.Log("=== 开始设置 Player Prefab 从 scene 到 game ===");
        
        // 步骤 1: 查找并打开 scene.scene
        string scenePath = "Assets/Scenes/scene.scene";
        if (!File.Exists(Path.Combine(Application.dataPath, "../" + scenePath)))
        {
            scenePath = "Assets/Scenes/scene.unity";
        }
        
        EditorSceneManager.SaveOpenScenes();
        
        if (!File.Exists(Path.Combine(Application.dataPath, "../" + scenePath)))
        {
            Debug.LogError($"找不到场景文件: {scenePath}");
            return;
        }
        
        Debug.Log($"加载场景: {scenePath}");
        Scene sceneScene = EditorSceneManager.OpenScene(scenePath);
        
        // 步骤 2: 查找 Player GameObject
        GameObject player = GameObject.Find("Player");
        
        if (player == null)
        {
            // 尝试在所有根物体中查找
            GameObject[] rootObjects = sceneScene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                if (root.name.Contains("Player") || root.name.Contains("player"))
                {
                    player = root;
                    Debug.Log($"找到 Player: {player.name}");
                    break;
                }
            }
        }
        
        if (player == null)
        {
            Debug.LogError("在 scene.scene 中找不到 Player GameObject！");
            EditorUtility.DisplayDialog("错误", "在 scene.scene 中找不到 Player GameObject！请确认场景中有名为 'Player' 的对象。", "确定");
            return;
        }
        
        Debug.Log($"找到 Player GameObject: {player.name}");
        
        // 步骤 3: 创建 Prefabs 文件夹
        string prefabFolderPath = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabFolderPath))
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Prefabs"));
            AssetDatabase.Refresh();
            Debug.Log($"创建文件夹: {prefabFolderPath}");
        }
        
        // 步骤 4: 创建 Player Prefab
        string prefabPath = $"{prefabFolderPath}/Player.prefab";
        bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
        
        if (prefabExists)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Prefabs 已存在",
                $"Player.prefab 已存在于 {prefabPath}，是否覆盖？",
                "覆盖", "取消"
            );
            
            if (!overwrite)
            {
                Debug.Log("用户取消操作，退出。");
                return;
            }
            
            AssetDatabase.DeleteAsset(prefabPath);
        }
        
        GameObject playerPrefab = PrefabUtility.SaveAsPrefabAsset(player, prefabPath);
        Debug.Log($"创建 Player Prefab: {prefabPath}");
        
        // 步骤 5: 从场景中删除 Player（因为现在有了 Prefab）
        Object.DestroyImmediate(player);
        EditorSceneManager.MarkSceneDirty(sceneScene);
        
        // 步骤 6: 保存 scene.scene
        EditorSceneManager.SaveScene(sceneScene);
        Debug.Log("保存 scene.scene");
        
        // 步骤 7: 打开 game.unity
        string gameScenePath = "Assets/Scenes/game.unity";
        Scene gameScene = EditorSceneManager.OpenScene(gameScenePath, OpenSceneMode.Single);
        Debug.Log($"加载场景: {gameScenePath}");
        
        // 步骤 8: 清除 game 场景中可能存在的旧 Player
        GameObject oldPlayer = GameObject.Find("Player");
        if (oldPlayer != null)
        {
            Debug.Log($"删除旧的 Player GameObject: {oldPlayer.name}");
            Object.DestroyImmediate(oldPlayer);
        }
        
        // 步骤 9: 查找或创建 GameStartManager
        GameObject gameStartManager = GameObject.Find("GameStartManager");
        if (gameStartManager == null)
        {
            gameStartManager = new GameObject("GameStartManager");
            Debug.Log("创建 GameStartManager GameObject");
        }
        
        // 步骤 10: 获取或添加 GameStartManager 组件
        GameStartManager manager = gameStartManager.GetComponent<GameStartManager>();
        if (manager == null)
        {
            manager = gameStartManager.AddComponent<GameStartManager>();
            Debug.Log("添加 GameStartManager 组件");
        }
        
        // 步骤 11: 设置 Player Prefab
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
        so.ApplyModifiedProperties();
        Debug.Log("设置 Player Prefab 到 GameStartManager");
        
        // 步骤 12: 配置 SpawnPoint
        GameObject spawnPoint = GameObject.Find("SpawnPoint");
        if (spawnPoint == null)
        {
            spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.position = new Vector3(0, 1, 0);
            Debug.Log("创建 SpawnPoint (位置: 0, 1, 0)");
        }
        
        so = new SerializedObject(manager);
        so.FindProperty("spawnPoint").objectReferenceValue = spawnPoint.transform;
        so.ApplyModifiedProperties();
        Debug.Log("设置 SpawnPoint 到 GameStartManager");
        
        // 步骤 13: 保存 game.unity
        EditorSceneManager.SaveScene(gameScene);
        Debug.Log("保存 game.unity");
        
        // 步骤 14: 检查 Build Settings
        BuildSettingsCheck();
        
        Debug.Log("=== 设置完成！现在可以打开 main.unity 进行测试了 ===");
        EditorUtility.DisplayDialog("设置完成", "Player Prefab 设置完成！\n\n请打开 main.unity 场景，点击播放按钮测试。", "确定");
    }
    
    private static void BuildSettingsCheck()
    {
        // 打开 Build Settings
        EditorApplication.ExecuteMenuItem("File/Build Settings...");
        
        // 这里我们只是记录信息，实际检查需要用户在 Build Settings 窗口中确认
        Debug.Log("请检查 Build Settings:");
        Debug.Log("- main.unity 应该在顶部 (Index 0)");
        Debug.Log("- game.unity 应该在列表中");
    }
}