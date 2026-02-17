// Assets/Scripts/Portal/AutoTeleportFixer.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// 自动传送修复器
/// 将此组件添加到DontDestroyOnLoad对象上，会自动修复所有传送问题
/// </summary>
public class AutoTeleportFixer : MonoBehaviour
{
    public static AutoTeleportFixer Instance;

    [Header("自动修复设置")]
    [Tooltip("启用自动修复")]
    public bool enableAutoFix = true;

    [Tooltip("调试模式")]
    public bool debugMode = true;

    [Tooltip("默认安全位置")]
    public Vector3 safePosition = new Vector3(0, 1.5f, 0);

    private bool isInitialized = false;
    private Coroutine _checkRoutine;
    private Coroutine _fixRoutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (debugMode)
            Debug.Log("[AutoTeleportFixer] 已初始化");

        // 订阅场景加载事件
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        _fixRoutine = StartCoroutine(FixPlayerRoutine());
    }

    void Update()
    {
        if (!enableAutoFix) return;

        // 持续监控Player状态（防止重复启动协程）
        if (_checkRoutine == null)
        {
            _checkRoutine = StartCoroutine(CheckPlayerRoutine());
        }
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (debugMode)
            Debug.Log($"[AutoTeleportFixer] 场景已加载: {scene.name}");

        // 场景加载后立即开始修复（防止重复启动）
        if (_fixRoutine == null)
        {
            _fixRoutine = StartCoroutine(FixPlayerRoutine());
        }
    }

    /// <summary>
    /// 修复Player的协程
    /// </summary>
    private IEnumerator FixPlayerRoutine()
    {
        if (isInitialized)
        {
            _fixRoutine = null;
            yield break;
        }

        // 等待几帧让所有系统初始化
        for (int i = 0; i < 5; i++)
        {
            yield return null;
        }

        isInitialized = true;

        if (debugMode)
            Debug.Log("[AutoTeleportFixer] 开始自动修复...");

        // 查找Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            if (debugMode)
                Debug.LogWarning("[AutoTeleportFixer] 未找到Player，尝试生成...");

            yield break;
        }

        // 修复Player
        yield return StartCoroutine(FixPlayerObject(player));

        if (debugMode)
            Debug.Log("[AutoTeleportFixer] 自动修复完成");

        // 重置初始化状态，以便下次场景加载时再次修复
        isInitialized = false;
        _fixRoutine = null;
    }

    /// <summary>
    /// 检查Player状态的协程
    /// </summary>
    private IEnumerator CheckPlayerRoutine()
    {
        // 防止频繁检查
        yield return new WaitForSeconds(1f);

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            _checkRoutine = null;
            yield break;
        }

        // 检查Player是否被禁用
        if (!player.activeInHierarchy)
        {
            if (debugMode)
                Debug.LogWarning("[AutoTeleportFixer] 检测到Player被禁用，正在启用...");

            player.SetActive(true);

            if (debugMode)
                Debug.Log("[AutoTeleportFixer] Player已启用");
        }

        // 检查Player位置是否异常
        Vector3 pos = player.transform.position;
        if (pos.y < -10f || pos.y > 100f)
        {
            if (debugMode)
                Debug.LogWarning($"[AutoTeleportFixer] 检测到Player位置异常: {pos}，正在修复...");

            yield return StartCoroutine(FixPlayerPosition(player));
        }

        // 协程执行完毕，允许下次启动
        _checkRoutine = null;
    }

    /// <summary>
    /// 修复Player对象
    /// </summary>
    private IEnumerator FixPlayerObject(GameObject player)
    {
        // 确保Player启用
        if (!player.activeInHierarchy)
        {
            player.SetActive(true);
            if (debugMode) Debug.Log("[AutoTeleportFixer] ✓ 已启用Player");
        }

        // 检查Player位置
        Vector3 pos = player.transform.position;
        if (pos.y < -10f || pos.y > 100f)
        {
            if (debugMode)
                Debug.LogWarning($"[AutoTeleportFixer] Player位置异常: {pos}，正在修复...");

            yield return StartCoroutine(FixPlayerPosition(player));
        }

        // 检查Player组件
        CheckPlayerComponents(player);

        yield return null;
    }

    /// <summary>
    /// 修复Player位置
    /// </summary>
    private IEnumerator FixPlayerPosition(GameObject player)
    {
        // 禁用CharacterController
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            if (debugMode) Debug.Log("[AutoTeleportFixer] 已禁用CharacterController");
        }

        // 设置到安全位置
        player.transform.position = safePosition;
        player.transform.rotation = Quaternion.identity;

        if (debugMode)
            Debug.Log($"[AutoTeleportFixer] Player位置已修复: {safePosition}");

        yield return null;

        // 重新启用CharacterController
        if (cc != null)
        {
            cc.enabled = true;
            if (debugMode) Debug.Log("[AutoTeleportFixer] 已重新启用CharacterController");
        }
    }

    /// <summary>
    /// 检查Player组件
    /// </summary>
    private void CheckPlayerComponents(GameObject player)
    {
        if (!debugMode) return;

        Debug.Log("[AutoTeleportFixer] 检查Player组件...");

        // 检查必要的组件
        string[] requiredComponents = { "CharacterController", "TPSInput", "TPSCharacter" };

        foreach (string componentName in requiredComponents)
        {
            Component comp = player.GetComponent(componentName);
            if (comp == null)
            {
                Debug.LogWarning($"[AutoTeleportFixer] ⚠ Player缺少组件: {componentName}");
            }
            else
            {
                if (debugMode) Debug.Log($"[AutoTeleportFixer] ✓ {componentName}");
            }
        }
    }

    /// <summary>
    /// 手动修复Player
    /// </summary>
    [ContextMenu("手动修复Player")]
    public void ManualFixPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("[AutoTeleportFixer] 无法修复：场景中没有Player");
            return;
        }

        StartCoroutine(PerformManualFix(player));
    }

    /// <summary>
    /// 执行手动修复
    /// </summary>
    private IEnumerator PerformManualFix(GameObject player)
    {
        if (debugMode)
            Debug.Log("[AutoTeleportFixer] 开始手动修复...");

        // 确保Player启用
        if (!player.activeInHierarchy)
        {
            player.SetActive(true);
            if (debugMode) Debug.Log("[AutoTeleportFixer] ✓ 已启用Player");
            yield return null;
        }

        // 修复位置
        yield return StartCoroutine(FixPlayerPosition(player));

        // 检查组件
        CheckPlayerComponents(player);

        if (debugMode)
            Debug.Log("[AutoTeleportFixer] 手动修复完成");
    }

    /// <summary>
    /// 显示当前状态
    /// </summary>
    [ContextMenu("显示状态")]
    public void ShowStatus()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.Log("[AutoTeleportFixer] 当前场景中没有Player");
            return;
        }

        System.Text.StringBuilder status = new System.Text.StringBuilder();
        status.AppendLine($"=== AutoTeleportFixer 状态 ===");
        status.AppendLine($"场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        status.AppendLine($"Player: {player.name}");
        status.AppendLine($"启用: {player.activeInHierarchy}");
        status.AppendLine($"位置: {player.transform.position}");
        status.AppendLine($"组件数量: {player.GetComponents<Component>().Length}");

        Debug.Log(status.ToString());
    }
}
