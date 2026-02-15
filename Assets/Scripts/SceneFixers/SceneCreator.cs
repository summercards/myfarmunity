using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 场景创建器
/// 根据SceneTemplate配置一键生成完整的新场景
/// 使用方法：选择一个SceneTemplate资源，然后点击 Inspector 中的 "Create Scene" 按钮
/// </summary>
public class SceneCreator : MonoBehaviour
{
    [Header("快速创建")]
    [Tooltip("选择场景模板")]
    public SceneTemplate template;

    [Tooltip("创建后立即打开")]
    public bool openAfterCreate = true;

    /// <summary>
    /// 创建场景（上下文菜单）
    /// </summary>
    [ContextMenu("Create Scene From Template")]
    public void CreateScene()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("[SceneCreator] 请在编辑器模式下运行，不要在Play模式执行！");
            return;
        }

        if (template == null)
        {
            Debug.LogError("[SceneCreator] 请先选择一个 SceneTemplate！");
            return;
        }

        CreateSceneFromTemplate(template);
    }

    /// <summary>
    /// 从模板创建场景
    /// </summary>
    public static GameObject CreateSceneFromTemplate(SceneTemplate config)
    {
        if (config == null)
        {
            Debug.LogError("[SceneCreator] 配置为空！");
            return null;
        }

        Debug.Log($"[SceneCreator] ===== 开始创建场景: {config.sceneName} =====");

        // 重置计数器
        int objCount = 0;
        int compCount = 0;
        GameObject playerObj = null;

        // 保存当前场景
        string currentScene = SceneManager.GetActiveScene().path;

        // 删除旧场景（如果存在）
        string scenePath = config.scenePath + config.sceneName + ".unity";
        if (config.deleteOldScene && System.IO.File.Exists(scenePath))
        {
            Debug.Log($"[SceneCreator] 删除旧场景: {scenePath}");
            AssetDatabase.DeleteAsset(scenePath);
        }

        // 创建新场景
        // Unity 2022.3+ 使用新的 API
        Scene newScene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);
        List<GameObject> createdObjects = new List<GameObject>();

        // === 创建基础环境 ===
        if (config.createLighting)
        {
            // 定向光
            GameObject lightObj = new GameObject("Directional Light");
            Light dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            createdObjects.Add(lightObj);
            objCount++;

            // 环境光
            // Unity 2022.3+ 不再需要设置 ambientMode
            RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.45f);
            RenderSettings.ambientIntensity = 1f;

            // 雾效
            RenderSettings.fog = true;
            RenderSettings.fogMode = config.fogMode;
            RenderSettings.fogColor = config.fogColor;
            RenderSettings.fogDensity = config.fogDensity;

            // 天空盒
            if (config.skyboxMaterial != null)
            {
                RenderSettings.skybox = config.skyboxMaterial;
            }
            compCount++;
        }

        // === 创建地面 ===
        if (config.createGround)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = config.groundScale;
            ground.transform.position = Vector3.zero;
            ground.layer = LayerMask.NameToLayer("Ground");

            // 移除碰撞体（可选）
            // Collider groundCol = ground.GetComponent<Collider>();
            // if (groundCol != null) DestroyImmediate(groundCol);

            createdObjects.Add(ground);
            objCount++;
        }

        // === 创建玩家 ===
        if (config.createPlayer)
        {
            playerObj = new GameObject(config.playerNamePrefix);
            playerObj.tag = config.playerNamePrefix;
            playerObj.transform.position = config.playerSpawnPosition;
            playerObj.transform.rotation = Quaternion.Euler(config.playerSpawnRotation);

            // 必需组件
            if (config.addCharacterController)
            {
                CharacterController charCtrl = playerObj.AddComponent<CharacterController>();
                charCtrl.height = 2f;
                charCtrl.radius = 0.5f;
                charCtrl.center = new Vector3(0, 1f, 0);
                compCount++;
            }

            if (config.addPlayerStats)
            {
                PlayerStats stats = playerObj.AddComponent<PlayerStats>();
                compCount++;
            }

            if (config.addPlayerInventoryHolder)
            {
                PlayerInventoryHolder inv = playerObj.AddComponent<PlayerInventoryHolder>();
                compCount++;
            }

            if (config.addActiveItemController)
            {
                ActiveItemController active = playerObj.AddComponent<ActiveItemController>();
                compCount++;
            }

            if (config.addPlayerBuilder)
            {
                PlayerBuilder builder = playerObj.AddComponent<PlayerBuilder>();
                compCount++;
            }

            createdObjects.Add(playerObj);
            objCount++;
            Debug.Log($"[SceneCreator] ✓ Player对象创建完成");
        }

        // === 创建管理器对象 ===
        GameObject managers = new GameObject("--- Managers ---");

        if (config.createGameManager)
        {
            GameObject gm = new GameObject("GameManager");
            gm.transform.parent = managers.transform;
            GameManager gameManager = gm.AddComponent<GameManager>();
            createdObjects.Add(gm);
            objCount++;
        }

        if (config.createSaveManager)
        {
            GameObject sm = new GameObject("SaveManager");
            sm.transform.parent = managers.transform;
            SaveManager saveManager = sm.AddComponent<SaveManager>();
            createdObjects.Add(sm);
            objCount++;
        }

        if (config.createBuildSaveManager)
        {
            GameObject bm = new GameObject("BuildSaveManager");
            bm.transform.parent = managers.transform;
            BuildSaveManager buildManager = bm.AddComponent<BuildSaveManager>();
            createdObjects.Add(bm);
            objCount++;
        }

        if (config.createTimeController)
        {
            GameObject tc = new GameObject("TimeController");
            tc.transform.parent = managers.transform;
            TimeController timeCtrl = tc.AddComponent<TimeController>();
            createdObjects.Add(tc);
            objCount++;
        }

        if (config.createPortalManager)
        {
            GameObject pm = new GameObject("PortalManager");
            pm.transform.parent = managers.transform;
            PortalManager portalManager = pm.AddComponent<PortalManager>();
            createdObjects.Add(pm);
            objCount++;
        }

        Debug.Log($"[SceneCreator] ✓ 管理器对象创建完成");

        // === 创建相机 ===
        if (config.createMainCamera)
        {
            // 检查场景中是否已有 Main Camera，避免重复创建
            Camera existingCamera = Camera.main;
            GameObject camObj = null;

            if (existingCamera != null)
            {
                // 复用现有相机
                camObj = existingCamera.gameObject;
                Debug.Log($"[SceneCreator] 复用现有 Main Camera: {camObj.name}");

                // 更新相机位置
                camObj.transform.position = config.cameraPosition;
            }
            else
            {
                // 没有现有相机，创建新的
                camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                camObj.transform.position = config.cameraPosition;
                Camera cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.backgroundColor = new Color(0.5f, 0.6f, 0.7f);
                cam.farClipPlane = 1000f;

                FlareLayer flareLayer = camObj.AddComponent<FlareLayer>();

                // 检查场景中是否已有 AudioListener，避免重复
                AudioListener existingListener = FindObjectOfType<AudioListener>();
                if (existingListener == null)
                {
                    AudioListener listener = camObj.AddComponent<AudioListener>();
                    Debug.Log("[SceneCreator] ✓ 添加 AudioListener");
                }
                else
                {
                    Debug.Log("[SceneCreator] AudioListener 已存在，跳过添加");
                }

                createdObjects.Add(camObj);
                objCount++;
                compCount += 2;
                Debug.Log($"[SceneCreator] ✓ 创建新 Main Camera");
            }
        }

        // === 配置引用 ===
        if (playerObj != null)
        {
            // 配置PlayerBuilder的引用
            PlayerBuilder builder = playerObj.GetComponent<PlayerBuilder>();
            if (builder != null && config.buildCatalog != null)
            {
                builder.catalog = config.buildCatalog;
                Debug.Log($"[SceneCreator] ✓ PlayerBuilder.catalog 已配置");
            }

            // 配置PlayerInventoryHolder的引用
            PlayerInventoryHolder inv = playerObj.GetComponent<PlayerInventoryHolder>();
            if (inv != null && config.itemDatabase != null)
            {
                inv.itemDB = config.itemDatabase;
                Debug.Log($"[SceneCreator] ✓ PlayerInventoryHolder.itemDB 已配置");
            }
        }

        // === 保存场景 ===
        string finalScenePath = config.scenePath + config.sceneName + ".unity";

        // 确保目录存在
        string folderPath = config.scenePath.Replace("Assets/", Application.dataPath + "/");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(folderPath));

        bool saved = EditorSceneManager.SaveScene(newScene, finalScenePath);
        if (!saved)
        {
            Debug.LogError($"[SceneCreator] 场景保存失败: {finalScenePath}");
        }
        else
        {
            Debug.Log($"[SceneCreator] ✓ 场景已保存: {finalScenePath}");
        }

        // 添加到Build Settings（如果需要）
        AddSceneToBuildSettings(finalScenePath);

        // 刷新Asset Database
        AssetDatabase.Refresh();

        // === 输出摘要 ===
        Debug.Log($"[SceneCreator] ===== 场景创建完成 =====");
        Debug.Log($"[SceneCreator] 创建对象数: {objCount}");
        Debug.Log($"[SceneCreator] 添加组件数: {compCount}");
        Debug.Log($"[SceneCreator] 场景路径: {finalScenePath}");

        // 返回玩家对象（用于后续引用）
        return playerObj;
    }

    /// <summary>
    /// 添加场景到Build Settings
    /// </summary>
    private static void AddSceneToBuildSettings(string scenePath)
    {
        // 查找Build Settings中是否已存在
        var scenes = EditorBuildSettings.scenes;
        foreach (var scene in scenes)
        {
            if (scene.path == scenePath)
            {
                Debug.Log($"[SceneCreator] 场景已在Build Settings中");
                return;
            }
        }

        // 添加到Build Settings
        var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        System.Array.Copy(scenes, newScenes, scenes.Length);
        // Unity 2022.3+ 构造函数需要两个参数：路径和启用状态
        newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;
        Debug.Log($"[SceneCreator] ✓ 场景已添加到Build Settings");
    }
}

#region Editor Menu
/// <summary>
/// 场景创建器编辑器菜单
/// </summary>
public class SceneCreatorMenu
{
    [MenuItem("Tools/Scene Fixers/Create Scene from Selected Template", false, 10)]
    private static void CreateSceneFromSelection()
    {
        SceneTemplate template = Selection.activeObject as SceneTemplate;
        if (template == null)
        {
            EditorUtility.DisplayDialog("未选择模板",
                "请先选择一个 SceneTemplate 资源，然后再次点击此菜单项。",
                "确定");
            return;
        }

        GameObject player = SceneCreator.CreateSceneFromTemplate(template);
        if (player != null)
        {
            Selection.activeGameObject = player;
        }
    }

    [MenuItem("Tools/Scene Fixers/Create Scene from Selected Template", true)]
    private static bool ValidateCreateSceneFromSelection()
    {
        return Selection.activeObject is SceneTemplate;
    }

    [MenuItem("Tools/Scene Fixers/Create Default Scene Template", false, 11)]
    private static void CreateDefaultTemplate()
    {
        string folderPath = "Assets/SceneTemplates";
        if (!System.IO.Directory.Exists(Application.dataPath + "/SceneTemplates"))
        {
            System.IO.Directory.CreateDirectory(Application.dataPath + "/SceneTemplates");
            AssetDatabase.Refresh();
        }

        string path = folderPath + "/DefaultGameSceneTemplate.asset";
        SceneTemplate template = ScriptableObject.CreateInstance<SceneTemplate>();
        AssetDatabase.CreateAsset(template, path);
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = template;
        Debug.Log($"[SceneCreator] 已创建默认模板: {path}");
    }

    [MenuItem("Tools/Scene Fixers/Open Scene Templates Folder", false, 20)]
    private static void OpenTemplatesFolder()
    {
        string folderPath = Application.dataPath + "/SceneTemplates";
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }
        EditorUtility.RevealInFinder(folderPath);
    }
}

#endregion

#endif
