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

    [Header("提示信息")]
    [Tooltip("显示传送提示文字")]
    public bool showPrompt = true;

    [Tooltip("提示文字")]
    public string promptText = "按 E 传送";

    private bool isPlayerNearby = false;
    private bool isTeleporting = false;
    private float teleportTimer = 0f;

    private void Awake()
    {
        // 自动添加碰撞体（如果还没有）
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<SphereCollider>();
        }
        collider.isTrigger = true;
    }

    private void Reset()
    {
        // 在编辑器中添加组件时自动设置
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<SphereCollider>();
        }
        collider.isTrigger = true;
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

        if (requireKeyPress)
        {
            // 需要按键传送
            if (Input.GetKeyDown(teleportKey))
            {
                Debug.Log($"[Portal] 玩家按下 {teleportKey} 键，开始传送");
                StartTeleport();
            }
        }
        else
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

        if (PortalManager.Instance == null)
        {
            Debug.LogError("场景中没有 PortalManager！");
            return;
        }

        isTeleporting = true;
        ShowPrompt(false);

        Debug.Log($"[Portal] 传送到场景: {targetScene}, SpawnID: {targetSpawnID}");

        PortalManager.Instance.Teleport(targetScene, targetSpawnID);

        // 传送后重置状态（如果场景未加载成功）
        Invoke(nameof(ResetTeleportState), 5f);
    }

    private void ResetTeleportState()
    {
        isTeleporting = false;
    }

    private void ShowPrompt(bool show)
    {
        // TODO: 这里可以接入UI系统显示提示
        // 目前使用Debug.Log作为临时方案
        if (show)
        {
            Debug.Log($"[Portal] {promptText}");
        }
    }

    /// <summary>
    /// 在Scene视图中绘制Gizmos
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Collider collider = GetComponent<Collider>();

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
