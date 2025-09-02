// Assets/Scripts/Character/TPSCharacter.cs
using UnityEngine;

/// <summary>
/// 角色核心：运动参数、地面检测、跳跃、重力、状态机
/// 需要 CharacterController 组件
/// 可选 Animator（Speed/Grounded/Jump 触发）
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class TPSCharacter : MonoBehaviour
{
    [Header("Refs")]
    public TPSInput input;                 // 从同物体或场景引用
    public Transform cameraRoot;           // 旧的参考（相机枢纽），可留空
    public Animator animator;              // 可为空

    [Header("Move")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 5.5f;
    public float acceleration = 20f;       // 水平加速
    public float rotationSpeed = 540f;     // 面朝移动方向

    [Header("Jump/Gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -20f;           // 向下（负值）
    public float airControl = 0.5f;        // 空中控制比
    public float coyoteTime = 0.12f;       // 离地后短时间可起跳
    public float jumpBuffer = 0.12f;       // 提前按跳跃缓冲

    [Header("Ground Check")]
    public Vector3 groundCheckOffset = new Vector3(0, 0.1f, 0);
    public float groundCheckRadius = 0.3f;
    public LayerMask groundMask = ~0;

    CharacterController cc;
    TPSStateMachine fsm;

    // 运行态
    Vector3 velocity;       // 包含垂直速度
    Vector3 planarVel;      // 水平速度
    float lastGroundedTime;
    float lastJumpPressedTime;
    bool grounded;

    // 状态实例
    State stGrounded, stAir;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!input) input = GetComponent<TPSInput>();
    }

    void Start()
    {
        // 状态机
        fsm = new TPSStateMachine();
        stGrounded = new GroundedState(this);
        stAir = new AirborneState(this);
        fsm.Init(stGrounded);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 输入时间戳（用于缓冲跳）
        if (input && input.JumpPressed)
            lastJumpPressedTime = Time.time;

        // 地面检测（比 cc.isGrounded 更稳）
        grounded = Physics.CheckSphere(transform.position + groundCheckOffset, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
        if (grounded) lastGroundedTime = Time.time;

        fsm.Tick(dt);

        // 动画参数（可选）
        if (animator)
        {
            float speed = new Vector2(planarVel.x, planarVel.z).magnitude;
            animator.SetFloat("Speed", speed);
            animator.SetBool("Grounded", grounded);
        }
    }

    // === 公共：供状态读取/操作 ===

    public bool IsGrounded() => grounded;
    public bool CanCoyoteJump() => (Time.time - lastGroundedTime) <= coyoteTime;
    public bool HasBufferedJump() => (Time.time - lastJumpPressedTime) <= jumpBuffer;

    public void ConsumeBufferedJump()
    {
        lastJumpPressedTime = -999f;
        if (input) input.ConsumeJump();
    }

    public void ApplyGravity(float dt)
    {
        // 在地时轻贴地（防止下坡悬空弹跳）
        if (grounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * dt;
    }

    public void Jump()
    {
        // v = sqrt(2gh); gravity 为负
        velocity.y = Mathf.Sqrt(Mathf.Max(0.01f, -2f * gravity * jumpHeight));
        animator?.SetTrigger("Jump");
        ConsumeBufferedJump();
    }

    /// <summary>
    /// 核心改动：移动方向参考使用 Main Camera（若不存在再回退到 cameraRoot / 自身）
    /// </summary>
    public void MovePlanar(float dt, Vector2 inputMove, bool sprint)
    {
        // 取方位参考
        Transform src = null;
        if (Camera.main) src = Camera.main.transform;          // 首选：相机本体朝向
        else if (cameraRoot) src = cameraRoot;                 // 备选：相机枢纽（可能不随相机旋转）
        else src = transform;                                  // 最后兜底

        // 由参考朝向得到水平面的前/右
        Vector3 camF = Vector3.ProjectOnPlane(src.forward, Vector3.up).normalized;
        Vector3 camR = Vector3.ProjectOnPlane(src.right, Vector3.up).normalized;

        // 输入映射到世界方向（W=camF，D=camR）
        Vector3 desired = camF * inputMove.y + camR * inputMove.x;
        desired = desired.sqrMagnitude > 1e-4f ? desired.normalized : Vector3.zero;

        float targetSpeed = sprint ? sprintSpeed : walkSpeed;
        Vector3 targetVel = desired * targetSpeed;

        // 地面与空中不同加速
        float acc = grounded ? acceleration : (acceleration * airControl);
        planarVel = Vector3.MoveTowards(new Vector3(planarVel.x, 0, planarVel.z), new Vector3(targetVel.x, 0, targetVel.z), acc * dt);

        // 仅当有输入时转身（鼠标只控制相机，不直接改角色旋转）
        if (desired.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desired, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * dt);
        }
    }

    public void ApplyMotion(float dt)
    {
        // 合并水平 + 垂直
        Vector3 motion = new Vector3(planarVel.x, 0, planarVel.z) + Vector3.up * velocity.y;
        cc.Move(motion * dt);
    }

    // === 具体状态 ===

    class GroundedState : State
    {
        public GroundedState(TPSCharacter o) : base(o) { }
        public override void OnEnter()
        {
            // 贴地/清理垂直速度
            if (owner.velocity.y < 0f) owner.velocity.y = -2f;
        }

        public override void Tick(float dt)
        {
            var inp = owner.input;
            // 移动
            owner.MovePlanar(dt, inp ? inp.Move : Vector2.zero, inp && inp.SprintHeld);

            // 跳跃：在地 或 Coyote + 缓冲
            bool wantJump = (inp && inp.JumpPressed) || owner.HasBufferedJump();
            if (wantJump && (owner.IsGrounded() || owner.CanCoyoteJump()))
            {
                owner.Jump();
            }

            // 重力 & 运动
            owner.ApplyGravity(dt);
            owner.ApplyMotion(dt);

            // 状态切换
            if (!owner.IsGrounded())
                owner.fsm.Change(owner.stAir);
        }
    }

    class AirborneState : State
    {
        public AirborneState(TPSCharacter o) : base(o) { }
        public override void Tick(float dt)
        {
            var inp = owner.input;
            // 空中仍有少量水平控制
            owner.MovePlanar(dt, inp ? inp.Move : Vector2.zero, inp && inp.SprintHeld);

            // 缓冲跳只在落地前记录，不在空中二段跳（若要加二段跳，可在此判断）
            if (inp && inp.JumpPressed)
                owner.lastJumpPressedTime = Time.time;

            owner.ApplyGravity(dt);
            owner.ApplyMotion(dt);

            if (owner.IsGrounded())
                owner.fsm.Change(owner.stGrounded);
        }
    }
}
