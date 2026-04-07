using UnityEngine;
using FarmGame.Core;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;   // Input System 1.x
#endif

public class TPSOrbitCamera : MonoBehaviour
{
    private static TPSOrbitCamera instance;

    public Transform target;
    public TPSInput input;

    void Awake()
    {
        if (!RuntimeService.TryClaimSingleton(this, instance, nameof(TPSOrbitCamera)))
        {
            return;
        }

        instance = this;
        RuntimeRefs.RegisterTpsOrbitCamera(this);
    }

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
    public bool useSpecificLayers = true;  // 默认启用碰撞检测

    [Header("Mouse Look Gate")]
    public bool requireRightMouseHold = true;
    public bool lockCursorWhileHolding = false;

    float yaw, pitch;

    void Start()
    {
        // 每次场景加载时都尝试恢复 target 引用
        RefreshTargetReference();

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

    void OnEnable()
    {
        RuntimeRefs.RegisterTpsOrbitCamera(this);
        RuntimeRefs.PlayerTransformChanged += HandlePlayerTransformChanged;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        RuntimeRefs.PlayerTransformChanged -= HandlePlayerTransformChanged;
        RuntimeRefs.UnregisterTpsOrbitCamera(this);
    }

    void HandleSceneLoaded(Scene _, LoadSceneMode __)
    {
        RefreshTargetReference();
    }

    /// <summary>
    /// 在场景加载后刷新 target 引用
    /// 解决场景切换时 Inspector 引用丢失的问题
    /// </summary>
    void RefreshTargetReference()
    {
        Transform player = RuntimeRefs.PlayerTransform;
        if (player != null)
        {
            Transform cameraPivot = player.Find("CameraPivot");
            if (cameraPivot != null)
            {
                target = cameraPivot;
                Debug.Log($"[TPSOrbitCamera] ✅ 已恢复 target: {cameraPivot.name}");
            }
            else
            {
                target = player;
                Debug.Log($"[TPSOrbitCamera] ⚠️ CameraPivot 未找到，使用 Player 作为 target: {player.name}");
            }
            return;
        }

        if (target == null)
            Debug.LogWarning("[TPSOrbitCamera] ❌ 无法找到 Player 对象");
    }

    void HandlePlayerTransformChanged(Transform _)
    {
        RefreshTargetReference();
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

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
