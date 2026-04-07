using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FarmGame.Core;

public class PortalManager : MonoBehaviour
{
    public static PortalManager Instance;

    private string pendingSpawnID;
    private bool isTeleporting;
    private Coroutine _loadSceneRoutine;
    private Coroutine _placePlayerRoutine;

    private void Awake()
    {
        if (!RuntimeService.TryClaimSingleton(this, Instance, nameof(PortalManager)))
        {
            return;
        }

        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// 由 Portal 调用
    /// </summary>
    public void Teleport(string sceneName, string spawnID)
    {
        if (isTeleporting) return;
        if (string.IsNullOrWhiteSpace(spawnID))
        {
            Debug.LogWarning("[PortalManager] Teleport aborted: spawnID is empty.");
            return;
        }

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
        if (!isTeleporting || string.IsNullOrWhiteSpace(pendingSpawnID))
        {
            return;
        }

        _placePlayerRoutine = StartCoroutine(PlacePlayer());
    }

    IEnumerator PlacePlayer()
    {
        Transform playerTransform = null;

        // 等待多帧确保玩家和出生点都已注册完成
        for (int i = 0; i < 10; i++)
        {
            playerTransform = RuntimeRefs.PlayerTransform;
            if (playerTransform != null)
            {
                break;
            }
            yield return null;
        }

        if (playerTransform == null)
        {
            Debug.LogError("[PortalManager] 找不到 Player（请确认Tag=Player）");
            Debug.Log($"[PortalManager] 当前场景: {SceneManager.GetActiveScene().name}");
            Debug.Log($"[PortalManager] 目标 SpawnID: {pendingSpawnID}");
            isTeleporting = false;
            pendingSpawnID = null;
            yield break;
        }

        GameObject player = playerTransform.gameObject;

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
            pendingSpawnID = null;
            yield break;
        }

        Debug.Log($"[PortalManager] 找到 Player: {player.name}, 位置: {player.transform.position}");

        SpawnPoint targetSpawn = null;
        for (int i = 0; i < 10 && targetSpawn == null; i++)
        {
            RuntimeRefs.TryGetSpawnPoint(pendingSpawnID, out targetSpawn);
            if (targetSpawn == null)
            {
                yield return null;
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
            pendingSpawnID = null;
            yield break;
        }

        Debug.LogError($"[PortalManager] 场景里找不到 SpawnID = '{pendingSpawnID}'");
        Debug.Log($"[PortalManager] 可用的 SpawnIDs: {string.Join(", ", RuntimeRefs.GetSpawnIds())}");
        isTeleporting = false;
        pendingSpawnID = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }
}
