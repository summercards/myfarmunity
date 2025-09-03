// Assets/Scripts/Items/HeldItemDisplay.cs
using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class HeldItemDisplay : MonoBehaviour
{
    [Header("Socket / Animator")]
    public Transform handSocketOverride;
    public Animator animator;

    [Header("Behaviour")]
    public float defaultShowSeconds = 0f; // 建议设 0：一直显示，直到丢弃/切换
    public bool forceActiveOnSpawn = true;

    [Header("Debug")]
    public bool logDebug = false;

    Transform _hand;
    GameObject _current;
    Coroutine _hideCo;

    public ItemSO CurrentItem { get; private set; }
    public ItemSO LastItem { get; private set; }
    public Transform Hand => _hand ? _hand : (_hand = ResolveHand());

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        _hand = ResolveHand();
    }

    Transform ResolveHand()
    {
        if (handSocketOverride) return handSocketOverride;
        var t = FindDeepChildByName(transform, "HandSocket");
        if (t) return t;
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
        CurrentItem = item;
        LastItem = item;

        if (item == null || item.heldPrefab == null) return;

        var hand = Hand;
        if (!hand) { Debug.LogWarning("[HeldItemDisplay] 未找到 HandSocket/右手骨骼"); return; }

        _current = Instantiate(item.heldPrefab, hand, false);
        _current.name = $"HELD_{item.id}";
        if (forceActiveOnSpawn && !_current.activeSelf) _current.SetActive(true);

        _current.transform.localPosition = item.heldLocalPosition;
        _current.transform.localEulerAngles = item.heldLocalEulerAngles;
        _current.transform.localScale = (item.heldLocalScale == Vector3.zero ? Vector3.one : item.heldLocalScale);

        if (seconds < 0f) seconds = defaultShowSeconds;
        if (seconds > 0f) _hideCo = StartCoroutine(HideAfter(seconds));
    }

    IEnumerator HideAfter(float s)
    {
        yield return new WaitForSeconds(s);
        _hideCo = null;
        Clear();
    }

    public void Clear()
    {
        if (_hideCo != null) { StopCoroutine(_hideCo); _hideCo = null; }
        if (_current) Destroy(_current);
        _current = null;
        CurrentItem = null;
    }

    public void PurgeHeldVisuals()
    {
        var hand = Hand;
        if (!hand) return;
        for (int i = hand.childCount - 1; i >= 0; i--)
        {
            var child = hand.GetChild(i);
            if (child && child.name.StartsWith("HELD_"))
                Destroy(child.gameObject);
        }
    }
}
