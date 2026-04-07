using UnityEngine;
using System.Collections;   // IEnumerator
using FarmGame.Core.Contracts;

#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 玩家"交互"控制器：
/// - 在半径内搜索最近的 IInteractable，显示提示；按 E 执行交互；
/// - 对话打开时隐藏提示；
/// - 离开当前对话 NPC 超过 closeDistance 自动关闭对话框；
/// - 对话期间可就近切换到另一个 NPC（通过协程安全切换，避免 UI 竞态）。
/// NPC 主线使用 ActorSystem，旧交互体通过 IDialogSubject 兼容。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerInteractor : MonoBehaviour
{
    private const string LegacyNpcInteractableTypeName = "FarmGame.NPCSystem.NPCInteractable";

    [Header("Detect")]
    [Tooltip("交互搜索半径（米）。")]
    public float searchRadius = 2.2f;

    [Tooltip("只检测这些层（建议为 NPC/可交互物体单独建一层，如 Interactable）。")]
    public LayerMask interactMask = ~0;

    [Header("UI (Optional)")]
    [Tooltip("用于显示提示字符串（可直接拖你项目里的 PickupHUD）。")]
    public MonoBehaviour hintHUD;

    [Tooltip("（可选）对话面板引用。若 NPC 未绑定，会回退使用这里的。")]
    public MonoBehaviour sharedDialogUI;

    [Header("Auto Close")]
    [Tooltip("对话打开时，若玩家与该 NPC 距离大于该值则自动关闭。")]
    public bool autoCloseWhenFar = true;
    public float closeDistance = 3.0f;

    [Header("Legacy Compatibility")]
    [Tooltip("是否允许回退到旧 NPCInteractable。阶段4建议关闭，仅用于迁移排障。")]
    public bool allowLegacyNpcFallback = false;

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
    private IHintHUD _hintHUDContract;
    private IDialogUI _sharedDialogUIContract;
    private IDialogUI _cachedDialogUI;
    private IDialogWorldBridge _cachedWorldBridge;

    void Awake()
    {
        ResolveHintHUDContract();
        ResolveSharedDialogUIContract();
        _cachedDialogUI = _sharedDialogUIContract ?? RuntimeRefs.DialogUIContract;
        _cachedWorldBridge = RuntimeRefs.DialogWorldBridgeContract;
    }

    void OnEnable()
    {
        RuntimeRefs.DialogUIContractChanged += HandleDialogUIChanged;
        RuntimeRefs.DialogWorldBridgeContractChanged += HandleWorldBridgeChanged;
    }

    void OnDisable()
    {
        RuntimeRefs.DialogUIContractChanged -= HandleDialogUIChanged;
        RuntimeRefs.DialogWorldBridgeContractChanged -= HandleWorldBridgeChanged;
    }

    void HandleDialogUIChanged(IDialogUI dialogUI)
    {
        if (_sharedDialogUIContract == null)
        {
            _cachedDialogUI = dialogUI;
        }
    }

    void HandleWorldBridgeChanged(IDialogWorldBridge worldBridge)
    {
        _cachedWorldBridge = worldBridge;
    }

    void Update()
    {
        if (_hintHUDContract == null)
        {
            ResolveHintHUDContract();
        }

        if (_sharedDialogUIContract == null && sharedDialogUI != null)
        {
            ResolveSharedDialogUIContract();
        }

        if (_cachedDialogUI == null && _sharedDialogUIContract != null)
        {
            _cachedDialogUI = _sharedDialogUIContract;
        }

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
            var subject = _cachedDialogUI.CurrentSubject;
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
        var currSubject = _cachedDialogUI.CurrentSubject;
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
            MonoBehaviour[] candidates = cols[i].GetComponentsInParent<MonoBehaviour>(true);
            IInteractable inter = null;
            IInteractable legacyFallback = null;

            for (int j = 0; j < candidates.Length; j++)
            {
                MonoBehaviour candidate = candidates[j];
                if (!(candidate is IInteractable interactable))
                {
                    continue;
                }

                if (IsLegacyNpcInteractable(interactable))
                {
                    if (!allowLegacyNpcFallback)
                    {
                        continue;
                    }

                    if (legacyFallback == null)
                    {
                        legacyFallback = interactable;
                    }
                    continue;
                }

                inter = interactable;
                break;
            }

            if (inter == null)
            {
                inter = legacyFallback;
            }

            if (inter == null)
            {
                // Debug.Log($"[PlayerInteractor] Ignored collider {cols[i].name} (No IInteractable)");
                continue;
            }
            
            // Debug.Log($"[PlayerInteractor] Found candidate: {inter.GetTransform().name}");

            Transform interactTransform = inter.GetTransform();
            if (interactTransform == null)
            {
                continue;
            }

            float d = Vector3.Distance(transform.position, interactTransform.position);
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

        try
        {
            TryResolveDialogSubject(target, out IDialogSubject dialogSubject, out GameObject targetGameObject);

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

            if (_cachedDialogUI != null && dialogSubject != null)
            {
                _cachedDialogUI.Open(dialogSubject);
                Debug.Log($"[PlayerInteractor] Opened NPC dialogue: {dialogSubject.Name}");
            }
            else
            {
                target.Interact(gameObject);
                Debug.Log("[PlayerInteractor] Target does not expose dialog subject, fallback to Interact()");
            }

            if (_cachedWorldBridge != null)
            {
                if (dialogSubject != null)
                {
                    _cachedWorldBridge.BindToSubject(dialogSubject);
                }
                else
                {
                    _cachedWorldBridge.Bind(target.GetTransform());
                }
            }
        }
        finally
        {
            // 5) 清理状态（确保任何提前退出也会清理）
            _pendingSwitch = null;
            _switchCo = null;
        }
    }

    private void TryResolveDialogSubject(IInteractable target, out IDialogSubject dialogSubject, out GameObject targetGameObject)
    {
        dialogSubject = null;
        targetGameObject = null;
        IDialogSubject legacySubjectFallback = null;

        if (target == null)
        {
            return;
        }

        if (target is Component targetComponent)
        {
            targetGameObject = targetComponent.gameObject;
        }

        if (target is IDialogSubject directSubject)
        {
            if (IsLegacyNpcDialogSubject(directSubject))
            {
                if (allowLegacyNpcFallback)
                {
                    legacySubjectFallback = directSubject;
                }
            }
            else
            {
                dialogSubject = directSubject;
            }
        }

        if (targetGameObject == null && dialogSubject != null && dialogSubject.SubjectTransform != null)
        {
            targetGameObject = dialogSubject.SubjectTransform.gameObject;
        }

        if (targetGameObject == null)
        {
            return;
        }

        if (dialogSubject != null)
        {
            return;
        }

        MonoBehaviour[] candidates = targetGameObject.GetComponentsInParent<MonoBehaviour>(true);
        foreach (MonoBehaviour candidate in candidates)
        {
            if (candidate is IDialogSubject subject)
            {
                if (IsLegacyNpcDialogSubject(subject))
                {
                    if (allowLegacyNpcFallback && legacySubjectFallback == null)
                    {
                        legacySubjectFallback = subject;
                    }
                }
                else
                {
                    dialogSubject = subject;
                    targetGameObject = subject.SubjectTransform != null ? subject.SubjectTransform.gameObject : candidate.gameObject;
                    return;
                }
            }
        }

        if (legacySubjectFallback != null)
        {
            dialogSubject = legacySubjectFallback;
            targetGameObject = legacySubjectFallback.SubjectTransform != null
                ? legacySubjectFallback.SubjectTransform.gameObject
                : targetGameObject;
        }
    }

    private static bool IsLegacyNpcInteractable(IInteractable interactable)
    {
        if (!(interactable is MonoBehaviour behaviour))
        {
            return false;
        }

        return behaviour.GetType().FullName == LegacyNpcInteractableTypeName;
    }

    private static bool IsLegacyNpcDialogSubject(IDialogSubject subject)
    {
        if (!(subject is MonoBehaviour behaviour))
        {
            return false;
        }

        return behaviour.GetType().FullName == LegacyNpcInteractableTypeName;
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
        _hintHUDContract?.Show(text);
    }

    private void HideHint()
    {
        _hintHUDContract?.Hide();
    }

    private void ResolveHintHUDContract()
    {
        _hintHUDContract = hintHUD as IHintHUD;
    }

    private void ResolveSharedDialogUIContract()
    {
        _sharedDialogUIContract = sharedDialogUI as IDialogUI;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
        Gizmos.DrawSphere(transform.position, searchRadius);
    }
}
