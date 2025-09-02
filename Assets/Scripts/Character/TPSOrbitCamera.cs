using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;   // Input System 1.x
#endif

public class TPSOrbitCamera : MonoBehaviour
{
    public Transform target;
    public TPSInput input; // 保留，不再用它的 Look 值；手柄/鼠标在本脚本内分别读取

    [Header("Orbit")]
    public float distance = 3.5f;
    public float minDistance = 1.0f;
    public float maxDistance = 5.5f;
    public Vector2 pitchLimits = new Vector2(-30f, 70f);
    public float yawSpeed = 120f;
    public float pitchSpeed = 120f;
    public float zoomSpeed = 2.0f;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.2f;

    [Header("Mouse Look Gate")]
    public bool requireRightMouseHold = true;   // 仅按住右键时才转镜头
    public bool lockCursorWhileHolding = false; // 按住右键时锁定鼠标

    float yaw, pitch;

    void Start()
    {
        if (target)
        {
            Vector3 dir = (transform.position - target.position).normalized;
            yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            pitch = Mathf.Asin(dir.y) * Mathf.Rad2Deg;
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // ===== 1) 采集 Look 输入 =====
        Vector2 look = Vector2.zero;

#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
        // 手柄右摇杆：始终允许
        if (Gamepad.current != null)
            look += Gamepad.current.rightStick.ReadValue();

        // 鼠标：仅右键按住时才读取
        bool rmbHeld = (Mouse.current != null && Mouse.current.rightButton.isPressed);
        if (!requireRightMouseHold || rmbHeld)
        {
            if (Mouse.current != null)
                look += Mouse.current.delta.ReadValue();
        }

        // 可选：按住右键锁定鼠标
        if (lockCursorWhileHolding)
        {
            if (rmbHeld) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            else         { Cursor.lockState = CursorLockMode.None;   Cursor.visible = true;  }
        }
#else
        // 旧输入：手柄略过，只处理鼠标
        bool rmbHeld = Input.GetMouseButton(1);
        if (!requireRightMouseHold || rmbHeld)
            look += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        if (lockCursorWhileHolding)
        {
            if (rmbHeld) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }
#endif

        // ===== 2) 应用旋转 =====
        yaw += look.x * yawSpeed * Time.deltaTime;
        pitch -= look.y * pitchSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPos = target.position - rot * Vector3.forward * distance;

        // ===== 3) 滚轮缩放（不受右键限制）=====
        float scroll = 0f;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
        if (Mouse.current != null)
            scroll = Mouse.current.scroll.ReadValue().y;
#else
        scroll = Input.mouseScrollDelta.y;
#endif
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance = Mathf.Clamp(distance - scroll * 0.01f * zoomSpeed, minDistance, maxDistance);
            desiredPos = target.position - rot * Vector3.forward * distance;
        }

        // ===== 4) 碰撞回缩 =====
        Vector3 dir = (desiredPos - target.position);
        if (Physics.SphereCast(target.position, collisionRadius, dir.normalized,
            out var hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            float hitDist = Mathf.Max(hit.distance - 0.05f, minDistance);
            desiredPos = target.position - rot * Vector3.forward * hitDist;
        }

        transform.SetPositionAndRotation(desiredPos, rot);
    }
}
