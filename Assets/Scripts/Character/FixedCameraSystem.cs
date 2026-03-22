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
        // 如果 target 为空，尝试查找 Player
        if (target == null)
        {
            TryFindPlayer();
        }

        // 初始化缩放距离
        currentZoomDistance = cameraOffset.magnitude;
    }

    void TryFindPlayer()
    {
        // 方法1：通过tag查找
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            Debug.Log($"[FixedCamera] 绑定Player (by tag): {player.name}");
            return;
        }

        // 方法2：通过TPSCharacter组件查找
        TPSCharacter tps = FindObjectOfType<TPSCharacter>();
        if (tps != null)
        {
            target = tps.transform;
            Debug.Log($"[FixedCamera] 绑定Player (by TPSCharacter): {tps.name}");
            return;
        }

        Debug.LogWarning("[FixedCamera] 找不到Player!");
    }

    void LateUpdate()
    {
        if (target == null)
        {
            TryFindPlayer();
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
