using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class HeldItemDisplay : MonoBehaviour
{
    [Header("Socket / Animator")]
    [Tooltip("优先使用这个 Transform 做手部插槽；留空则自动寻找 HandSocket 或 Humanoid 右手骨骼。")]
    public Transform handSocketOverride;

    [Tooltip("角色模型上的 Animator（Humanoid 可自动取右手骨骼）。留空则自动在子物体中查找。")]
    public Animator animator;

    [Header("Behaviour")]
    [Tooltip("默认显示秒数；<=0 表示不自动隐藏（一直持有）。")]
    public float defaultShowSeconds = 1.2f;

    [Tooltip("实例化后强制激活（有些预制体如果是 Inactive 会导致看不到）。")]
    public bool forceActiveOnSpawn = true;

    [Header("Debug")]
    public bool logDebug = true;
    [Tooltip("调试用：点上下文菜单可直接在手上显示这个物品，无需进入拾取流程。")]
    public ItemSO debugTestItem;

    Transform _hand;       // 运行时缓存
    GameObject _current;   // 当前手里的实例

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        _hand = ResolveHand();   // 先尝试缓存
    }

    Transform ResolveHand()
    {
        if (handSocketOverride) return handSocketOverride;

        // 1) 尝试在层级里按名字找 "HandSocket"
        var t = FindDeepChildByName(transform, "HandSocket");
        if (t) return t;

        // 2) Humanoid：取右手骨骼
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
            if (logDebug) Debug.Log($"[HeldItemDisplay] '{item.name}' 没有设置 heldPrefab，不显示。");
            return;
        }

        var hand = _hand ? _hand : (_hand = ResolveHand());
        if (!hand)
        {
            Debug.LogWarning("[HeldItemDisplay] 没有找到手部插槽（HandSocket/右手骨骼）。");
            return;
        }

        // 重要：用 (parent, worldPositionStays=false) 作为子物体实例化，直接走局部坐标
        _current = Instantiate(item.heldPrefab, hand, false);

        if (forceActiveOnSpawn && !_current.activeSelf) _current.SetActive(true);

        // 套用 ItemSO 中的局部偏移
        _current.transform.localPosition = item.heldLocalPosition;
        _current.transform.localEulerAngles = item.heldLocalEulerAngles;
        _current.transform.localScale = (item.heldLocalScale == Vector3.zero ? Vector3.one : item.heldLocalScale);

        if (seconds < 0f) seconds = defaultShowSeconds;
        if (seconds > 0f) StartCoroutine(HideAfter(seconds));

        if (logDebug) Debug.Log($"[HeldItemDisplay] 显示在手上：{item.name} （parent={hand.name}）");
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
    // 右键组件标题栏的三点 → Context Menu
    [ContextMenu("Debug/Show Debug Item")]
    void Debug_Show()
    {
        if (debugTestItem) Show(debugTestItem, 0f);
        else Debug.LogWarning("[HeldItemDisplay] 请先在 debugTestItem 指定一个 ItemSO。");
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
