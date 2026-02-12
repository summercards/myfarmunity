// Assets/Scripts/UI/MainMenuManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 主菜单管理器
/// 处理主菜单 UI 交互和场景加载
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("主游戏场景名")]
    [Tooltip("请填写主游戏场景的名称，不包含 .unity 后缀")]
    public string mainGameSceneName = "game"; // 默认为 game

    [Header("UI 元素")]
    public TMP_Text titleText;
    public Button startGameButton;

    void Start()
    {
        // 自动查找按钮 (如果 Inspector 中未赋值)
        if (startGameButton == null)
        {
            startGameButton = GetComponentInChildren<Button>();
            if (startGameButton != null)
            {
                Debug.Log("[MainMenuManager] 自动找到了 StartButton");
            }
        }

        // 绑定点击事件
        if (startGameButton != null)
        {
            // 先移除所有监听，防止重复绑定
            startGameButton.onClick.RemoveListener(StartGame);
            // 添加监听
            startGameButton.onClick.AddListener(StartGame);
            Debug.Log("[MainMenuManager] 已绑定按钮点击事件");
        }
        else
        {
            Debug.LogError("[MainMenuManager] 严重错误：场景中找不到任何按钮！");
        }

        Debug.Log("[MainMenuManager] 主菜单管理器已启动");
    }

    /// <summary>
    /// 开始游戏按钮点击事件
    /// </summary>
    public void StartGame()
    {
        Debug.Log($"[MainMenuManager] 点击了开始游戏，正在加载场景: {mainGameSceneName}");
        
        if (Application.CanStreamedLevelBeLoaded(mainGameSceneName))
        {
            SceneManager.LoadScene(mainGameSceneName);
        }
        else
        {
            Debug.LogError($"[MainMenuManager] 无法加载场景 '{mainGameSceneName}'！\n原因可能是：\n1. 场景名拼写错误\n2. 该场景未添加到 Build Settings");
        }
    }

    /// <summary>
    /// 退出游戏按钮点击事件
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[MainMenuManager] 退出游戏");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
