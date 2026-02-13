// Assets/Scripts/Portal/Portal.cs
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 传送门系统
/// 用于场景之间的传送
/// </summary>
[RequireComponent(typeof(Collider))]
public class Portal : MonoBehaviour
{
    [Header("传送配置")]
    [Tooltip("目标场景名称（留空则不传送）")]
    public string targetSceneName;

    [Tooltip("传送后的位置（留空则使用默认位置）")]
    public Transform spawnPoint;

    [Tooltip("传送门的显示名称（用于调试或UI）")]
    public string portalName = "传送门";

    [Header("传送触发")]
    [Tooltip("需要按键才能传送（留空则自动传送）")]
    public KeyCode interactKey = KeyCode.None;

    [Tooltip("自动传送的延迟时间（秒）")]
    public float autoTeleportDelay = 0.5f;

    [Header("视觉效果")]
    [Tooltip("传送时的淡入淡出时间")]
    public float fadeDuration = 0.5f;

    [Tooltip("传送门颜色")]
    public Color portalColor = new Color(0f, 0.8f, 1f, 0.5f);

    [Header("调试选项")]
    [Tooltip("显示调试信息")]
    public bool showDebugInfo = true;

    private Collider portalCollider;
    private bool isPlayerNearby = false;
    private bool isTeleporting = false;
    private float teleportTimer = 0f;

    private void Awake()
    {
        portalCollider = GetComponent<Collider>();
        if (portalCollider != null)
        {
            portalCollider.isTrigger = true;
        }

        if (showDebugInfo)
        {
            Debug.Log($"[Portal] 传送门 '{portalName}' 已初始化 -> 目标场景: {targetSceneName}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTeleporting) return;

        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;

            if (showDebugInfo)
            {
                Debug.Log($"[Portal] 玩家接近传送门 '{portalName}'");
            }

            // 如果设置了按键，显示提示
            if (interactKey != KeyCode.None)
            {
                Debug.Log($"[Portal] 按 {interactKey} 键传送");
            }
            else
            {
                // 自动传送
                teleportTimer = 0f;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isTeleporting || !isPlayerNearby) return;

        if (other.CompareTag("Player"))
        {
            if (interactKey != KeyCode.None)
            {
                // 需要按键传送
                if (Input.GetKeyDown(interactKey))
                {
                    StartTeleport();
                }
            }
            else
            {
                // 自动传送，等待延迟时间
                teleportTimer += Time.deltaTime;
                if (teleportTimer >= autoTeleportDelay)
                {
                    StartTeleport();
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            teleportTimer = 0f;

            if (showDebugInfo)
            {
                Debug.Log($"[Portal] 玩家离开传送门 '{portalName}'");
            }
        }
    }

    /// <summary>
    /// 开始传送
    /// </summary>
    public void StartTeleport()
    {
        if (isTeleporting) return;
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning($"[Portal] 传送门 '{portalName}' 没有设置目标场景！");
            return;
        }

        isTeleporting = true;

        if (showDebugInfo)
        {
            Debug.Log($"[Portal] 开始传送 '{portalName}' -> '{targetSceneName}'");
        }

        // 使用协程处理传送
        StartCoroutine(TeleportCoroutine());
    }

    /// <summary>
    /// 传送协程
    /// </summary>
    private System.Collections.IEnumerator TeleportCoroutine()
    {
        // 1. 禁用玩家控制
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 禁用玩家输入
            var input = player.GetComponentInChildren<TPSInput>();
            if (input != null)
            {
                input.enabled = false;
            }
        }

        // 2. 淡出效果（可选，可以接入UI系统）
        // 这里只是预留接口，实际淡入淡出需要配合UI系统
        yield return new WaitForSeconds(fadeDuration * 0.5f);

        // 3. 保存位置信息（用于传送回来）
        if (spawnPoint != null)
        {
            PlayerPrefs.SetString($"LastSpawnPoint_{targetSceneName}", spawnPoint.name);
            PlayerPrefs.SetFloat($"LastSpawnX_{targetSceneName}", spawnPoint.position.x);
            PlayerPrefs.SetFloat($"LastSpawnY_{targetSceneName}", spawnPoint.position.y);
            PlayerPrefs.SetFloat($"LastSpawnZ_{targetSceneName}", spawnPoint.position.z);
            PlayerPrefs.Save();
        }

        // 4. 加载新场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);

        // 等待场景加载完成
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 5. 等待新场景初始化
        yield return new WaitForSeconds(0.1f);

        // 6. 查找新场景中的玩家并设置位置
        GameObject newPlayer = GameObject.FindGameObjectWithTag("Player");
        if (newPlayer != null)
        {
            // 设置玩家位置到目标传送门的生成点
            if (spawnPoint != null)
            {
                newPlayer.transform.position = spawnPoint.position;
                newPlayer.transform.rotation = spawnPoint.rotation;

                if (showDebugInfo)
                {
                    Debug.Log($"[Portal] 玩家传送到位置: {spawnPoint.position}");
                }
            }
            else if (portalCollider != null)
            {
                // 如果没有指定生成点，使用传送门的位置
                newPlayer.transform.position = portalCollider.bounds.center;
            }

            // 7. 启用玩家控制
            var input = newPlayer.GetComponentInChildren<TPSInput>();
            if (input != null)
            {
                input.enabled = true;
            }
        }

        isTeleporting = false;

        if (showDebugInfo)
        {
            Debug.Log($"[Portal] 传送完成！");
        }
    }

    /// <summary>
    /// 手动设置目标场景
    /// </summary>
    public void SetTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
        Debug.Log($"[Portal] 传送门 '{portalName}' 目标场景已设置为: {sceneName}");
    }

    /// <summary>
    /// 手动设置生成点
    /// </summary>
    public void SetSpawnPoint(Transform spawn)
    {
        spawnPoint = spawn;
        Debug.Log($"[Portal] 传送门 '{portalName}' 生成点已设置");
    }

    /// <summary>
    /// 绘制调试信息
    /// </summary>
    private void OnDrawGizmos()
    {
        if (portalCollider == null)
        {
            portalCollider = GetComponent<Collider>();
        }

        // 绘制传送门边界
        Gizmos.color = portalColor;
        if (portalCollider != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            if (portalCollider is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (portalCollider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (portalCollider is CapsuleCollider capsule)
            {
                Gizmos.DrawWireSphere(capsule.center, capsule.radius);
            }
        }

        // 绘制生成点连接线
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, spawnPoint.position);
            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
        }

        // 显示目标场景名称
        if (!string.IsNullOrEmpty(targetSceneName))
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"传送门: {portalName}\n目标: {targetSceneName}");
#endif
        }
    }
}
