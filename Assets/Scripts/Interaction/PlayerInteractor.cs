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
    private NPCDialogWorldBridge _cachedWorldBridge;

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

        // 缓存 WorldBridge 引用
        _cachedWorldBridge = FindObjectOfType<NPCDialogWorldBridge>();
    }

    void Update()
    {
        if (_cachedDialogUI == null) return;

        // 对话打开时处理
        if (_cachedDialogUI.IsOpen)
        {
            HandleOpenDialog();
        }
        else
        {
            HandleNormalSearch();
        }
    }

    /// <summary>
    /// 处理对话打开时的逻辑（自动关闭 + 就近切换）
    /// </summary>
    private void HandleOpenDialog()
    {
        // 处理离开距离自动关闭
        if (autoCloseWhenFar)
        {
            var subject = _cachedDialogUI.CurrentNPC;
            if (subject != null)
            {
                float d = Vector3.Distance(transform.position, subject.SubjectTransform.position);
                if (d > closeDistance)
                {
                    _cachedDialogUI.Close();
                    if (_cachedWorldBridge != null) _cachedWorldBridge.EndStandalone();
                }
            }
        }

        // 处理就近切换
        HandleConversationSwitch();
    }

    /// <summary>
    /// 处理对话期间的NPC切换
    /// </summary>
    private void HandleConversationSwitch()
    {
        var currSubject = _cachedDialogUI.CurrentNPC;
        Transform currTrans = currSubject?.SubjectTransform;

        // 检查是否靠近另一个NPC
        bool tooFar = autoCloseWhenFar && currTrans != null &&
            Vector3.Distance(transform.position, currTrans.position) > closeDistance;
        if (tooFar) return;

        var nearest = FindClosestInteractable();
        bool isAnother = nearest != null && (currTrans == null || nearest.GetTransform() != currTrans);

        if (isAnother)
        {
            if (nearest is IDialogSubject sub)
                ShowHint($"按 E 对话：{sub.Name}");
            else
                ShowHint("按 E 交互");

            if (PressedInteractKey() && _switchCo == null && _pendingSwitch != nearest)
            {
                _switchCo = StartCoroutine(SwitchConversation(nearest));
            }
            return;
        }

        HideHint();
    }

    /// <summary>
    /// 处理正常搜索流程（对话未开启）
    /// </summary>
    private void HandleNormalSearch()
    {
        _current = FindClosestInteractable();

        if (_current != null)
        {
            string tip = _current.GetInteractPrompt();
            if (!string.IsNullOrEmpty(tip) && tip != _lastHintText)
            {
                Debug.Log($"[PlayerInteractor] ShowHint: {tip}");
                ShowHint(tip);
                _lastHintText = tip;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(_lastHintText))
            {
                HideHint();
                _lastHintText = "";
            }
        }

        if (_current != null && PressedInteractKey() && _switchCo == null && _pendingSwitch != _current)
        {
            Debug.Log("[PlayerInteractor] E pressed! Starting switch...");
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
        if (_cachedWorldBridge != null)
        {
            if (dialogSubject != null)
            {
                _cachedWorldBridge.BindToSubject(dialogSubject);
            }
            else if (targetGameObject != null)
            {
                // Phase 8: 如果是旧版 NPC，尝试绑定（保留兼容性）
                var oldNpc = targetGameObject.GetComponent<NPCInteractable>();
                if (oldNpc != null)
                    _cachedWorldBridge.BindToNPC(oldNpc);
            }
            else
            {
                // Phase 8: 简单的 Transform 绑定
                Transform anchor = target.GetTransform();
                _cachedWorldBridge.Bind(anchor);
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
