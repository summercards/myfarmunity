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
    public float acceleration = 20f;       // ˮƽ����
    public float rotationSpeed = 540f;     // �泯�ƶ�����

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

        // �� �Զ��� Buildable ͼ�㲢������⣨�������ｨ�˸ò㣩
        int buildable = LayerMask.NameToLayer("Buildable");
        if (buildable >= 0)
            groundMask |= (1 << buildable);
    }

    void Start()
    {
        // 缓存主相机引用
        _cachedMainCamera = Camera.main;

        // 状态机
        fsm = new TPSStateMachine();
        stGrounded = new GroundedState(this);
        stAir = new AirborneState(this);
        fsm.Init(stGrounded);
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
    /// �ƶ���ת���߼���ȫ�����㵱ǰ�汾��δ�Ķ�
    /// </summary>
    public void MovePlanar(float dt, Vector2 inputMove, bool sprint)
    {
        // ȡ��λ�ο�
        Transform src = null;
        if (_cachedMainCamera) src = _cachedMainCamera.transform;  //) ��ѡ��������峯��
        else if (cameraRoot) src = cameraRoot;                 // ��ѡ�������Ŧ�����ܲ��������ת��
        else src = transform;                                  // ��󶵵�

        // �ɲο�����õ�ˮƽ���ǰ/��
        Vector3 camF = Vector3.ProjectOnPlane(src.forward, Vector3.up).normalized;
        Vector3 camR = Vector3.ProjectOnPlane(src.right, Vector3.up).normalized;

        // ����ӳ�䵽���緽��W=camF��D=camR��
        Vector3 desired = camF * inputMove.y + camR * inputMove.x;
        desired = desired.sqrMagnitude > 1e-4f ? desired.normalized : Vector3.zero;

        float targetSpeed = sprint ? sprintSpeed : walkSpeed;
        Vector3 targetVel = desired * targetSpeed;

        // ��������в�ͬ����
        float acc = grounded ? acceleration : (acceleration * airControl);
        planarVel = Vector3.MoveTowards(new Vector3(planarVel.x, 0, planarVel.z), new Vector3(targetVel.x, 0, targetVel.z), acc * dt);

        // ����������ʱת�������ֻ�����������ֱ�ӸĽ�ɫ��ת��
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
