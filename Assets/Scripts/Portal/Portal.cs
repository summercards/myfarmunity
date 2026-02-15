using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("传送配置")]
    [Tooltip("要加载的场景名（必须在Build Settings里）")]
    public string targetScene;

    [Tooltip("目标门的 SpawnID")]
    public string targetSpawnID;

    [Header("触发方式")]
    [Tooltip("是否需要按键才能传送（勾选则自动传送）")]
    public bool requireKeyPress = true;

    [Tooltip("传送快捷键")]
    public KeyCode teleportKey = KeyCode.E;

    [Tooltip("自动传送的延迟时间（秒）")]
    public float autoTeleportDelay = 0.5f;

    [Header("传送后配置")]
    [Tooltip("传送后重置状态的延迟时间（秒）")]
    public float resetStateDelay = 5f;

    [Header("提示信息")]
    [Tooltip("显示传送提示文字")]
    public bool showPrompt = true;

    [Tooltip("提示文字")]
    public string promptText = "按 E 传送";

    [Tooltip("提示UI对象（可选，可以是3D Text或UI元素）")]
    public GameObject promptUIObject;

    private bool isPlayerNearby = false;
    private bool isTeleporting = false;
    private float teleportTimer = 0f;

    private void Awake()
    {
        // 自动添加碰撞体（如果还没有）
        var collider = GetComponent<Collider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<SphereCollider>();
        }
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        // 初始化时隐藏提示UI
        if (promptUIObject != null)
        {
            promptUIObject.SetActive(false);
        }
    }

    private void Reset()
    {
        // 在编辑器中添加组件时自动设置
        var collider = GetComponent<Collider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<SphereCollider>();
        }
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        // 尝试查找子对象中的提示UI
        if (promptUIObject == null)
        {
            // 查找名为 "PromptUI" 或 "Prompt" 的子对象
            var prompt = transform.Find("PromptUI") ?? transform.Find("Prompt");
            if (prompt != null)
            {
                promptUIObject = prompt.gameObject;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTeleporting) return;
        if (!other.CompareTag("Player")) return;

        isPlayerNearby = true;
        teleportTimer = 0f;

        if (showPrompt)
        {
            ShowPrompt(true);
        }

        if (!requireKeyPress)
        {
            // 自动传送模式，等待延迟时间
            Debug.Log($"[Portal] 靠近传送门，{autoTeleportDelay}秒后自动传送...");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isTeleporting || !isPlayerNearby) return;
        if (!other.CompareTag("Player")) return;

        if (!requireKeyPress)
        {
            // 自动传送，等待延迟时间
            teleportTimer += Time.deltaTime;
            if (teleportTimer >= autoTeleportDelay)
            {
                Debug.Log($"[Portal] 自动传送（延迟{autoTeleportDelay}秒）");
                StartTeleport();
            }
        }
    }

    private void Update()
    {
        // 在 Update 中处理按键输入，确保每次按键都能被捕获
        if (isTeleporting || !isPlayerNearby) return;
        if (!requireKeyPress) return;

        if (Input.GetKeyDown(teleportKey))
        {
            Debug.Log($"[Portal] 玩家按下 {teleportKey} 键，开始传送");
            StartTeleport();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerNearby = false;
        teleportTimer = 0f;

        if (showPrompt)
        {
            ShowPrompt(false);
        }
    }

    private void StartTeleport()
    {
        if (isTeleporting) return;

        // 参数验证
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("[Portal] 传送失败：目标场景未设置！请在Inspector中设置 targetScene");
            return;
        }

        if (string.IsNullOrEmpty(targetSpawnID))
        {
            Debug.LogWarning("[Portal] 目标 SpawnID 为空，将使用默认出生点");
        }

        if (PortalManager.Instance == null)
        {
            Debug.LogError("[Portal] 传送失败：场景中没有 PortalManager！");
            return;
        }

        isTeleporting = true;
        ShowPrompt(false);

        Debug.Log($"[Portal] 传送到场景: {targetScene}, SpawnID: {targetSpawnID}");

        PortalManager.Instance.Teleport(targetScene, targetSpawnID);

        // 传送后重置状态（如果场景未加载成功）
        Invoke(nameof(ResetTeleportState), resetStateDelay);
    }

    private void ResetTeleportState()
    {
        isTeleporting = false;
    }

    private void ShowPrompt(bool show)
    {
        // 如果配置了UI对象，显示/隐藏它
        if (promptUIObject != null)
        {
            promptUIObject.SetActive(show && isPlayerNearby);
        }

        // 作为备选方案，仍然保留日志输出（可选）
        if (show && promptUIObject == null)
        {
            Debug.Log($"[Portal] {promptText} (提示：请配置 promptUIObject 以显示UI)");
        }
    }

    /// <summary>
    /// 在Scene视图中绘制Gizmos
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        var collider = GetComponent<Collider>();

        if (collider is SphereCollider sphere)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, sphere.radius);
        }
        else if (collider is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }

        // 显示传送门信息
        Gizmos.color = Color.white;
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2,
            $"传送门\n目标: {targetScene}\nSpawnID: {targetSpawnID}\n按键: {(requireKeyPress ? teleportKey.ToString() : "自动")}"
        );
#endif
    }
}
