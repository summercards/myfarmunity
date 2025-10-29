// Assets/Scripts/Interaction/PlayerInteractor.cs
using UnityEngine;
using System.Collections;   // IEnumerator
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 玩家“交互”控制器：
/// - 在半径内搜索最近的 IInteractable，显示提示；按 E 执行交互；
/// - 对话打开时隐藏提示；
/// - 离开当前对话 NPC 超过 closeDistance 自动关闭对话框；
/// - 对话期间可就近切换到另一个 NPC（通过协程安全切换，避免 UI 竞态）。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Detect")]
    [Tooltip("交互搜索半径（米）。")]
    public float searchRadius = 2.2f;

    [Tooltip("只检测这些层（建议为 NPC/可交互物体单独建一层，如 Interactable）。")]
    public LayerMask interactMask = ~0;

    [Header("UI (Optional)")]
    [Tooltip("用于显示提示字符串（可直接拖你项目里的 PickupHUD）。")]
    public PickupHUD hintHUD;

    [Tooltip("（可选）对话面板引用。若 NPC 未绑定，会回退使用这里的。")]
    public NPCDialogUI sharedDialogUI;

    [Header("Auto Close")]
    [Tooltip("对话打开时，若玩家与该 NPC 距离大于该值则自动关闭。")]
    public bool autoCloseWhenFar = true;
    public float closeDistance = 3.0f;

    // 当前锁定的可交互体
    private IInteractable _current;

    // —— 对话切换：缓冲与超时 —— //
    private IInteractable _pendingSwitch = null;
    private Coroutine _switchCo = null;
    [SerializeField] private float switchTimeout = 0.3f; // 等待 UI 真正关闭的最长时间（秒）

    void Update()
    {
        // 对话打开时：优先处理“离开距离自动关闭”
        if (sharedDialogUI && sharedDialogUI.IsOpen && autoCloseWhenFar)
        {
            var npc = sharedDialogUI.CurrentNPC;
            if (npc != null)
            {
                float d = Vector3.Distance(transform.position, npc.transform.position);
                if (d > closeDistance)
                {
                    sharedDialogUI.Close();
                    // 关键：走远自动关闭时，也把世界气泡一并关掉
                    var bridge = FindObjectOfType<NPCDialogWorldBridge>();
                    if (bridge != null) bridge.EndStandalone();
                    // 不 return；允许继续逻辑，可能立刻切换到新的 NPC
                }
            }
        }

        // —— 对话期间允许“就近切换” —— //
        if (sharedDialogUI && sharedDialogUI.IsOpen)
        {
            var curr = sharedDialogUI.CurrentNPC;

            // 1) 若未超距，则检查是否靠近另一个 NPC
            if (!(autoCloseWhenFar && curr != null && Vector3.Distance(transform.position, curr.transform.position) > closeDistance))
            {
                var nearest = FindClosestInteractable();
                bool isAnotherNpc = nearest != null && !ReferenceEquals(nearest, curr);

                if (isAnotherNpc)
                {
                    if (nearest is NPCInteractable nn)
                        ShowHint($"按 E 对话：{nn.npcName}");
                    else
                        ShowHint("按 E 交互");

                    // 使用协程进行“安全切换”（不要直接 Close()+Interact）
                    if (PressedInteractKey() && _switchCo == null)
                    {
                        if (_pendingSwitch != nearest)
                            _switchCo = StartCoroutine(SwitchConversation(nearest));
                    }
                    return; // 本帧结束
                }

                // 最近的仍是当前 NPC：隐藏提示并早退
                HideHint();
                return;
            }

            // 2) 已在上面关掉对话（超距），继续往下走，允许重新搜索并与新 NPC 交互
        }

        // —— 正常（对话未开启）流程 —— //

        // 1) 查找最近的 IInteractable
        _current = FindClosestInteractable();

        // 2) 显示提示
        if (_current != null)
        {
            string tip = _current.GetInteractPrompt();
            if (!string.IsNullOrEmpty(tip)) ShowHint(tip);
        }
        else
        {
            HideHint();
        }

        // 3) 按键触发（统一走“安全切换”协程）
        if (_current != null && PressedInteractKey() && _switchCo == null)
        {
            if (_pendingSwitch != _current)
                _switchCo = StartCoroutine(SwitchConversation(_current));
        }
    }

    private IInteractable FindClosestInteractable()
    {
        Collider[] cols = Physics.OverlapSphere(
            transform.position,
            searchRadius,
            interactMask,
            QueryTriggerInteraction.Collide
        );

        IInteractable best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < cols.Length; i++)
        {
            var inter = cols[i].GetComponentInParent<IInteractable>();
            if (inter == null) continue;

            float d = Vector3.Distance(transform.position, inter.GetTransform().position);
            if (d < bestDist)
            {
                best = inter;
                bestDist = d;
            }
        }
        return best;
    }

    private IEnumerator SwitchConversation(IInteractable target)
    {
        // 标记目标，避免重复启动
        _pendingSwitch = target;

        // 1) 请求关闭当前对话（如果开着）
        if (sharedDialogUI != null && sharedDialogUI.IsOpen)
            sharedDialogUI.Close();

        // 2) 等待 UI 真正关闭（IsOpen == false），最多等待 switchTimeout
        float t = 0f;
        while (sharedDialogUI != null && sharedDialogUI.IsOpen && t < switchTimeout)
        {
            t += Time.unscaledDeltaTime;   // 不受 Time.timeScale 影响
            yield return null;             // 等一帧
        }

        // 3) 保险：再让一帧过去，让 Close 回调/事件收尾
        yield return null;

        // 4) 切到新的交互对象并立刻触发
        // 4) 切到新的交互对象并立刻触发
        _current = target;

        // 如果是 NPC 且没单独指定 dialogUI，则复用 sharedDialogUI
        var asNpc = _current as NPCInteractable;
        if (asNpc != null && asNpc.dialogUI == null && sharedDialogUI != null)
            asNpc.dialogUI = sharedDialogUI;

        // —— 关键：在打开对话前，先把 Bridge 绑到“当前 NPC 的锚点” —— //
        var bridge = FindObjectOfType<NPCDialogWorldBridge>();
        if (bridge != null)
        {
            Transform anchor = null;

            // 你的 NPC 脚本里通常会有 BubbleAnchor/WorldAnchor 之类的 Transform
            // 请优先使用它；没有就退回 NPC 的 transform
            // 按你项目常用的命名尝试（任选一个真实存在的字段/属性）：
            if (asNpc != null)
            {
                // 依次尝试几种常见命名（你有哪个就用哪个）
                anchor = asNpc.transform;
                var tField = asNpc.GetType().GetField("bubbleAnchor") ?? asNpc.GetType().GetField("BubbleAnchor");
                if (tField != null) anchor = tField.GetValue(asNpc) as Transform;

                var tProp = asNpc.GetType().GetProperty("BubbleAnchor") ?? asNpc.GetType().GetProperty("WorldAnchor");
                if (tProp != null) anchor = (tProp.GetValue(asNpc) as Transform) ?? anchor;
            }
            else
            {
                anchor = (target as MonoBehaviour)?.transform;
            }

            // 调用你 Bridge 暴露的方法（如果你已有 SetTarget/SetAnchor/BindTo，请把 Bind 换成你的方法名）
            bridge.Bind(anchor);
        }
        // 最后再真正打开对话
        _current.Interact(gameObject);

        // ★ 兜底：再次绑定到当前 NPC，确保世界气泡跟随正确对象
        var bridge2 = FindObjectOfType<NPCDialogWorldBridge>();
        if (bridge2 != null && asNpc != null) bridge2.BindToNPC(asNpc);

        // 5) 清理状态
        _pendingSwitch = null;
        _switchCo = null;

    }

    private bool PressedInteractKey()
    {
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
#endif
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton0);
    }

    private void ShowHint(string text)
    {
        if (hintHUD) hintHUD.Show(text);
    }

    private void HideHint()
    {
        if (hintHUD) hintHUD.Hide();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
        Gizmos.DrawSphere(transform.position, searchRadius);
    }
}
