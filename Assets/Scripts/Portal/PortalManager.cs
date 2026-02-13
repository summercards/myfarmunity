using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalManager : MonoBehaviour
{
    public static PortalManager Instance;

    private string pendingSpawnID;
    private bool isTeleporting;

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
        StartCoroutine(LoadSceneRoutine(sceneName));
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
        StartCoroutine(PlacePlayer());
    }

    IEnumerator PlacePlayer()
    {
        // 等玩家生成
        yield return null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("PortalManager: 找不到 Player（请确认Tag=Player）");
            isTeleporting = false;
            yield break;
        }

        SpawnPoint[] points = FindObjectsOfType<SpawnPoint>(true);

        foreach (var p in points)
        {
            if (p.spawnID == pendingSpawnID)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc) cc.enabled = false;

                player.transform.SetPositionAndRotation(
                    p.transform.position,
                    p.transform.rotation
                );

                if (cc) cc.enabled = true;

                isTeleporting = false;
                yield break;
            }
        }

        Debug.LogError("PortalManager: 场景里找不到 SpawnID = " + pendingSpawnID);
        isTeleporting = false;
    }
}