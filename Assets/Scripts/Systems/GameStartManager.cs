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
        // 不使用 LookAt，让 TPSOrbitCamera 自己控制旋转
        // cameraObj.transform.position = player.transform.position - Vector3.back * 3.5f + Vector3.up * 1.5f;
        // cameraObj.transform.LookAt(player.transform.position + Vector3.up);
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
}