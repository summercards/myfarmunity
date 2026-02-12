#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public class SimplePlayerSetup
{
    [MenuItem("Tools/Simple Player Setup")]
    public static void SetupPlayer()
    {
        Debug.Log("=== Starting Simple Player Setup ===");
        
        // 1. Create Player Prefab (simple capsule)
        GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerObj.name = "Player";
        playerObj.transform.position = new Vector3(0, 1, 0);
        
        // Add necessary components
        if (playerObj.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = playerObj.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.mass = 60f;
        }
        
        // Add camera
        GameObject cameraObj = new GameObject("PlayerCamera");
        cameraObj.transform.SetParent(playerObj.transform);
        cameraObj.transform.localPosition = new Vector3(0, 0.6f, 0);
        cameraObj.AddComponent<Camera>();
        cameraObj.AddComponent<AudioListener>();
        
        // Save Prefab
        string prefabPath = "Assets/Prefabs/Player.prefab";
        if (!Directory.Exists("Assets/Prefabs"))
        {
            Directory.CreateDirectory("Assets/Prefabs");
        }
        
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(playerObj, prefabPath);
        Debug.Log($"[SimplePlayerSetup] Player Prefab created: {prefabPath}");
        
        // Delete temporary object
        Object.DestroyImmediate(playerObj);
        
        // 2. Open game.unity scene
        string gameScenePath = "Assets/Scenes/game.unity";
        Scene gameScene = EditorSceneManager.OpenScene(gameScenePath, OpenSceneMode.Single);
        Debug.Log($"Opened scene: {gameScenePath}");
        
        // 3. Clear old Player
        GameObject oldPlayer = GameObject.Find("Player");
        if (oldPlayer != null)
        {
            Object.DestroyImmediate(oldPlayer);
            Debug.Log("Deleted old Player GameObject");
        }
        
        // 4. Create/Configure GameStartManager
        GameObject managerObj = GameObject.Find("GameStartManager");
        if (managerObj == null)
        {
            managerObj = new GameObject("GameStartManager");
            Debug.Log("Created GameStartManager");
        }
        
        GameStartManager manager = managerObj.GetComponent<GameStartManager>();
        if (manager == null)
        {
            manager = managerObj.AddComponent<GameStartManager>();
            Debug.Log("Added GameStartManager component");
        }
        
        // 5. Set Player Prefab
        manager.playerPrefab = prefab;
        Debug.Log("Set Player Prefab");
        
        // 6. Set SpawnPoint
        GameObject spawnPoint = GameObject.Find("SpawnPoint");
        if (spawnPoint == null)
        {
            spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.position = new Vector3(0, 1, 0);
            Debug.Log("Created SpawnPoint (position: 0, 1, 0)");
        }
        manager.spawnPoint = spawnPoint.transform;
        
        // 7. Save scene
        EditorSceneManager.SaveScene(gameScene);
        Debug.Log("Saved game.unity scene");
        
        Debug.Log("=== Setup Complete! Now open main.unity to test ===");
        EditorUtility.DisplayDialog("Setup Complete", "Player setup complete!\n\nPlease open main.unity scene and click Play button to test.", "OK");
    }
}
#endif