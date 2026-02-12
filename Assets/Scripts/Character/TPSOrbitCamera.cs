using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;   // Input System 1.x
#endif

public class TPSOrbitCamera : MonoBehaviour
{
    public Transform target;
    public TPSInput input;

    [Header("Orbit")]
    public float distance = 3.5f;
    public float minDistance = 1.0f;
    public float maxDistance = 5.5f;
    public Vector2 pitchLimits = new Vector2(-30f, 70f);
    public float yawSpeed = 120f;
    public float pitchSpeed = 120f;
    public float zoomSpeed = 2.0f;

    [Header("碰撞")]
    public LayerMask collisionMask = ~0; // 默认检测所有层
    public float collisionRadius = 0.2f;
    [Tooltip("如果勾选，则只检测 collisionMask 指定的层，否则检测所有层")]
    public bool useSpecificLayers = false;

    [Header("Mouse Look Gate")]
    public bool requireRightMouseHold = true;
    public bool lockCursorWhileHolding = false;

    float yaw, pitch;

    void Start()
    {
        if (target)
        {
            // 设置初始角度：俯视 25 度
            pitch = 25f;
            yaw = 0f;
            
            // 设置摄像机初始位置
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 desiredPos = target.position - rot * Vector3.forward * distance;
            
            transform.SetPositionAndRotation(desiredPos, rot);
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // ===== 1) 获取 Look 输入 =====
        Vector2 look = Vector2.zero;

#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
        // 手柄：摇杆开始检测
        if (Gamepad.current != null)
            look += Gamepad.current.rightStick.ReadValue();

        // 键盘：鼠标右键按住时才处理
        bool rmbHeld = (Mouse.current != null && Mouse.current.rightButton.isPressed);
        if (!requireRightMouseHold || rmbHeld)
        {
            if (Mouse.current != null)
                look += Mouse.current.delta.ReadValue();
        }

        // 鼠标：按住右键时锁定光标
        if (lockCursorWhileHolding)
        {
            if (rmbHeld) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            else         { Cursor.lockState = CursorLockMode.None;   Cursor.visible = true;  }
        }
#else
        // 旧输入：手柄只有左右，没有鼠标
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

        // ===== 3) 检测滚轮（鼠标滚轮控制）=====
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

        // ===== 4) 碰撞检测（只在指定层上检测，避免与 Player 自身碰撞）=====
        Vector3 dir = (target.position - desiredPos).normalized; // 从摄像机指向目标
        float actualDistance = distance;
        
        // 只在 useSpecificLayers 为 true 且 collisionMask 不为 0 时才进行碰撞检测
        if (useSpecificLayers && (int)collisionMask != 0)
        {
            // 从摄像机期望位置向 target 方向投射，避免与 target 自身碰撞
            if (Physics.SphereCast(desiredPos + dir * 0.1f, collisionRadius, dir,
                out var hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                // 只检测到不是 target 的物体才移动摄像机
                if (hit.transform != target)
                {
                    actualDistance = Mathf.Max(hit.distance - 0.05f, minDistance);
                    desiredPos = desiredPos + dir * (distance - actualDistance);
                }
            }
        }

        transform.SetPositionAndRotation(desiredPos, rot);
    }
}