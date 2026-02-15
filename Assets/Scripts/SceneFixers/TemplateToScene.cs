using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 模板应用工具
/// 将场景模板的内容应用到当前打开的场景
/// 使用方法：
/// 1. 创建或打开一个场景
/// 2. 选择一个 SceneTemplate 资源
/// 3. 点击 Tools > Scene Fixers > 应用模板到当前场景
/// </summary>
public class TemplateToScene : EditorWindow
{
    private SceneTemplate selectedTemplate;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Scene Fixers/应用模板到当前场景", false, 5)]
    public static void ShowWindow()
    {
        var window = GetWindow<TemplateToScene>("应用场景模板");
        window.titleContent = new GUIContent("场景模板");
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "将选中的模板内容应用到当前打开的场景\n" +
            "不会删除现有对象，只添加缺失的部分",
            MessageType.Info);

        EditorGUILayout.Space();

        // 模板选择器
        EditorGUILayout.LabelField("选择场景模板:", EditorStyles.boldLabel);
        selectedTemplate = (SceneTemplate)EditorGUILayout.ObjectField(
            GUIContent.none,
            selectedTemplate,
            typeof(SceneTemplate),
            false);

        EditorGUILayout.Space(10);

        // 显示当前场景信息
        Scene currentScene = EditorSceneManager.GetActiveScene();
        EditorGUILayout.LabelField($"当前场景: {currentScene.name}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"场景路径: {currentScene.path}", EditorStyles.miniLabel);

        EditorGUILayout.Space(10);

        // 应用按钮
        GUI.enabled = selectedTemplate != null;
        if (GUILayout.Button("应用模板到当前场景", GUILayout.Height(30)))
        {
            ApplyTemplateToScene(selectedTemplate);
        }
        GUI.enabled = true;

        // 显示模板信息
        if (selectedTemplate != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("模板配置:", EditorStyles.boldLabel);
            using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scroll.scrollPosition;
                ShowTemplateInfo(selectedTemplate);
            }
        }
    }

    void ShowTemplateInfo(SceneTemplate template)
    {
        EditorGUILayout.LabelField($"场景名称: {template.sceneName}");
        EditorGUILayout.LabelField($"玩家前缀: {template.playerNamePrefix}");
        EditorGUILayout.LabelField($"玩家出生: {template.playerSpawnPosition}");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("需要创建的组件:", EditorStyles.boldLabel);
        ShowBoolItem("Player", template.createPlayer);
        ShowBoolItem("GameManager", template.createGameManager);
        ShowBoolItem("SaveManager", template.createSaveManager);
        ShowBoolItem("TimeController", template.createTimeController);
        ShowBoolItem("Main Camera", template.createMainCamera);
        ShowBoolItem("光照", template.createLighting);
        ShowBoolItem("地面", template.createGround);
    }

    void ShowBoolItem(string name, bool value)
    {
        EditorGUILayout.LabelField($"  {(value ? "✓" : "✗")} {name}");
    }

    /// <summary>
    /// 应用模板到当前场景
    /// </summary>
    public static void ApplyTemplateToScene(SceneTemplate template)
    {
        if (template == null)
        {
            Debug.LogError("[TemplateToScene] 请先选择一个模板！");
            return;
        }

        Scene scene = EditorSceneManager.GetActiveScene();
        Debug.Log($"[TemplateToScene] ===== 应用模板到场景: {scene.name} =====");

        int createdCount = 0;
        int skippedCount = 0;

        // === 创建玩家 ===
        if (template.createPlayer)
        {
            GameObject player = FindOrCreatePlayer(template, ref createdCount, ref skippedCount);
            if (player != null)
            {
                AddPlayerComponents(player, template, ref createdCount);
            }
        }

        // === 创建管理器 ===
        CreateManagers(template, ref createdCount, ref skippedCount);

        // === 创建相机 ===
        if (template.createMainCamera)
        {
            CreateOrSkip<Camera>("Main Camera", ref createdCount, ref skippedCount);
        }

        // === 创建环境 ===
        if (template.createLighting)
        {
            CreateLighting(template, ref createdCount, ref skippedCount);
        }

        if (template.createGround)
        {
            CreateGround(template, ref createdCount, ref skippedCount);
        }

        // === 完成 ===
        Debug.Log($"[TemplateToScene] ✓ 创建了 {createdCount} 个对象");
        if (skippedCount > 0)
        {
            Debug.Log($"[TemplateToScene] ⊘ 跳过了 {skippedCount} 个已存在的对象");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[TemplateToScene] 场景已更新");
    }

    static GameObject FindOrCreatePlayer(SceneTemplate template, ref int created, ref int skipped)
    {
        // 尝试查找现有玩家
        GameObject[] roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        GameObject player = null;

        foreach (var root in roots)
        {
            if (root.name.StartsWith(template.playerNamePrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                player = root;
                break;
            }
        }

        if (player == null)
        {
            player = new GameObject(template.playerNamePrefix);
            player.tag = template.playerNamePrefix;
            player.transform.position = template.playerSpawnPosition;
            player.transform.rotation = Quaternion.Euler(template.playerSpawnRotation);
            created++;
            Debug.Log($"[TemplateToScene] ✓ 创建 Player: {player.name}");
        }
        else
        {
            skipped++;
            Debug.Log($"[TemplateToScene] ⊘ Player 已存在: {player.name}");
        }

        return player;
    }

    static void AddPlayerComponents(GameObject player, SceneTemplate template, ref int created)
    {
        // PlayerInventoryHolder
        if (template.addPlayerInventoryHolder)
        {
            CreateOrSkipComponent<PlayerInventoryHolder>(player, "PlayerInventoryHolder", ref created);
        }

        // ActiveItemController
        if (template.addActiveItemController)
        {
            CreateOrSkipComponent<ActiveItemController>(player, "ActiveItemController", ref created);
        }

        // PlayerStats
        if (template.addPlayerStats)
        {
            CreateOrSkipComponent<PlayerStats>(player, "PlayerStats", ref created);
        }

        // PlayerBuilder
        if (template.addPlayerBuilder)
        {
            var builder = CreateOrSkipComponent<PlayerBuilder>(player, "PlayerBuilder", ref created);
            if (builder != null && template.sceneName != null)
            {
                Debug.Log("[TemplateToScene]   提示: 需要手动配置 PlayerBuilder.catalog");
            }
        }

        // CharacterController
        if (template.addCharacterController)
        {
            var controller = CreateOrSkipComponent<CharacterController>(player, "CharacterController", ref created);
            if (controller != null)
            {
                controller.height = 2f;
                controller.radius = 0.5f;
                controller.center = new Vector3(0, 1f, 0);
            }
        }
    }

    static void CreateManagers(SceneTemplate template, ref int created, ref int skipped)
    {
        string managerParentName = "--- Managers ---";
        GameObject managerParent = GameObject.Find(managerParentName);

        if (managerParent == null)
        {
            managerParent = new GameObject(managerParentName);
            created++;
            Debug.Log($"[TemplateToScene] ✓ 创建管理器父对象");
        }
        else
        {
            skipped++;
        }

        if (template.createGameManager)
            CreateManagerChild<GameManager>(managerParent, "GameManager", ref created);

        if (template.createSaveManager)
            CreateManagerChild<SaveManager>(managerParent, "SaveManager", ref created);

        if (template.createBuildSaveManager)
            CreateManagerChild<BuildSaveManager>(managerParent, "BuildSaveManager", ref created);

        if (template.createTimeController)
            CreateManagerChild<TimeController>(managerParent, "TimeController", ref created);

        if (template.createPortalManager)
            CreateManagerChild<PortalManager>(managerParent, "PortalManager", ref created);
    }

    static void CreateManagerChild<T>(GameObject parent, string name, ref int created) where T : Component
    {
        Transform existing = parent.transform.Find(name);
        if (existing == null)
        {
            GameObject go = new GameObject(name);
            go.transform.parent = parent.transform;
            go.AddComponent<T>();
            created++;
            Debug.Log($"[TemplateToScene]   ✓ 创建 {typeof(T).Name}: {name}");
        }
    }

    static void CreateLighting(SceneTemplate template, ref int created, ref int skipped)
    {
        // 定向光
        CreateOrSkip<Light>("Directional Light", ref created, ref skipped, (light) =>
        {
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
        });

        // 环境光
        RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.45f);
        RenderSettings.ambientIntensity = 1f;

        // 雾效
        RenderSettings.fog = true;
        RenderSettings.fogMode = template.fogMode;
        RenderSettings.fogColor = template.fogColor;
        RenderSettings.fogDensity = template.fogDensity;

        // 天空盒
        if (template.skyboxMaterial != null)
        {
            RenderSettings.skybox = template.skyboxMaterial;
        }
    }

    static void CreateGround(SceneTemplate template, ref int created, ref int skipped)
    {
        GameObject ground = CreateOrSkip("Ground", ref created, ref skipped);
        if (ground != null)
        {
            ground.transform.localScale = template.groundScale;
            ground.transform.position = Vector3.zero;
            ground.layer = LayerMask.NameToLayer("Ground");
        }
    }

    static T CreateOrSkipComponent<T>(GameObject obj, string name, ref int created) where T : Component
    {
        T component = obj.GetComponent<T>();
        if (component == null)
        {
            component = obj.AddComponent<T>();
            created++;
            Debug.Log($"[TemplateToScene]   ✓ 添加组件: {typeof(T).Name}");
        }
        return component;
    }

    static GameObject CreateOrSkip(string name, ref int created, ref int skipped, System.Action<GameObject> setup = null)
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null)
        {
            obj = new GameObject(name);
            created++;
            setup?.Invoke(obj);
            Debug.Log($"[TemplateToScene]   ✓ 创建: {name}");
        }
        else
        {
            skipped++;
            Debug.Log($"[TemplateToScene]   ⊘ 已存在: {name}");
        }
        return obj;
    }

    static GameObject CreateOrSkip<T>(string name, ref int created, ref int skipped, System.Action<T> setup = null) where T : Component
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null)
        {
            obj = new GameObject(name);
            T component = obj.AddComponent<T>();
            created++;
            setup?.Invoke(component);
            Debug.Log($"[TemplateToScene]   ✓ 创建: {name} (with {typeof(T).Name})");
        }
        else
        {
            skipped++;
            Debug.Log($"[TemplateToScene]   ⊘ 已存在: {name}");
        }
        return obj;
    }
}

#endif
