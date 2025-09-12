// Assets/Scripts/Build/PlayerBuilder.cs
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif

/// <summary>
/// ���ס�����/��԰�ڷš���
/// - ����ֳ�ĳ����Ʒ��ActiveItemController.ActiveId��
/// - ������Ʒ�� BuildCatalogSO ������Ŀ�������Ԥ��/�ڷ�ģʽ
/// - �������ã��۳���������1��ʵ�������壬����¼�浵
/// </summary>
[RequireComponent(typeof(PlayerInventoryHolder))]
[RequireComponent(typeof(ActiveItemController))]
public class PlayerBuilder : MonoBehaviour
{
    [Header("Catalog & Raycast")]
    public BuildCatalogSO catalog;
    public float maxDistance = 8f;

    [Tooltip("��������Ԥ��/�ڷŵĲ㣨ͨ���ǵ��桢ǽ�塢����ȣ�")]
    public LayerMask surfaceLayers = ~0;

    [Header("Preview")]
    public Material previewValidMat;
    public Material previewInvalidMat;
    public float previewAlpha = 0.6f;

    [Header("Keys")]
    public KeyCode placeKey = KeyCode.Mouse0;
    public KeyCode cancelKey = KeyCode.Mouse1;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;

    [Header("Camera")]
    public Camera viewCamera;  // ��ָ�����Զ�ץ�����

    // ����ʱ
    PlayerInventoryHolder _inv;
    ActiveItemController _active;
    GameObject _ghost;
    string _ghostItemId = "";
    float _yaw = 0f;                       // ��ɢ��ת�ۼ�
    BuildCatalogSO.Entry _entry;           // ��ǰ��Ʒ�Ĺ���

    // Ԥ�����ʻ���
    readonly List<(Renderer r, Material[] original)> _renderers = new();

    void Awake()
    {
        _inv = GetComponent<PlayerInventoryHolder>();
        _active = GetComponent<ActiveItemController>();
        if (!viewCamera) viewCamera = Camera.main;
    }

    void Update()
    {
        string id = _active ? _active.ActiveId : "";
        var entry = catalog ? catalog.Get(id) : null;

        if (entry == null)
        {
            ClearGhost();
            return;
        }

        // ���л�����Ʒ���ؽ�Ԥ��
        if (_ghost == null || _ghostItemId != id)
        {
            BuildGhost(entry, id);
        }

        // ��ת��ݼ�
        if (Input.GetKeyDown(rotateLeftKey)) _yaw -= entry.yawStep;
        if (Input.GetKeyDown(rotateRightKey)) _yaw += entry.yawStep;

        // ����������������̬
        bool ok = ComputePose(entry, out Vector3 pos, out Quaternion rot);

        // ����Ԥ��λ������ɫ
        if (_ghost)
        {
            _ghost.transform.SetPositionAndRotation(pos, rot);
            SetGhostValid(ok);
        }

        // �µ��ڷ�
        if (ok && GetPlacePressed())
        {
            if (TryConsumeOne(id))
            {
                Place(entry, id, pos, rot);
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("[PlayerBuilder] ������������");
#endif
            }
        }

        // �Ҽ�ȡ��Ԥ��
        if (Input.GetKeyDown(cancelKey))
            ClearGhost();
    }

    bool GetPlacePressed()
    {
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
        if (Mouse.current != null) return Mouse.current.leftButton.wasPressedThisFrame;
#endif
        return Input.GetKeyDown(placeKey);
    }

    bool TryConsumeOne(string id)
    {
        if (_inv == null) return false;
        return _inv.RemoveItem(id, 1) > 0;
    }

    void Place(BuildCatalogSO.Entry e, string id, Vector3 pos, Quaternion rot)
    {
        var parent = e.optionalParentAtRuntime ? e.optionalParentAtRuntime : null;
        var obj = Instantiate(e.prefab, pos, rot, parent);
        var po = obj.GetComponent<PlacedObject>();
        if (!po) po = obj.AddComponent<PlacedObject>();
        po.itemId = id;

        // ��ѡ���ٳ���Ͷһ������������ظ������ء�
        if (e.alignToGroundAfterPlace)
        {
            if (Physics.Raycast(obj.transform.position + Vector3.up * 0.1f, Vector3.down,
                out var hit, e.groundProbeDistance, e.surfaceLayers, QueryTriggerInteraction.Ignore))
            {
                obj.transform.position = hit.point + Vector3.up * e.yOffset;
            }
        }
    }

    bool ComputePose(BuildCatalogSO.Entry e, out Vector3 pos, out Quaternion rot)
    {
        // ���ӿ����ķ���һ������
        var cam = viewCamera ? viewCamera : Camera.main;
        var ray = cam ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)) :
                        new Ray(transform.position + Vector3.up * 1.5f, transform.forward);

        if (!Physics.Raycast(ray, out var hit, maxDistance, e.surfaceLayers, QueryTriggerInteraction.Ignore))
        {
            pos = Vector3.zero; rot = Quaternion.identity;
            return false;
        }

        // ���� snap ģʽ����̬
        Vector3 targetPos = hit.point;
        Quaternion targetRot = Quaternion.identity;

        switch (e.snapMode)
        {
            case BuildSnapMode.Ground:
                targetPos = SnapXZ(targetPos + Vector3.up * e.yOffset, e.grid);
                targetRot = Quaternion.Euler(0f, SnapYaw(_yaw, e.yawStep), 0f);
                break;
            case BuildSnapMode.Surface:
                targetPos = SnapXZ(targetPos + Vector3.up * e.yOffset, e.grid);
                targetRot = Quaternion.Euler(0f, SnapYaw(_yaw, e.yawStep), 0f);
                break;
            case BuildSnapMode.Wall:
                // �� forward ���� -���ߣ�������ң�
                var forward = -hit.normal; forward.y = 0f; forward.Normalize();
                var yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                yaw = SnapYaw(yaw + _yaw, e.yawStep);
                targetRot = Quaternion.Euler(0f, yaw, 0f);
                targetPos = hit.point + hit.normal * 0.01f;
                break;
        }

        // Overlap ��⣺ʹ�ú��壨��ߴ磩
        bool collide = Physics.CheckBox(
            targetPos,
            e.checkBoxSize,
            targetRot,
            e.blockerLayers,
            QueryTriggerInteraction.Ignore);

        pos = targetPos;
        rot = targetRot;
        return !collide;
    }

    static Vector3 SnapXZ(Vector3 p, float grid)
    {
        if (grid <= 0.0001f) return p;
        p.x = Mathf.Round(p.x / grid) * grid;
        p.z = Mathf.Round(p.z / grid) * grid;
        return p;
    }

    static float SnapYaw(float y, float step)
    {
        if (step <= 0.0001f) return y;
        return Mathf.Round(y / step) * step;
    }

    void BuildGhost(BuildCatalogSO.Entry entry, string id)
    {
        ClearGhost();

        if (entry == null || entry.prefab == null) return;

        _entry = entry;
        _ghostItemId = id;
        _yaw = 0f;

        _ghost = Instantiate(entry.prefab);
        _ghost.name = "[GHOST] " + entry.prefab.name;
        _ghost.transform.position = transform.position + transform.forward * 2f;

        foreach (var c in _ghost.GetComponentsInChildren<Collider>())
            c.enabled = false;

        foreach (var rb in _ghost.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;

        // ����ԭ���ʣ�������Ԥ������
        _renderers.Clear();
        foreach (var r in _ghost.GetComponentsInChildren<Renderer>())
        {
            if (!r) continue;
            _renderers.Add((r, r.sharedMaterials));
        }
        SetGhostValid(false);
    }

    void SetGhostValid(bool valid)
    {
        var mat = valid ? previewValidMat : previewInvalidMat;
        if (!mat) return;

        for (int i = 0; i < _renderers.Count; i++)
        {
            var (r, original) = _renderers[i];
            if (!r) continue;

            var arr = new Material[original.Length];
            for (int j = 0; j < arr.Length; j++) arr[j] = mat;
            r.sharedMaterials = arr;

            // ����͸��
            foreach (var m in r.sharedMaterials)
            {
                if (!m) continue;
                if (m.HasProperty("_Color"))
                {
                    var c = m.color; c.a = previewAlpha;
                    m.color = c;
                }
            }
        }
    }

    void ClearGhost()
    {
        if (_ghost) Destroy(_ghost);
        _ghost = null;
        _renderers.Clear();
        _ghostItemId = "";
        _entry = null;
    }
}
