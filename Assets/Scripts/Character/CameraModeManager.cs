using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 相机模式管理器 - 控制第三人称和固定视角之间的切换
/// </summary>
public class CameraModeManager : MonoBehaviour
{
    public enum CameraMode
    {
        TPS,           // 第三人称旋转视角
        Fixed45Degree  // 45度固定顶视角
    }

    [Header("相机引用")]
    public Camera tpsCamera;
    public Camera fixedCamera;

    [Header("角色引用")]
    public TPSCharacter playerCharacter;  // 用于切换移动模式

    [Header("当前模式")]
    public CameraMode currentMode = CameraMode.Fixed45Degree;

    [Header("视角切换键")]
    public KeyCode switchKey = KeyCode.V;

    public static CameraModeManager instance { get; private set; }

    // 当前活动的相机引用（供 TPSCharacter 使用，避免依赖 Camera.main）
    public Camera activeCamera { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);  // 跨场景保持
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            // 场景切换后，新的CameraManager对象如果存在就销毁
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 场景加载后，重新绑定相机引用
        StartCoroutine(ReBindReferences());
    }

    System.Collections.IEnumerator ReBindReferences()
    {
        yield return null;  // 等待一帧

        // 尝试找到TPS相机（在新场景中）
        if (tpsCamera == null)
        {
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (var cam in cameras)
            {
                if (cam.gameObject.GetComponent<TPSOrbitCamera>() != null)
                {
                    tpsCamera = cam;
                    Debug.Log("[CameraMode] 在新场景中找到 TPS相机");
                    break;
                }
            }
        }

        // 尝试找到Fixed相机
        if (fixedCamera == null)
        {
            FixedCameraSystem fcs = FindObjectOfType<FixedCameraSystem>();
            if (fcs != null)
            {
                fixedCamera = fcs.GetComponent<Camera>();
                Debug.Log("[CameraMode] 在新场景中找到 Fixed相机");
            }
        }

        // 尝试找到Player
        if (playerCharacter == null)
        {
            TPSCharacter[] tpsChars = FindObjectsOfType<TPSCharacter>();
            if (tpsChars.Length > 0)
            {
                playerCharacter = tpsChars[0];
                Debug.Log("[CameraMode] 在新场景中找到 Player");
            }
        }

        // 查找完成后应用当前模式
        SetCameraMode(currentMode);
    }

    private void Start()
    {
        // 启动时自动查找引用
        StartCoroutine(ReBindReferences());
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            ToggleCameraMode();
        }
    }

    public void SetCameraMode(CameraMode mode)
    {
        currentMode = mode;

        // 关闭所有相机
        if (tpsCamera != null) tpsCamera.enabled = false;
        if (fixedCamera != null) fixedCamera.enabled = false;

        // 根据模式激活对应相机
        switch (mode)
        {
            case CameraMode.TPS:
                if (tpsCamera != null)
                {
                    tpsCamera.enabled = true;
                    activeCamera = tpsCamera;
                    Debug.Log("[CameraMode] 切换到 TPS 第三人称视角");
                }
                // 切换到相机相对移动
                if (playerCharacter != null)
                {
                    playerCharacter.movementMode = TPSCharacter.MovementMode.CameraRelative;
                    // 强制刷新相机的缓存引用
                    playerCharacter.RefreshCachedCamera();
                }
                break;

            case CameraMode.Fixed45Degree:
                if (fixedCamera != null)
                {
                    fixedCamera.enabled = true;
                    activeCamera = fixedCamera;
                    Debug.Log("[CameraMode] 切换到 固定45度视角");
                }
                // 切换到世界相对移动
                if (playerCharacter != null)
                {
                    playerCharacter.movementMode = TPSCharacter.MovementMode.WorldRelative;
                    // 强制刷新相机的缓存引用
                    playerCharacter.RefreshCachedCamera();
                }
                break;
        }
    }

    public void ToggleCameraMode()
    {
        CameraMode newMode = currentMode == CameraMode.TPS
            ? CameraMode.Fixed45Degree
            : CameraMode.TPS;

        SetCameraMode(newMode);
    }

    // 切换到TPS
    public void SwitchToTPS()
    {
        SetCameraMode(CameraMode.TPS);
    }

    // 切换到固定视角
    public void SwitchToFixed()
    {
        SetCameraMode(CameraMode.Fixed45Degree);
    }
}
