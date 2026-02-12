// Assets/Scripts/Editor/PlayerSetupTool.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PlayerSetupTool : EditorWindow
{
    [MenuItem("Tools/角色/自动组装角色")]
    public static void ShowWindow()
    {
        GetWindow<PlayerSetupTool>("组装角色");
    }

    void OnGUI()
    {
        GUILayout.Label("角色自动组装器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("此工具将创建一个胶囊体，并尝试挂载所有带 'Player' 关键字的脚本。", MessageType.Info);

        if (GUILayout.Button("创建 Player 预制体"))
        {
            CreatePlayerPrefab();
        }
    }

    public static void CreatePlayerPrefab()
    {
        // 1. 创建基础物体
        GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerObj.name = "Player";
        playerObj.tag = "Player"; // 尝试设置 Tag
        playerObj.transform.position = new Vector3(0, 1, 0);

        // 2. 添加基础组件
        if (playerObj.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = playerObj.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.mass = 60f;
        }

        // 3. 查找并添加脚本
        string[] scriptGuids = AssetDatabase.FindAssets("Player t:MonoScript");
        List<string> addedScripts = new List<string>();

        foreach (string guid in scriptGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            
            // 排除编辑器脚本和一些不需要的脚本
            if (script != null && 
                !path.Contains("/Editor/") && 
                !script.GetClass().IsSubclassOf(typeof(EditorWindow)) &&
                !script.GetClass().IsAbstract)
            {
                // 尝试添加组件
                if (playerObj.GetComponent(script.GetClass()) == null)
                {
                    playerObj.AddComponent(script.GetClass());
                    addedScripts.Add(script.name);
                }
            }
        }

        // 4. 添加摄像机
        GameObject cameraObj = new GameObject("PlayerCamera");
        cameraObj.transform.SetParent(playerObj.transform);
        cameraObj.transform.localPosition = new Vector3(0, 0.6f, 0);
        cameraObj.AddComponent<Camera>();
        cameraObj.AddComponent<AudioListener>();

        // 5. 保存为 Prefab
        string prefabPath = "Assets/Prefabs/Player.prefab";
        if (!System.IO.Directory.Exists("Assets/Prefabs"))
        {
            System.IO.Directory.CreateDirectory("Assets/Prefabs");
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(playerObj, prefabPath);
        Debug.Log($"[PlayerSetup] 角色预制体已创建: {prefabPath}");
        Debug.Log($"[PlayerSetup] 已添加脚本: {string.Join(", ", addedScripts)}");

        // 删除场景中的临时对象
        DestroyImmediate(playerObj);

        // 选中新创建的 Prefab
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }
}
#endif
