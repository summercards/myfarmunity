using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalManager : MonoBehaviour
{
    public static PortalManager Instance;

    private string pendingSpawnID;
    private bool isTeleporting;
    private Coroutine _loadSceneRoutine;
    private Coroutine _placePlayerRoutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 由 Portal 调用
    /// </summary>
    public void Teleport(string sceneName, string spawnID)
    {
        if (isTeleporting) return;

        pendingSpawnID = spawnID;
        _loadSceneRoutine = StartCoroutine(LoadSceneRoutine(sceneName));
    }

    IEnumerator LoadSceneRoutine(string scene)
    {
        isTeleporting = true;

        AsyncOperation op = SceneManager.LoadSceneAsync(scene);

        while (!op.isDone)
            yield return null;

        // 等一帧让所有 Start() 执行
        yield return null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _placePlayerRoutine = StartCoroutine(PlacePlayer());
    }

    IEnumerator PlacePlayer()
    {
        // 等待多帧确保玩家生成完成
        for (int i = 0; i < 5; i++)
        {
            yield return null;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("[PortalManager] 找不到 Player（请确认Tag=Player）");
            Debug.Log($"[PortalManager] 当前场景: {SceneManager.GetActiveScene().name}");
            Debug.Log($"[PortalManager] 目标 SpawnID: {pendingSpawnID}");
            isTeleporting = false;
            yield break;
        }

        // 确保Player是启用的
        if (!player.activeInHierarchy)
        {
            Debug.LogWarning("[PortalManager] Player 被禁用，正在启用...");
            player.SetActive(true);
        }

        // 确保Player有Transform
        if (player.transform == null)
        {
            Debug.LogError("[PortalManager] Player 没有 Transform 组件！");
            isTeleporting = false;
            yield break;
        }

        Debug.Log($"[PortalManager] 找到 Player: {player.name}, 位置: {player.transform.position}");

        SpawnPoint[] points = FindObjectsOfType<SpawnPoint>(true);
        Debug.Log($"[PortalManager] 找到 {points.Length} 个 SpawnPoint");

        SpawnPoint targetSpawn = null;
        foreach (var p in points)
        {
            Debug.Log($"[PortalManager] 检查 SpawnPoint: {p.spawnID}");
            if (p.spawnID == pendingSpawnID)
            {
                targetSpawn = p;
                break;
            }
        }

        if (targetSpawn != null)
        {
            Debug.Log($"[PortalManager] 找到目标 SpawnPoint: {targetSpawn.spawnID}, 位置: {targetSpawn.transform.position}");

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                Debug.Log("[PortalManager] 已禁用 CharacterController");
            }

            player.transform.SetPositionAndRotation(
                targetSpawn.transform.position,
                targetSpawn.transform.rotation
            );

            Debug.Log($"[PortalManager] Player 已传送到: {player.transform.position}");

            if (cc != null)
            {
                cc.enabled = true;
                Debug.Log("[PortalManager] 已重新启用 CharacterController");
            }

            isTeleporting = false;
            yield break;
        }

        Debug.LogError($"[PortalManager] 场景里找不到 SpawnID = '{pendingSpawnID}'");
        Debug.Log($"[PortalManager] 可用的 SpawnIDs: {string.Join(", ", System.Array.ConvertAll(points, p => p.spawnID))}");
        isTeleporting = false;
    }
}