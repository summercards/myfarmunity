// Assets/Scripts/NPC/NPCInteractable.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 最简 NPC：靠近显示“按E对话…”，按 E 打开对话框；包含一个“功能”按钮（留作后续商店/任务）。
/// </summary>
[RequireComponent(typeof(Collider))]
public class NPCInteractable : MonoBehaviour, IInteractable
{
    [Header("Basic Info")]
    public string npcId = "npc_001";
    public string npcName = "村民";
    [TextArea(2, 5)] public string[] dialogLines;

    [Header("UI")]
    [Tooltip("优先使用这里绑定的 UI；为空时由 PlayerInteractor.sharedDialogUI 兜底。")]
    public NPCDialogUI dialogUI;

    [Header("Function Button")]
    [Tooltip("功能按钮的可读标题文本，例如“打开商店”。")]
    public string functionButtonText = "功能";
    [Tooltip("点击功能按钮时触发的事件。可在 Inspector 里直接挂载商店打开方法等。")]
    public UnityEvent onFunction;

    Collider _col;

    void Reset()
    {
        _col = GetComponent<Collider>();
        if (_col) { _col.isTrigger = true; }
    }

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col) _col.isTrigger = true;
    }

    // =========== IInteractable ===========
    public string GetInteractPrompt()
        => $"按 [E] 对话：{npcName}";

    public Transform GetTransform() => transform;

    public void Interact(GameObject interactor)
    {
        // 打开对话 UI
        NPCDialogUI ui = dialogUI;
        if (!ui)
        {
            // 尝试在玩家身上找共用 UI
            var shared = interactor.GetComponent<PlayerInteractor>();
            if (shared) ui = shared.sharedDialogUI;
        }

        if (!ui)
        {
            Debug.LogWarning($"[{name}] 没有可用的 NPCDialogUI，请在 NPC 或 PlayerInteractor 上拖入一个。");
            return;
        }

        ui.Open(this);
    }

    // 供 UI 调用
    public void InvokeFunction()
    {
        onFunction?.Invoke();
    }
}
