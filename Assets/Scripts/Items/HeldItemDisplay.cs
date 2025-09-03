using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class HeldItemDisplay : MonoBehaviour
{
    [Header("Socket / Animator")]
    [Tooltip("����ʹ����� Transform ���ֲ���ۣ��������Զ�Ѱ�� HandSocket �� Humanoid ���ֹ�����")]
    public Transform handSocketOverride;

    [Tooltip("��ɫģ���ϵ� Animator��Humanoid ���Զ�ȡ���ֹ��������������Զ����������в��ҡ�")]
    public Animator animator;

    [Header("Behaviour")]
    [Tooltip("Ĭ����ʾ������<=0 ��ʾ���Զ����أ�һֱ���У���")]
    public float defaultShowSeconds = 1.2f;

    [Tooltip("ʵ������ǿ�Ƽ����ЩԤ��������� Inactive �ᵼ�¿���������")]
    public bool forceActiveOnSpawn = true;

    [Header("Debug")]
    public bool logDebug = true;
    [Tooltip("�����ã��������Ĳ˵���ֱ����������ʾ�����Ʒ���������ʰȡ���̡�")]
    public ItemSO debugTestItem;

    Transform _hand;       // ����ʱ����
    GameObject _current;   // ��ǰ�����ʵ��

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        _hand = ResolveHand();   // �ȳ��Ի���
    }

    Transform ResolveHand()
    {
        if (handSocketOverride) return handSocketOverride;

        // 1) �����ڲ㼶�ﰴ������ "HandSocket"
        var t = FindDeepChildByName(transform, "HandSocket");
        if (t) return t;

        // 2) Humanoid��ȡ���ֹ���
        if (animator && animator.isHuman)
        {
            var bone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (bone) return bone;
        }

        return null;
    }

    static Transform FindDeepChildByName(Transform root, string name)
    {
        foreach (Transform c in root)
        {
            if (c.name == name) return c;
            var r = FindDeepChildByName(c, name);
            if (r) return r;
        }
        return null;
    }

    public void Show(ItemSO item, float seconds = -1f)
    {
        Clear();

        if (item == null)
        {
            if (logDebug) Debug.LogWarning("[HeldItemDisplay] item is null.");
            return;
        }
        if (!item.heldPrefab)
        {
            if (logDebug) Debug.Log($"[HeldItemDisplay] '{item.name}' û������ heldPrefab������ʾ��");
            return;
        }

        var hand = _hand ? _hand : (_hand = ResolveHand());
        if (!hand)
        {
            Debug.LogWarning("[HeldItemDisplay] û���ҵ��ֲ���ۣ�HandSocket/���ֹ�������");
            return;
        }

        // ��Ҫ���� (parent, worldPositionStays=false) ��Ϊ������ʵ������ֱ���߾ֲ�����
        _current = Instantiate(item.heldPrefab, hand, false);

        if (forceActiveOnSpawn && !_current.activeSelf) _current.SetActive(true);

        // ���� ItemSO �еľֲ�ƫ��
        _current.transform.localPosition = item.heldLocalPosition;
        _current.transform.localEulerAngles = item.heldLocalEulerAngles;
        _current.transform.localScale = (item.heldLocalScale == Vector3.zero ? Vector3.one : item.heldLocalScale);

        if (seconds < 0f) seconds = defaultShowSeconds;
        if (seconds > 0f) StartCoroutine(HideAfter(seconds));

        if (logDebug) Debug.Log($"[HeldItemDisplay] ��ʾ�����ϣ�{item.name} ��parent={hand.name}��");
    }

    IEnumerator HideAfter(float s)
    {
        yield return new WaitForSeconds(s);
        Clear();
    }

    public void Clear()
    {
        if (_current) Destroy(_current);
        _current = null;
    }

#if UNITY_EDITOR
    // �Ҽ���������������� �� Context Menu
    [ContextMenu("Debug/Show Debug Item")]
    void Debug_Show()
    {
        if (debugTestItem) Show(debugTestItem, 0f);
        else Debug.LogWarning("[HeldItemDisplay] ������ debugTestItem ָ��һ�� ItemSO��");
    }

    [ContextMenu("Debug/Clear")]
    void Debug_Clear() => Clear();

    void OnDrawGizmosSelected()
    {
        var hand = handSocketOverride ? handSocketOverride : ResolveHand();
        if (!hand) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(hand.position, 0.03f);
    }
#endif
}
