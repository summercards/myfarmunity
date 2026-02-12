// Assets/Scripts/Editor/AutoSetupGame.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class AutoSetupGame
{
    [MenuItem("Tools/Auto Setup Game")]
    public static void Setup()
    {
        Debug.Log("=== Starting Auto Setup ===");

        // 1. Create Player Prefab
        Debug.Log("Creating Player Prefab...");
        PlayerSetupTool.CreatePlayerPrefab();

        // 2. Open game.unity
        string scenePath = "Assets/Scenes/game.unity";
        Debug.Log($"Opening scene: {scenePath}");
        Scene gameScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        if (!gameScene.IsValid())
        {
            Debug.LogError($"Failed to open scene: {scenePath}");
            return;
        }

        // 3. Configure GameStartManager
        Debug.Log("Configuring GameStartManager...");
        GameObject managerObj = GameObject.Find("GameStartManager");
        if (managerObj == null)
        {
            managerObj = new GameObject("GameStartManager");
            Undo.RegisterCreatedObjectUndo(managerObj, "Create GameStartManager");
        }

        GameStartManager manager = managerObj.GetComponent<GameStartManager>();
        if (manager == null)
        {
            manager = Undo.AddComponent<GameStartManager>(managerObj);
        }

        // 4. Assign Player Prefab
        string prefabPath = "Assets/Prefabs/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (playerPrefab != null)
        {
            manager.playerPrefab = playerPrefab;
            Debug.Log("Assigned Player Prefab.");
        }
        else
        {
            Debug.LogError($"Could not find Player Prefab at {prefabPath}");
        }

        // 5. Setup SpawnPoint
        GameObject spawnPointObj = GameObject.Find("SpawnPoint");
        if (spawnPointObj == null)
        {
            spawnPointObj = new GameObject("SpawnPoint");
            spawnPointObj.transform.position = new Vector3(0, 1, 0); // Default spawn height
            Undo.RegisterCreatedObjectUndo(spawnPointObj, "Create SpawnPoint");
        }
        manager.spawnPoint = spawnPointObj.transform;
        Debug.Log("Assigned Spawn Point.");

        // 6. Save Scene
        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);
        AssetDatabase.SaveAssets();

        Debug.Log("=== Auto Setup Complete! ===");
        EditorUtility.DisplayDialog("Auto Setup", "Game Setup Complete!\n\n1. Player Prefab Created.\n2. Game Scene Configured.\n\nYou can now run the game from main.unity.", "OK");
    }
}
#endif
