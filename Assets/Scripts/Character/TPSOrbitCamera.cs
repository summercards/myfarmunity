using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;   // Input System 1.x
#endif

public class TPSOrbitCamera : MonoBehaviour
{
    public Transform target;
    public TPSInput input; // ���������������� Look ֵ���ֱ�/����ڱ��ű��ڷֱ��ȡ

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
    public bool requireRightMouseHold = true;   // ����ס�Ҽ�ʱ��ת��ͷ
    public bool lockCursorWhileHolding = false; // ��ס�Ҽ�ʱ�������

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

        // ===== 1) �ɼ� Look ���� =====
        Vector2 look = Vector2.zero;

#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
        // �ֱ���ҡ�ˣ�ʼ������
        if (Gamepad.current != null)
            look += Gamepad.current.rightStick.ReadValue();

        // ��꣺���Ҽ���סʱ�Ŷ�ȡ
        bool rmbHeld = (Mouse.current != null && Mouse.current.rightButton.isPressed);
        if (!requireRightMouseHold || rmbHeld)
        {
            if (Mouse.current != null)
                look += Mouse.current.delta.ReadValue();
        }

        // ��ѡ����ס�Ҽ��������
        if (lockCursorWhileHolding)
        {
            if (rmbHeld) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            else         { Cursor.lockState = CursorLockMode.None;   Cursor.visible = true;  }
        }
#else
        // �����룺�ֱ��Թ���ֻ�������
        bool rmbHeld = Input.GetMouseButton(1);
        if (!requireRightMouseHold || rmbHeld)
            look += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        if (lockCursorWhileHolding)
        {
            if (rmbHeld) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }
#endif

        // ===== 2) Ӧ����ת =====
        yaw += look.x * yawSpeed * Time.deltaTime;
        pitch -= look.y * pitchSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPos = target.position - rot * Vector3.forward * distance;

        // ===== 3) �������ţ������Ҽ����ƣ�=====
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

        // ===== 4) ��ײ���� =====
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
