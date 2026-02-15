// Assets/Scripts/Systems/GameStartManager.cs
using UnityEngine;

/// <summary>
/// 游戏启动管理器
/// 负责在场景加载时生成角色，并重新绑定背包UI引用
/// </summary>
public class GameStartManager : MonoBehaviour
{
    [Header("角色配置")]
    public GameObject playerPrefab;
    public Transform spawnPoint;

    void Start()
    {
        SpawnPlayer();
        // 场景加载后重新绑定所有背包UI
        RebindAllInventoryUIs();
    }

    private void SpawnPlayer()
    {
        // 先检查场景中是否已有 Player（避免与 PortalManager 冲突）
        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer != null)
        {
            Debug.Log("[GameStart] 场景中已有 Player，跳过创建");
            CreatePlayerCamera(existingPlayer);
            return;
        }

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

        // 自动为 Player 创建摄像机
        CreatePlayerCamera(player);
    }

    private void CreatePlayerCamera(GameObject player)
    {
        // 检查是否已有摄像机
        Camera[] existingCameras = FindObjectsOfType<Camera>();
        foreach (Camera existingCamera in existingCameras)
        {
            if (existingCamera.name == "PlayerCamera")
            {
                // 如果已有摄像机，只是更新它的 target
                TPSOrbitCamera existingOrbitCam = existingCamera.GetComponent<TPSOrbitCamera>();
                if (existingOrbitCam != null && existingOrbitCam.target == null)
                {
                    existingOrbitCam.target = player.transform;
                    Debug.Log("[GameStart] 已有摄像机，更新 target 为 Player");
                }
                return;
            }
        }

        // 没有摄像机，创建新的（独立对象，不作为 Player 的子物体）
        GameObject cameraObj = new GameObject("PlayerCamera");
        cameraObj.transform.position = new Vector3(0, 0, 0); // 初始位置不重要，TPSOrbitCamera 会设置
        cameraObj.transform.rotation = Quaternion.identity; // 初始旋转不重要

        // 添加 Camera 组件
        Camera playerCamera = cameraObj.AddComponent<Camera>();
        playerCamera.nearClipPlane = 0.3f;
        playerCamera.farClipPlane = 1000f;
        playerCamera.fieldOfView = 60f;
        playerCamera.tag = "MainCamera";

        // 添加 TPSOrbitCamera 组件
        TPSOrbitCamera orbitCam = cameraObj.AddComponent<TPSOrbitCamera>();
        orbitCam.target = player.transform;
        orbitCam.distance = 3.5f;
        orbitCam.minDistance = 1f;
        orbitCam.maxDistance = 5.5f;
        orbitCam.yawSpeed = 120f;
        orbitCam.pitchSpeed = 120f;
        orbitCam.pitchLimits = new Vector2(-30f, 70f);

        Debug.Log("[GameStart] 摄像机已创建并绑定到 Player");
    }

    /// <summary>
    /// 重新绑定所有背包UI的PlayerInventoryHolder引用
    /// 场景切换后调用此方法确保UI引用正确的单例实例
    /// </summary>
    private void RebindAllInventoryUIs()
    {
        // 延迟一帧执行，确保所有组件都已初始化
        StartCoroutine(RebindInventoryUIsCoroutine());
    }

    private System.Collections.IEnumerator RebindInventoryUIsCoroutine()
    {
        yield return null; // 等待一帧

        // 确保背包单例已存在
        if (PlayerInventoryHolder.Instance == null)
        {
            Debug.LogWarning("[GameStart] PlayerInventoryHolder 单例不存在，无法重新绑定UI");
            yield break;
        }

        // 查找所有 InventoryUI 并重新绑定
        InventoryUI[] inventoryUIs = FindObjectsOfType<InventoryUI>();
        int reboundCount = 0;

        foreach (var ui in inventoryUIs)
        {
            bool wasBound = ui.playerInv != null;
            bool needsRebind = ui.playerInv == null || ui.playerInv.gameObject == null;

            if (needsRebind)
            {
                // 重新绑定到单例
                ui.playerInv = PlayerInventoryHolder.Instance;

                // 如果 itemDB 为空，从单例获取
                if (ui.itemDB == null)
                {
                    ui.itemDB = PlayerInventoryHolder.Instance.itemDB;
                }

                // 刷新UI
                ui.RefreshAll();
                reboundCount++;

                Debug.Log($"[GameStart] 已重新绑定 InventoryUI (WasBound: {wasBound})");
            }
        }

        if (reboundCount > 0)
        {
            Debug.Log($"[GameStart] 成功重新绑定 {reboundCount} 个 InventoryUI");
        }
        else
        {
            Debug.Log("[GameStart] 所有 InventoryUI 引用都已正确，无需重新绑定");
        }
    }
}
