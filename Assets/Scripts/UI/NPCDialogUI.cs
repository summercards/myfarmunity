using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmGame.NPCSystem;
using FarmGame.UI; // IDialogSubject
using FarmGame.ActorSystem; // Phase 4: 引入 Actor 命名空间

#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 极简对话面板：显示 NPC 名称、分行台词、两个按钮（继续/关闭 + 功能）。
/// - 兼容功能按钮文字：TextMeshProUGUI 与 UGUI Text；若未手动绑定，会在 BtnFunc 下自动查找。
/// - 暴露 CurrentNPC，便于 PlayerInteractor 做“离开距离自动关闭”。
/// </summary>
public class NPCDialogUI : MonoBehaviour
{
    [Header("Bind In Inspector")]
    public GameObject root;                  // 面板根节点（启用/隐藏）
    public TextMeshProUGUI nameText;         // NPC 名称显示
    public TextMeshProUGUI lineText;         // 台词显示
    public Button nextButton;                // 继续/关闭
    public Button functionButton;            // 功能按钮

    [Tooltip("优先使用 TMP 版本的标签；若留空将自动在按钮子物体中查找。")]
    public TextMeshProUGUI functionButtonLabelTMP;   // 兼容 TMP
    [Tooltip("如果没有使用 TMP，可使用 UGUI Text；若留空将自动查找。")]
    public Text functionButtonLabelUGUI;             // 兼容 UGUI Text

    [Header("Options")]
    [Tooltip("打开对话时是否把玩家的鼠标光标显示出来。")]
    public bool showCursor = true;
    [Tooltip("按 Esc 关闭面板。")]
    public bool closeOnEsc = true;

    /// <summary> 面板是否打开 </summary>
    public bool IsOpen { get; private set; }

    /// <summary> 当前正在对话的 NPC（供外部读取） </summary>
    public IDialogSubject CurrentNPC => _curr;

    private IDialogSubject _curr;
    private int _index = 0;

    void Awake()
    {
        WireButtons();
        HideImmediate();
    }

    void Update()
    {
        if (!IsOpen) return;

#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
        if (closeOnEsc && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
#else
        if (closeOnEsc && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
#endif
    }

    private void WireButtons()
    {
        if (nextButton) nextButton.onClick.AddListener(() =>
        {
            if (!IsOpen) return;
            ShowNextOrClose();
        });

        if (functionButton) functionButton.onClick.AddListener(() =>
        {
            if (!IsOpen || _curr == null) return;
            _curr.InvokeFunction();
        });
    }

    /// <summary> 打开对话并显示第一句 </summary>
    public void Open(IDialogSubject npc)
    {
        Debug.Log($"[NPCDialogUI] Open called for {npc?.Name}");
        _curr = npc;
        _index = 0;

        if (nameText) nameText.text = npc != null ? npc.Name : "";
        SetFunctionLabel(npc != null && !string.IsNullOrEmpty(npc.FunctionButtonText) ? npc.FunctionButtonText : "功能");

        IsOpen = true;
        if (root) root.SetActive(true);
        if (showCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // Phase 4: 如果是 Actor，通知对话事件到 ActorDialogue
        if (npc is Actor actor)
        {
            var dialogue = actor.GetComponent<ActorDialogue>();
            if (dialogue != null)
            {
                dialogue.OnDialogueStarted();
            }
        }

        // 通知世界气泡 Bridge (Phase 2: 支持IDialogSubject)
        var bridge = FindObjectOfType<NPCDialogWorldBridge>();
        if (bridge != null)
        {
            // Phase 2: 使用新的 BindToSubject 方法，同时支持 Actor 和 NPC
            bridge.BindToSubject(npc);
        }

        RefreshLine();
    }

    /// <summary> 关闭对话面板 </summary>
    public void Close()
    {
        IsOpen = false;
        if (root) root.SetActive(false);

        _index = 0;
        _curr = null;

        var bridge = FindObjectOfType<NPCDialogWorldBridge>();
        if (bridge != null) bridge.EndStandalone();
    }

    private void HideImmediate()
    {
        IsOpen = false;
        if (root) root.SetActive(false);
    }

    private void RefreshLine()
    {
        if (_curr == null || _curr.DialogLines == null || _curr.DialogLines.Count == 0)
        {
            if (lineText) lineText.text = "(……)";
            return;
        }

        _index = Mathf.Clamp(_index, 0, _curr.DialogLines.Count - 1);
        if (lineText) lineText.text = _curr.DialogLines[_index];
    }

    private void ShowNextOrClose()
    {
        if (_curr == null || _curr.DialogLines == null || _curr.DialogLines.Count == 0)
        {
            Close();
            return;
        }

        _index++;
        if (_index >= _curr.DialogLines.Count)
        {
            Close();
        }
        else
        {
            RefreshLine();
        }
    }

    private void SetFunctionLabel(string s)
    {
        if (functionButtonLabelTMP) { functionButtonLabelTMP.text = s; return; }
        if (functionButtonLabelUGUI) { functionButtonLabelUGUI.text = s; return; }

        if (functionButton)
        {
            var tmp = functionButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp) { functionButtonLabelTMP = tmp; tmp.text = s; return; }

            var txt = functionButton.GetComponentInChildren<Text>(true);
            if (txt) { functionButtonLabelUGUI = txt; txt.text = s; return; }
        }
    }
}
