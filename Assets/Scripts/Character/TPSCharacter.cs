// Assets/Scripts/Character/TPSCharacter.cs
using UnityEngine;

/// <summary>
/// ��ɫ���ģ��˶������������⡢��Ծ��������״̬��
/// ��Ҫ CharacterController ���
/// ��ѡ Animator��Speed/Grounded/Jump ������
/// ���޸���վ�ڰڷ��Buildable �㣩���޷��еص����⣻�����ƶ�/ת���߼����ֲ��䡣
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class TPSCharacter : MonoBehaviour
{
    [Header("Refs")]
    public TPSInput input;                 // ��ͬ����򳡾�����
    public Transform cameraRoot;           // �ɵĲο��������Ŧ����������
    public Animator animator;              // ��Ϊ��

    [Header("Move")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 5.5f;
    public float acceleration = 20f;       // 水平加速度
    public float rotationSpeed = 540f;     // 转身速度

    [Header("移动模式")]
    public MovementMode movementMode = MovementMode.CameraRelative;

    public enum MovementMode
    {
        CameraRelative,    // 第三人称：基于相机朝向移动
        WorldRelative      // 固定视角：基于世界坐标移动
    }

    [Header("Jump/Gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -20f;           // ���£���ֵ��
    public float airControl = 0.5f;        // ���п��Ʊ�
    public float coyoteTime = 0.12f;       // ��غ��ʱ�������
    public float jumpBuffer = 0.12f;       // ��ǰ����Ծ����

    [Header("Ground Check")]
    public Vector3 groundCheckOffset = new Vector3(0, 0.1f, 0);
    public float groundCheckRadius = 0.3f;
    public LayerMask groundMask = ~0;

    CharacterController cc;
    TPSStateMachine fsm;
    Camera _cachedMainCamera;

    // ����̬
    Vector3 velocity;       // ������ֱ�ٶ�
    Vector3 planarVel;      // ˮƽ�ٶ�
    float lastGroundedTime;
    float lastJumpPressedTime;
    bool grounded;

    // ״̬ʵ��
    State stGrounded, stAir;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!input) input = GetComponent<TPSInput>();
        RuntimeRefs.RegisterPlayerCharacter(this);

        // �� �Զ��� Buildable ͼ�㲢������⣨�������ｨ�˸ò㣩
        int buildable = LayerMask.NameToLayer("Buildable");
        if (buildable >= 0)
            groundMask |= (1 << buildable);
    }

    void Start()
    {
        // 缓存主相机引用
        RefreshCachedCamera();

        // 状态机
        fsm = new TPSStateMachine();
        stGrounded = new GroundedState(this);
        stAir = new AirborneState(this);
        fsm.Init(stGrounded);
    }

    /// <summary>
    /// 刷新缓存的相机引用。用于场景切换或相机模式变更后
    /// </summary>
    public void RefreshCachedCamera()
    {
        // 优先使用 CameraModeManager 的当前活动相机
        if (CameraModeManager.instance != null && CameraModeManager.instance.activeCamera != null)
        {
            _cachedMainCamera = CameraModeManager.instance.activeCamera;
        }
        else
        {
            _cachedMainCamera = Camera.main;
        }
        Debug.Log($"[TPSCharacter] 刷新相机缓存: {_cachedMainCamera?.name ?? "null"}");
    }

    void OnDestroy()
    {
        RuntimeRefs.UnregisterPlayerCharacter(this);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // ����ʱ��������ڻ�������
        if (input && input.JumpPressed)
            lastJumpPressedTime = Time.time;

        // �� �����⣨���ȣ���cc.isGrounded || SphereCast������ Trigger��
        bool ccGround = cc.isGrounded;

        // ���ȡ��ɫ������ײ����Ϸ�������Ƕ��
        Vector3 origin = GetGroundCheckOrigin();   // ���·�����
        float radius = Mathf.Max(0.01f, groundCheckRadius);
        bool sphereHit = Physics.SphereCast(
            origin, radius, Vector3.down, out _,     // ������Ϣ�˴���ʹ��
            0.2f,                                    // �̾���̽��
            groundMask,
            QueryTriggerInteraction.Ignore);

        grounded = ccGround || sphereHit;

        if (grounded) lastGroundedTime = Time.time;

        fsm.Tick(dt);

        // ������������ѡ��
        if (animator)
        {
            float speed = new Vector2(planarVel.x, planarVel.z).magnitude;
            animator.SetFloat("Speed", speed);
            animator.SetBool("Grounded", grounded);
        }
    }

    // === ��������״̬��ȡ/���� ===

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
        // �ڵ�ʱ�����أ���ֹ�������յ�����
        if (grounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * dt;
    }

    public void Jump()
    {
        // v = sqrt(2gh); gravity Ϊ��
        velocity.y = Mathf.Sqrt(Mathf.Max(0.01f, -2f * gravity * jumpHeight));
        animator?.SetTrigger("Jump");
        ConsumeBufferedJump();
    }

    /// <summary>
    /// 移动和转向逻辑。根据movementMode选择不同的移动方式
    /// </summary>
    public void MovePlanar(float dt, Vector2 inputMove, bool sprint)
    {
        Vector3 desired;
        float targetSpeed = sprint ? sprintSpeed : walkSpeed;

        if (movementMode == MovementMode.CameraRelative)
        {
            // 第三人称模式：基于相机朝向移动
            // 关键：使用相机的观察方向，而不是CameraPivot的forward
            // 因为相机在target后方，CameraPivot.forward指向相机后方，而不是玩家应该走向的方向

            Camera cam = _cachedMainCamera;
            if (cam == null)
            {
                // 尝试重新获取相机
                cam = Camera.main;
            }

            if (cam != null)
            {
                // 使用相机观察方向（从相机指向target）的反方向
                // 这样W就是走向相机看到的前方
                Vector3 toCamera = (cam.transform.position - transform.position).normalized;
                Vector3 camF = Vector3.ProjectOnPlane(-toCamera, Vector3.up).normalized;
                Vector3 camR = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;

                desired = camF * inputMove.y + camR * inputMove.x;
            }
            else if (cameraRoot != null)
            {
                // 备用：使用cameraRoot
                Vector3 camF = Vector3.ProjectOnPlane(cameraRoot.forward, Vector3.up).normalized;
                Vector3 camR = Vector3.ProjectOnPlane(cameraRoot.right, Vector3.up).normalized;
                desired = camF * inputMove.y + camR * inputMove.x;
            }
            else
            {
                // 最后备用：使用自身transform
                desired = Vector3.forward * inputMove.y + Vector3.right * inputMove.x;
            }
        }
        else
        {
            // 固定视角模式：基于世界坐标移动（上下左右就是世界上下左右）
            desired = Vector3.forward * inputMove.y + Vector3.right * inputMove.x;
        }

        desired = desired.sqrMagnitude > 1e-4f ? desired.normalized : Vector3.zero;
        Vector3 targetVel = desired * targetSpeed;

        // 空中/地面不同加速度
        float acc = grounded ? acceleration : (acceleration * airControl);
        planarVel = Vector3.MoveTowards(new Vector3(planarVel.x, 0, planarVel.z), new Vector3(targetVel.x, 0, targetVel.z), acc * dt);

        // 有输入时转向（只改变朝向，不改变移动方向）
        if (desired.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desired, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * dt);
        }
    }

    public void ApplyMotion(float dt)
    {
        // �ϲ�ˮƽ + ��ֱ
        Vector3 motion = new Vector3(planarVel.x, 0, planarVel.z) + Vector3.up * velocity.y;
        cc.Move(motion * dt);
    }

    // === ����״̬ ===

    class GroundedState : State
    {
        public GroundedState(TPSCharacter o) : base(o) { }
        public override void OnEnter()
        {
            // ����/������ֱ�ٶ�
            if (owner.velocity.y < 0f) owner.velocity.y = -2f;
        }

        public override void Tick(float dt)
        {
            var inp = owner.input;
            // �ƶ�
            owner.MovePlanar(dt, inp ? inp.Move : Vector2.zero, inp && inp.SprintHeld);

            // ��Ծ���ڵ� �� Coyote + ����
            bool wantJump = (inp && inp.JumpPressed) || owner.HasBufferedJump();
            if (wantJump && (owner.IsGrounded() || owner.CanCoyoteJump()))
            {
                owner.Jump();
            }

            // ���� & �˶�
            owner.ApplyGravity(dt);
            owner.ApplyMotion(dt);

            // ״̬�л�
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
            // ������������ˮƽ����
            owner.MovePlanar(dt, inp ? inp.Move : Vector2.zero, inp && inp.SprintHeld);

            // ������ֻ�����ǰ��¼�����ڿ��ж���������Ҫ�Ӷ����������ڴ��жϣ�
            if (inp && inp.JumpPressed)
                owner.lastJumpPressedTime = Time.time;

            owner.ApplyGravity(dt);
            owner.ApplyMotion(dt);

            if (owner.IsGrounded())
                owner.fsm.Change(owner.stGrounded);
        }
    }

    // === ���ߣ��� CharacterController �ײ�Ϊ��׼�ļ����� ===
    Vector3 GetGroundCheckOrigin()
    {
        // �Խ��ҵײ�Ϊ��׼���ټ���������ƫ�ƣ������������ڱ����ڲ�
        Vector3 bottom = transform.position + cc.center + Vector3.down * (cc.height * 0.5f - cc.radius);
        return bottom + Vector3.up * 0.05f + groundCheckOffset;
    }
}
