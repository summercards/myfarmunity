// Assets/Scripts/Systems/GameStartManager.cs
using UnityEngine;

/// <summary>
/// 游戏启动管理器
/// 负责在场景加载时生成角色
/// </summary>
public class GameStartManager : MonoBehaviour
{
    [Header("角色配置")]
    public GameObject playerPrefab;
    public Transform spawnPoint;

    void Start()
    {
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[GameStart] 未设置 Player Prefab！请在 Inspector 中赋值。");
            return;
        }

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : new Vector3(0, 1, 0);
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject player = Instantiate(playerPrefab, spawnPos, spawnRot);
        player.name = "Player";
        Debug.Log("[GameStart] 角色已生成");
    }
}
