using UnityEngine;
using System.Collections;   // IEnumerator
using FarmGame.NPCSystem; // Phase 1: 保留兼容性
using FarmGame.UI;          // Added for IDialogSubject
using FarmGame.ActorSystem;    // Phase 8: 新系统支持

#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 玩家"交互"控制器：
/// - 在半径内搜索最近的 IInteractable，显示提示；按 E 执行交互；
/// - 对话打开时隐藏提示；
/// - 离开当前对话 NPC 超过 closeDistance 自动关闭对话框；
/// - 对话期间可就近切换到另一个 NPC（通过协程安全切换，避免 UI 竞态）。
/// Phase 8: 支持新 Actor 系统（优先）和旧 NPC 系统（兼容）
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
    private IInteractable _lastDetectedInteractable; // 用于减少日志刷屏和优化检测

    // —— 对话切换：缓冲与超时 —— //
    private IInteractable _pendingSwitch = null;
    private Coroutine _switchCo = null;
    [SerializeField] private float switchTimeout = 0.3f; // 等待 UI 真正关闭的最长时间（秒）

    // Phase 4 修复：记录上一次的提示文本，避免重复显示
    private string _lastHintText = "";

    // Phase 4 修复：缓存的 UI 引用
    private NPCDialogUI _cachedDialogUI;

    void Awake()
    {
        // Phase 4 修复：缓存 NPCDialogUI 引用
        if (sharedDialogUI != null)
        {
            _cachedDialogUI = sharedDialogUI;
            Debug.Log("[PlayerInteractor] 已使用指定的 NPCDialogUI 引用");
        }
        else
        {
            _cachedDialogUI = FindObjectOfType<NPCDialogUI>();
            if (_cachedDialogUI != null)
            {
                Debug.Log("[PlayerInteractor] 已找到并缓存 NPCDialogUI");
            }
            else
            {
                Debug.LogWarning("[PlayerInteractor] 场景中找不到 NPCDialogUI，交互功能将不可用");
            }
        }
    }

    void Update()
    {
        // Phase 4 修复：检查 UI 缓存
        if (_cachedDialogUI == null)
        {
            return;
        }

        // 对话打开时：优先处理"离开距离自动关闭"
        if (_cachedDialogUI.IsOpen && autoCloseWhenFar)
        {
            var subject = _cachedDialogUI.CurrentNPC;
            if (subject != null)
            {
                // Phase 8: 使用 IDialogSubject.SubjectTransform（新旧系统通用）
                float d = Vector3.Distance(transform.position, subject.SubjectTransform.position);
                if (d > closeDistance)
                {
                    _cachedDialogUI.Close();
                    // 关键：走远自动关闭时，也把世界气泡一并关掉
                    var bridge = FindObjectOfType<NPCDialogWorldBridge>();
                    if (bridge != null) bridge.EndStandalone();
                    // 不 return；允许继续逻辑，可能立刻切换到新的 NPC
                }
            }
        }

        // —— 对话期间允许"就近切换" —— //
        if (_cachedDialogUI.IsOpen)
        {
            var currSubject = _cachedDialogUI.CurrentNPC;
            // Phase 8: 比较 Transform 引用来判断是否是同一个人
            // （注意：currSubject 可能为 null，防御性编程）
            Transform currTrans = currSubject?.SubjectTransform;

            // 1) 若未超距，则检查是否靠近另一个 NPC
            if (!(autoCloseWhenFar && currTrans != null && Vector3.Distance(transform.position, currTrans.position) > closeDistance))
            {
                var nearest = FindClosestInteractable();

                // 判断是否是"另一个人"
                // 只要 nearest 存在，且它的 Transform 不等于当前正在对话者的 Transform
                bool isAnother = nearest != null && (currTrans == null || nearest.GetTransform() != currTrans);

                if (isAnother)
                {
                    // Phase 8: 优先使用 IDialogSubject.Name
                    if (nearest is IDialogSubject sub)
                        ShowHint($"按 E 对话：{sub.Name}");
                    else
                        ShowHint("按 E 交互");

                    // 使用协程进行"安全切换"（不要直接 Close()+Interact()）
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

        // 2) 显示提示（Phase 4 修复：只在提示变化时显示）
        if (_current != null)
        {
            string tip = _current.GetInteractPrompt();
            if (!string.IsNullOrEmpty(tip))
            {
                // Phase 4 修复：只在提示变化时才显示
                if (tip != _lastHintText)
                {
                    Debug.Log($"[PlayerInteractor] ShowHint: {tip}");
                    ShowHint(tip);
                    _lastHintText = tip;
                }
            }
        }
        else
        {
            // Phase 4 修复：清除提示文本，避免下次重复显示
            if (!string.IsNullOrEmpty(_lastHintText))
            {
                HideHint();
                _lastHintText = "";
            }
        }

        // 3) 按键触发（统一走"安全切换"协程）
        if (_current != null && PressedInteractKey() && _switchCo == null)
        {
            Debug.Log("[PlayerInteractor] E pressed! Starting switch...");
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

        // Debug: Log if anything is found
        // if (cols.Length > 0)
        // {
        //     Debug.Log($"[PlayerInteractor] OverlapSphere found {cols.Length} colliders");
        // }

        IInteractable best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < cols.Length; i++)
        {
            var inter = cols[i].GetComponentInParent<IInteractable>();
            if (inter == null)
            {
                // Debug.Log($"[PlayerInteractor] Ignored collider {cols[i].name} (No IInteractable)");
                continue;
            }
            
            // Debug.Log($"[PlayerInteractor] Found candidate: {inter.GetTransform().name}");

            float d = Vector3.Distance(transform.position, inter.GetTransform().position);
            if (d < bestDist)
            {
                best = inter;
                bestDist = d;
            }
        }
        
        // Debug.Log($"[PlayerInteractor] Closest is: {best?.GetTransform().name}");
        return best;
    }

    private IEnumerator SwitchConversation(IInteractable target)
    {
        Debug.Log($"[PlayerInteractor] SwitchConversation to {target.GetTransform().name}");
        // 标记目标，避免重复启动
        _pendingSwitch = target;

        // Phase 8: 检查目标类型并处理兼容性
        IDialogSubject dialogSubject = null;
        GameObject targetGameObject = null;

        if (target is IDialogSubject)
        {
            dialogSubject = (IDialogSubject)target;
            targetGameObject = dialogSubject.SubjectTransform?.gameObject;
        }
        else if (target is Component comp)
        {
            targetGameObject = comp.gameObject;
            // 尝试获取 IDialogSubject
            dialogSubject = comp.GetComponent<IDialogSubject>();
        }

        if (targetGameObject == null)
        {
            Debug.LogWarning("[PlayerInteractor] Target is null, cannot switch conversation");
            yield break;
        }

        // 1) 请求关闭当前对话（如果开着）
        if (_cachedDialogUI != null && _cachedDialogUI.IsOpen)
            _cachedDialogUI.Close();

        // 2) 等待 UI 真正关闭（IsOpen == false），最多等待 switchTimeout
        float t = 0f;
        while (_cachedDialogUI != null && _cachedDialogUI.IsOpen && t < switchTimeout)
        {
            t += Time.unscaledDeltaTime;   // 不受 Time.timeScale 影响
            yield return null;             // 等一帧
        }

        // 3) 保险：再让一帧过去，让 Close 回调/事件收尾
        yield return null;

        // 4) 切换到新的交互对象并立刻触发
        _current = target;

        // Phase 8: 优先使用新系统的 Actor
        if (targetGameObject != null)
        {
            Actor newActor = targetGameObject.GetComponent<Actor>();
            if (newActor != null)
            {
                // 新系统：直接传递 Actor 给 UI
                if (_cachedDialogUI != null)
                {
                    _cachedDialogUI.Open(newActor);
                    Debug.Log($"[PlayerInteractor] Opened new Actor system NPC: {newActor.Identity?.Name}");
                }
            }
            else
            {
                // 旧系统兼容：使用 IInteractable
                // Phase 8: 如果是旧版 NPC 且没单独指定 dialogUI，则复用 sharedDialogUI
                if (target is NPCInteractable oldNpc && oldNpc.dialogUI == null && sharedDialogUI != null)
                {
                    oldNpc.dialogUI = sharedDialogUI;
                }

                // Phase 8: 通知 UI
                if (target is IDialogSubject sub)
                {
                    _cachedDialogUI.Open(sub);
                    Debug.Log($"[PlayerInteractor] Opened legacy NPC system: {sub.Name}");
                }
                else
                {
                    // Phase 8: 回退到旧方式
                    target.Interact(gameObject);
                    Debug.Log("[PlayerInteractor] Used legacy Interact() method");
                }
            }
        }

        // Phase 8: 绑定世界气泡到当前 NPC
        var bridge = FindObjectOfType<NPCDialogWorldBridge>();
        if (bridge != null)
        {
            if (dialogSubject != null)
            {
                bridge.BindToSubject(dialogSubject);
            }
            else if (targetGameObject != null)
            {
                // Phase 8: 如果是旧版 NPC，尝试绑定（保留兼容性）
                var oldNpc = targetGameObject.GetComponent<NPCInteractable>();
                if (oldNpc != null)
                    bridge.BindToNPC(oldNpc);
            }
            else
            {
                // Phase 8: 简单的 Transform 绑定
                Transform anchor = target.GetTransform();
                bridge.Bind(anchor);
            }
        }

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
