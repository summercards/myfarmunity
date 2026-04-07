using UnityEngine;

#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 固定视角相机系统 - 45度顶视角，跟随Player移动
/// 不做单例管理，由CameraModeManager统一控制
/// </summary>
public class FixedCameraSystem : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;

    [Header("相机偏移（相对于Player）")]
    public Vector3 cameraOffset = new Vector3(0, 10f, -10f);  // Y=高度，Z=后方距离

    [Header("旋转角度")]
    public float pitchAngle = 45f;   // 俯视角度
    public float yawAngle = 0f;       // 偏航角度

    [Header("跟随设置")]
    public float followSpeed = 8f;    // 跟随平滑速度

    [Header("滚轮缩放（可选）")]
    public bool enableZoom = true;
    public float zoomSpeed = 2f;
    public float minZoomDistance = 6f;
    public float maxZoomDistance = 15f;
    private float currentZoomDistance = 10f;

    // 缓存
    private Vector3 currentVelocity;

    void Start()
    {
        TryAssignPlayer(RuntimeRefs.PlayerTransform);

        // 初始化缩放距离
        currentZoomDistance = cameraOffset.magnitude;
    }

    void OnEnable()
    {
        RuntimeRefs.RegisterFixedCameraSystem(this);
        RuntimeRefs.PlayerTransformChanged += HandlePlayerTransformChanged;
    }

    void OnDisable()
    {
        RuntimeRefs.PlayerTransformChanged -= HandlePlayerTransformChanged;
        RuntimeRefs.UnregisterFixedCameraSystem(this);
    }

    void TryAssignPlayer(Transform playerTransform)
    {
        if (target != null || playerTransform == null)
        {
            return;
        }

        target = playerTransform;
        Debug.Log($"[FixedCamera] 已绑定 Player: {target.name}");
    }

    void HandlePlayerTransformChanged(Transform playerTransform)
    {
        if (target == null || target == playerTransform)
        {
            target = playerTransform;
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            TryAssignPlayer(RuntimeRefs.PlayerTransform);
            if (target == null) return;
        }

        // ===== 处理滚轮缩放 =====
        if (enableZoom)
        {
            float scroll = 0f;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
            if (Mouse.current != null)
                scroll = Mouse.current.scroll.ReadValue().y;
#else
            scroll = Input.mouseScrollDelta.y;
#endif

            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentZoomDistance -= scroll * 0.01f * zoomSpeed;
                currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);
            }
        }

        // ===== 计算相机位置 =====
        // 基于缩放比例调整偏移
        float zoomRatio = (currentZoomDistance - minZoomDistance) / (maxZoomDistance - minZoomDistance);
        zoomRatio = Mathf.Clamp01(zoomRatio);

        // 计算当前偏移（缩放）
        Vector3 offset = cameraOffset.normalized * currentZoomDistance;
        offset.y = cameraOffset.y * (0.5f + zoomRatio * 0.5f);  // 高度随缩放微调

        // 目标位置 = Player位置 + 偏移
        Vector3 targetPos = target.position + offset;

        // 平滑移动到目标位置
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, 1f / followSpeed);

        // 相机保持固定角度
        Quaternion targetRot = Quaternion.Euler(pitchAngle, yawAngle, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);
    }

    /// <summary>
    /// 设置跟随目标
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// 设置视角偏移
    /// </summary>
    public void SetOffset(Vector3 offset)
    {
        cameraOffset = offset;
        currentZoomDistance = offset.magnitude;
    }

    /// <summary>
    /// 设置固定角度
    /// </summary>
    public void SetAngle(float pitch, float yaw)
    {
        pitchAngle = pitch;
        yawAngle = yaw;
    }
}
