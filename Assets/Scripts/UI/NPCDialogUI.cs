// Assets/Scripts/UI/NPCDialogUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    public NPCInteractable CurrentNPC => _curr;

    private NPCInteractable _curr;
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
    public void Open(NPCInteractable npc)
    {
        _curr = npc;
        _index = 0;

        if (nameText) nameText.text = npc ? npc.npcName : "";
        SetFunctionLabel(npc && !string.IsNullOrEmpty(npc.functionButtonText) ? npc.functionButtonText : "功能");

        IsOpen = true;
        if (root) root.SetActive(true);
        if (showCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        RefreshLine();
    }

    /// <summary> 关闭对话面板 </summary>
    public void Close()
    {
        IsOpen = false;
        if (root) root.SetActive(false);
    }

    private void HideImmediate()
    {
        IsOpen = false;
        if (root) root.SetActive(false);
    }

    private void RefreshLine()
    {
        if (!_curr || _curr.dialogLines == null || _curr.dialogLines.Length == 0)
        {
            if (lineText) lineText.text = "(……)";
            return;
        }

        _index = Mathf.Clamp(_index, 0, _curr.dialogLines.Length - 1);
        if (lineText) lineText.text = _curr.dialogLines[_index];
    }

    private void ShowNextOrClose()
    {
        if (_curr == null || _curr.dialogLines == null || _curr.dialogLines.Length == 0)
        {
            Close();
            return;
        }

        _index++;
        if (_index >= _curr.dialogLines.Length)
        {
            Close();
        }
        else
        {
            RefreshLine();
        }
    }

    /// <summary>
    /// 设置功能按钮文字（支持 TMP 与旧 Text；未绑定时会在 functionButton 子物体中自动寻找）
    /// </summary>
    private void SetFunctionLabel(string s)
    {
        // 优先使用显式绑定的
        if (functionButtonLabelTMP) { functionButtonLabelTMP.text = s; return; }
        if (functionButtonLabelUGUI) { functionButtonLabelUGUI.text = s; return; }

        // 未绑定则自动查找
        if (functionButton)
        {
            var tmp = functionButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp) { functionButtonLabelTMP = tmp; tmp.text = s; return; }

            var txt = functionButton.GetComponentInChildren<Text>(true);
            if (txt) { functionButtonLabelUGUI = txt; txt.text = s; return; }
        }
    }
}
