// Assets/Scripts/Editor/DebugMainMenu.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class DebugMainMenu : EditorWindow
{
    [MenuItem("Tools/调试/诊断主菜单问题")]
    public static void DiagnoseMainMenu()
    {
        Debug.Log("=== 开始诊断主菜单问题 ===");

        // 1. 检查当前场景
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name != "main")
        {
            Debug.LogError($"[错误] 当前打开的场景是 '{currentScene.name}'，请先打开 'main' 场景！");
            return;
        }
        Debug.Log($"[检查] 当前场景: {currentScene.name} (正确)");

        // 2. 检查 EventSystem
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("[严重错误] 场景中缺少 EventSystem！按钮无法响应点击。");
            if (EditorUtility.DisplayDialog("修复问题", "场景中缺少 EventSystem，是否自动创建？", "创建", "取消"))
            {
                new GameObject("EventSystem").AddComponent<EventSystem>().gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("[修复] 已创建 EventSystem。");
            }
        }
        else
        {
            if (!eventSystem.gameObject.activeInHierarchy)
            {
                Debug.LogError("[严重错误] EventSystem 组件存在但被禁用！");
                eventSystem.gameObject.SetActive(true);
                Debug.Log("[修复] 已激活 EventSystem。");
            }
            else
            {
                Debug.Log("[检查] EventSystem 存在且已激活 (正确)");
            }
        }

        // 3. 检查 Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[严重错误] 场景中找不到 Canvas！");
            return;
        }
        Debug.Log("[检查] Canvas 存在 (正确)");

        // 4. 检查 GraphicRaycaster
        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            Debug.LogError("[严重错误] Canvas 上缺少 GraphicRaycaster 组件！UI 无法接收点击。");
            if (EditorUtility.DisplayDialog("修复问题", "Canvas 缺少 GraphicRaycaster，是否添加？", "添加", "取消"))
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("[修复] 已添加 GraphicRaycaster。");
            }
        }
        else
        {
            if (!raycaster.enabled)
            {
                Debug.LogError("[严重错误] GraphicRaycaster 组件存在但被禁用！");
                raycaster.enabled = true;
                Debug.Log("[修复] 已启用 GraphicRaycaster。");
            }
            else
            {
                Debug.Log("[检查] GraphicRaycaster 存在且已启用 (正确)");
            }
        }

        // 5. 检查 MainMenuManager
        MainMenuManager manager = FindObjectOfType<MainMenuManager>();
        if (manager == null)
        {
            Debug.LogError("[严重错误] 场景中找不到 MainMenuManager 脚本！");
            return;
        }
        Debug.Log($"[检查] MainMenuManager 存在，目标场景名: '{manager.mainGameSceneName}'");

        // 6. 检查目标场景是否在 Build Settings 中
        bool sceneInBuild = false;
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            // 简单的名称匹配检查
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
            if (sceneName == manager.mainGameSceneName)
            {
                sceneInBuild = true;
                if (!scene.enabled)
                {
                    Debug.LogError($"[错误] 目标场景 '{manager.mainGameSceneName}' 在 Build Settings 中但未启用 (Disabled)！");
                }
                break;
            }
        }

        if (!sceneInBuild)
        {
            Debug.LogError($"[严重错误] 目标场景 '{manager.mainGameSceneName}' 未添加到 Build Settings！");
            Debug.LogWarning("请去 File -> Build Settings 添加该场景。");
        }
        else
        {
            Debug.Log($"[检查] 目标场景 '{manager.mainGameSceneName}' 在 Build Settings 中 (正确)");
        }

        // 7. 检查按钮及其事件绑定
        Button startButton = null;
        // 尝试通过 MainMenuManager 引用查找
        if (manager.startGameButton != null)
        {
            startButton = manager.startGameButton;
            Debug.Log("[检查] 通过 MainMenuManager 引用找到了按钮");
        }
        else
        {
            // 尝试在 Canvas 子物体中查找
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                // 简单的名称猜测
                if (btn.name.Contains("Start") || btn.name.Contains("Play") || btn.name.Contains("Game"))
                {
                    startButton = btn;
                    Debug.Log($"[推测] 找到了可能的开始按钮: '{btn.name}'");
                    
                    // 自动修复引用
                    if (EditorUtility.DisplayDialog("修复引用", $"MainMenuManager 的 Button 引用为空，是否绑定 '{btn.name}'？", "绑定", "跳过"))
                    {
                        manager.startGameButton = startButton;
                        EditorUtility.SetDirty(manager);
                        Debug.Log("[修复] 已更新 MainMenuManager 的按钮引用");
                    }
                    break;
                }
            }
        }

        if (startButton == null)
        {
            Debug.LogError("[严重错误] 无法找到开始游戏按钮！");
            return;
        }

        // 检查按钮的 OnClick 事件
        int persistentEventCount = startButton.onClick.GetPersistentEventCount();
        bool hasRuntimeBinding = false;
        
        // 检查是否有持久化事件 (Inspector 中拖拽的)
        if (persistentEventCount > 0)
        {
            for (int i = 0; i < persistentEventCount; i++)
            {
                string methodName = startButton.onClick.GetPersistentMethodName(i);
                Object target = startButton.onClick.GetPersistentTarget(i);
                Debug.Log($"[检查] 按钮持久化事件 {i}: 目标={target?.name}, 方法={methodName}");
                
                if (target == manager && methodName == "StartGame")
                {
                    hasRuntimeBinding = true;
                }
            }
        }
        else
        {
            Debug.Log("[提示] 按钮没有 Inspector 中的事件绑定。这不一定是错误，如果脚本在 Start() 中用代码绑定了事件也是可以的。");
            Debug.Log("请检查 MainMenuManager.cs 的 Start() 方法是否包含: startGameButton.onClick.AddListener(StartGame);");
        }

        // 如果没有绑定，提供自动修复
        if (!hasRuntimeBinding && persistentEventCount == 0)
        {
             // 实际上我们无法轻易检测代码添加的 Listener，但我们可以尝试帮用户在 Inspector 添加一个保险
             if (EditorUtility.DisplayDialog("建议", "按钮似乎没有在 Inspector 中绑定事件。虽然代码可能绑定了，但为了保险，是否要在 Inspector 中显式添加事件？", "添加", "取消"))
             {
                 UnityEditor.Events.UnityEventTools.AddPersistentListener(startButton.onClick, manager.StartGame);
                 Debug.Log("[修复] 已在 Inspector 中为按钮添加了 StartGame 事件。");
             }
        }

        Debug.Log("=== 诊断结束，请根据红色错误信息进行修复 ===");
    }
}
#endif
