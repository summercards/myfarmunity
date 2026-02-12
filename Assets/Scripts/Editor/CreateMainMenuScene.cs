// Assets/Scripts/Editor/CreateMainMenuScene.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 编辑器工具：创建主菜单场景
/// </summary>
public class CreateMainMenuScene : EditorWindow
{
    private string mainGameSceneName = "TestScene"; // 假设主游戏场景名

    [MenuItem("Tools/场景/创建主菜单场景")]
    public static void ShowWindow()
    {
        GetWindow<CreateMainMenuScene>("创建主菜单场景");
    }

    void OnGUI()
    {
        GUILayout.Label("主菜单场景配置", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        mainGameSceneName = EditorGUILayout.TextField("主游戏场景名", mainGameSceneName);
        EditorGUILayout.HelpBox("请确保主游戏场景已存在，并在此处填写正确的场景名称（不含 .unity 后缀）。", MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("创建并配置主菜单场景"))
        {
            CreateAndSetupMainMenuScene(mainGameSceneName);
        }
    }

    private static void CreateAndSetupMainMenuScene(string gameSceneName)
    {
        Debug.Log("=== 开始创建主菜单场景 ===");

        // 创建新场景
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        newScene.name = "main";

        // 设置主摄像机
        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.transform.position = new Vector3(0, 1, -10);
        cameraObj.transform.rotation = Quaternion.Euler(0, 0, 0);
        Camera camera = cameraObj.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black; // 主菜单背景为黑色
        cameraObj.AddComponent<AudioListener>();

        // 创建 Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        // 创建 EventSystem
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 在 Canvas 上添加主菜单管理器
        MainMenuManager mainMenuManager = canvasObj.AddComponent<MainMenuManager>();
        mainMenuManager.mainGameSceneName = gameSceneName;

        // 创建标题文本
        GameObject titleTextObj = new GameObject("TitleText");
        titleTextObj.transform.SetParent(canvasObj.transform, false);
        TMP_Text titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "我的农场游戏";
        titleText.fontSize = 72;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.8f);
        titleRect.anchorMax = new Vector2(0.5f, 0.9f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(800, 100);
        titleRect.anchoredPosition = new Vector2(0, 0);

        // 创建开始游戏按钮
        GameObject startButtonObj = new GameObject("StartButton");
        startButtonObj.transform.SetParent(canvasObj.transform, false);
        Button startButton = startButtonObj.AddComponent<Button>();
        Image buttonImage = startButtonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.2f, 1f); // 绿色按钮
        RectTransform buttonRect = startButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.4f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(300, 80);
        buttonRect.anchoredPosition = new Vector2(0, 0);

        // 按钮文本
        GameObject buttonTextObj = new GameObject("ButtonText");
        buttonTextObj.transform.SetParent(startButtonObj.transform, false);
        TMP_Text buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "开始游戏";
        buttonText.fontSize = 48;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;
        RectTransform buttonTextRect = buttonText.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = new Vector2(0, 0);
        buttonTextRect.anchorMax = new Vector2(1, 1);
        buttonTextRect.pivot = new Vector2(0.5f, 0.5f);
        buttonTextRect.anchoredPosition = new Vector2(0, 0);

        // 关键修复：正确赋值引用
        mainMenuManager.startGameButton = startButton;
        mainMenuManager.titleText = titleText;

        // 关键修复：添加持久化点击事件 (这样会显示在 Inspector 中)
        UnityEditor.Events.UnityEventTools.AddPersistentListener(startButton.onClick, mainMenuManager.StartGame);

        // 保存场景
        string scenePath = "Assets/Scenes/main.unity";
        string folder = "Assets/Scenes";

        if (!System.IO.Directory.Exists(folder))
        {
            System.IO.Directory.CreateDirectory(folder);
        }

        EditorSceneManager.SaveScene(newScene, scenePath);
        AssetDatabase.Refresh();

        Debug.Log($"主菜单场景已创建: {scenePath}");

        // 添加到 Build Settings
        AddSceneToBuildSettings(scenePath, gameSceneName);

        Debug.Log("=== 主菜单场景配置完成！ ===");
        EditorUtility.DisplayDialog("主菜单配置完成", "main.unity 场景已创建并添加到 Build Settings。请确保你的主游戏场景也已添加到 Build Settings！", "确定");
    }

    private static void AddSceneToBuildSettings(string mainMenuScenePath, string gameSceneName)
    {
        EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
        bool mainMenuFound = false;
        bool gameSceneFound = false;

        foreach (EditorBuildSettingsScene scene in currentScenes)
        {
            if (scene.path == mainMenuScenePath)
            {
                mainMenuFound = true;
            }
            if (scene.path.EndsWith($"/{gameSceneName}.unity"))
            {
                gameSceneFound = true;
            }
        }

        if (!mainMenuFound || !gameSceneFound)
        {
            EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[currentScenes.Length + (mainMenuFound ? 0 : 1) + (gameSceneFound ? 0 : 1)];
            currentScenes.CopyTo(newScenes, 0);

            int index = currentScenes.Length;
            if (!mainMenuFound)
            {
                newScenes[index++] = new EditorBuildSettingsScene(mainMenuScenePath, true);
            }
            if (!gameSceneFound)
            {
                // 查找主游戏场景的实际路径
                string[] guids = AssetDatabase.FindAssets($"{gameSceneName} t:Scene");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    newScenes[index++] = new EditorBuildSettingsScene(path, true);
                    Debug.Log($"已找到并添加主游戏场景到 Build Settings: {path}");
                }
                else
                {
                    Debug.LogError($"未找到主游戏场景 '{gameSceneName}.unity'！请手动添加到 Build Settings。");
                }
            }

            EditorBuildSettings.scenes = newScenes;
            Debug.Log("Build Settings 已更新。");
        }
        else
        {
            Debug.Log("主菜单场景和主游戏场景已存在于 Build Settings。");
        }

        // 确保主菜单场景是第一个
        EditorBuildSettingsScene[] finalScenes = EditorBuildSettings.scenes;
        int mainMenuIndex = -1;
        for (int i = 0; i < finalScenes.Length; i++)
        {
            if (finalScenes[i].path == mainMenuScenePath)
            {
                mainMenuIndex = i;
                break;
            }
        }

        if (mainMenuIndex > 0)
        {
            // 将主菜单场景移动到第一个位置
            EditorBuildSettingsScene temp = finalScenes[mainMenuIndex];
            for (int i = mainMenuIndex; i > 0; i--)
            {
                finalScenes[i] = finalScenes[i - 1];
            }
            finalScenes[0] = temp;
            EditorBuildSettings.scenes = finalScenes;
            Debug.Log("主菜单场景已设置为 Build Settings 的第一个场景。");
        }
    }
}
#endif
