// Assets/Scripts/Portal/SceneInitializer.cs
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景初始化器
/// 确保场景启动时Player正确生成
/// </summary>
public class SceneInitializer : MonoBehaviour
{
    [Header("Player配置")]
    [Tooltip("Player预制体")]
    public GameObject playerPrefab;

    [Tooltip("默认出生位置")]
    public Transform defaultSpawnPoint;

    [Tooltip("调试模式")]
    public bool debugMode = true;

    [Header("自动修复")]
    [Tooltip("启用自动修复Player问题")]
    public bool enableAutoFix = true;

    private void Awake()
    {
        if (debugMode)
            Debug.Log($"[SceneInitializer] 场景 '{SceneManager.GetActiveScene().name}' 开始初始化");

        // 检查是否已有Player
        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");

        if (existingPlayer != null)
        {
            if (debugMode)
                Debug.Log($"[SceneInitializer] 找到现有Player: {existingPlayer.name}");

            // 确保Player是启用的
            if (!existingPlayer.activeInHierarchy)
            {
                Debug.LogWarning("[SceneInitializer] Player被禁用，正在启用...");
                existingPlayer.SetActive(true);
            }

            // 检查Player位置是否合理
            CheckPlayerPosition(existingPlayer);
            return;
        }

        if (debugMode)
            Debug.Log("[SceneInitializer] 没有找到Player，准备生成...");

        // 生成Player
        StartCoroutine(SpawnPlayerDelayed());
    }

    /// <summary>
    /// 延迟生成Player（等待其他系统初始化）
    /// </summary>
    private System.Collections.IEnumerator SpawnPlayerDelayed()
    {
        // 等待3帧
        for (int i = 0; i < 3; i++)
        {
            yield return null;
        }

        // 再次检查是否已有Player（可能在延迟期间被生成）
        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer != null)
        {
            if (debugMode)
                Debug.Log($"[SceneInitializer] 在延迟期间找到了Player: {existingPlayer.name}");
            CheckPlayerPosition(existingPlayer);
            yield break;
        }

        // 如果没有指定PlayerPrefab，尝试在Resources中加载
        if (playerPrefab == null)
        {
            playerPrefab = Resources.Load<GameObject>("Prefabs/Player");
            if (debugMode && playerPrefab != null)
                Debug.Log("[SceneInitializer] 从Resources加载了PlayerPrefab");
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[SceneInitializer] 无法生成Player：未指定PlayerPrefab且Resources中也没有找到");
            yield break;
        }

        // 确定生成位置
        Vector3 spawnPos = defaultSpawnPoint != null ? defaultSpawnPoint.position : new Vector3(0, 1, 0);
        Quaternion spawnRot = defaultSpawnPoint != null ? defaultSpawnPoint.rotation : Quaternion.identity;

        // 生成Player
        GameObject player = Instantiate(playerPrefab, spawnPos, spawnRot);
        player.name = "Player";
        player.tag = "Player";

        if (debugMode)
        {
            Debug.Log($"[SceneInitializer] Player已生成，位置: {player.transform.position}");
            Debug.Log($"[SceneInitializer] Player实例ID: {player.GetInstanceID()}");
        }

        // 5秒后销毁自己
        Destroy(gameObject, 5f);
    }

    /// <summary>
    /// 检查Player位置
    /// </summary>
    private void CheckPlayerPosition(GameObject player)
    {
        if (!enableAutoFix) return;

        Vector3 pos = player.transform.position;

        // 检查位置是否异常
        if (pos.y < -10f || pos.y > 100f)
        {
            Debug.LogWarning($"[SceneInitializer] Player位置异常: {pos}，正在修复...");

            Vector3 safePos = defaultSpawnPoint != null ? defaultSpawnPoint.position : new Vector3(0, 1, 0);

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = safePos;

            if (cc != null) cc.enabled = true;

            Debug.Log($"[SceneInitializer] Player位置已修复: {safePos}");
        }
        else
        {
            if (debugMode)
                Debug.Log($"[SceneInitializer] Player位置正常: {pos}");
        }
    }

    /// <summary>
    /// 手动生成Player
    /// </summary>
    [ContextMenu("手动生成Player")]
    public void ManualSpawnPlayer()
    {
        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer != null)
        {
            Debug.LogWarning("[SceneInitializer] 场景中已有Player，无法生成");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[SceneInitializer] 未设置PlayerPrefab");
            return;
        }

        Vector3 spawnPos = defaultSpawnPoint != null ? defaultSpawnPoint.position : new Vector3(0, 1, 0);
        Quaternion spawnRot = defaultSpawnPoint != null ? defaultSpawnPoint.rotation : Quaternion.identity;

        GameObject player = Instantiate(playerPrefab, spawnPos, spawnRot);
        player.name = "Player";
        player.tag = "Player";

        Debug.Log($"[SceneInitializer] 手动生成Player成功，位置: {player.transform.position}");
    }
}
