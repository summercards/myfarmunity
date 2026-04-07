using UnityEngine;
using UnityEngine.SceneManagement;
using FarmGame.Core;

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
        if (!RuntimeService.TryClaimSingleton(this, instance, nameof(CameraModeManager)))
        {
            return;
        }

        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshReferencesFromRuntime();
        SetCameraMode(currentMode);
    }

    void OnEnable()
    {
        RuntimeRefs.TpsOrbitCameraChanged += HandleTpsOrbitCameraChanged;
        RuntimeRefs.FixedCameraSystemChanged += HandleFixedCameraSystemChanged;
        RuntimeRefs.PlayerCharacterChanged += HandlePlayerCharacterChanged;
    }

    void Start()
    {
        RefreshReferencesFromRuntime();
        SetCameraMode(currentMode);
    }

    void OnDisable()
    {
        RuntimeRefs.TpsOrbitCameraChanged -= HandleTpsOrbitCameraChanged;
        RuntimeRefs.FixedCameraSystemChanged -= HandleFixedCameraSystemChanged;
        RuntimeRefs.PlayerCharacterChanged -= HandlePlayerCharacterChanged;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    void RefreshReferencesFromRuntime()
    {
        if (RuntimeRefs.TpsCamera != null)
        {
            tpsCamera = RuntimeRefs.TpsCamera;
        }

        if (RuntimeRefs.FixedCamera != null)
        {
            fixedCamera = RuntimeRefs.FixedCamera;
        }

        if (RuntimeRefs.PlayerCharacter != null)
        {
            playerCharacter = RuntimeRefs.PlayerCharacter;
        }
    }

    void HandleTpsOrbitCameraChanged(TPSOrbitCamera orbitCamera)
    {
        tpsCamera = orbitCamera ? orbitCamera.GetComponent<Camera>() : null;
        if (currentMode == CameraMode.TPS)
        {
            SetCameraMode(currentMode);
        }
    }

    void HandleFixedCameraSystemChanged(FixedCameraSystem cameraSystem)
    {
        fixedCamera = cameraSystem ? cameraSystem.GetComponent<Camera>() : null;
        if (currentMode == CameraMode.Fixed45Degree)
        {
            SetCameraMode(currentMode);
        }
    }

    void HandlePlayerCharacterChanged(TPSCharacter character)
    {
        playerCharacter = character;
        if (playerCharacter != null)
        {
            playerCharacter.RefreshCachedCamera();
        }
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
        activeCamera = null;

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
