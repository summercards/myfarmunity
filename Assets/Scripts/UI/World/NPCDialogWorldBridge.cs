// Assets/Scripts/UI/World/NPCDialogWorldBridge.cs
using UnityEngine;
using TMPro;
using System.Reflection;
using UnityEngine.UI;

/// <summary>
/// 将 NPCDialogUI 的“台词文本”重定向到 3D 气泡中显示：
/// - 挂到同一物体（含 NPCDialogUI）上；
/// - 不修改原 UI 逻辑，按钮/功能保持不变；
/// - 当面板打开时：隐藏原 lineText 组件，只把文本内容渲染到世界空间气泡；
/// - 当面板关闭或当前 NPC 为空时：自动隐藏气泡。
/// </summary>
[DisallowMultipleComponent]
public class NPCDialogWorldBridge : MonoBehaviour
{
    [Header("References")]
    public NPCDialogUI ui;                       // 原对话 UI（需在场景里绑定或自动查找）
    [Tooltip("可选：指定 NPC 头顶锚点的名称；若找不到则使用 Collider 的 bounds 顶部。")]
    public string anchorChildName = "BubbleAnchor";

    [Header("Bubble")]
    public SpeechBubble3D bubblePrefab;          // 可不填：不填则运行时自动构建一个简易气泡
    [Tooltip("世界坐标偏移（相对锚点）。")]
    public Vector3 bubbleOffset = new Vector3(0f, 1.8f, 0f);
    [Range(160f, 800f)]
    public float bubbleMaxWidth = 420f;

    [Header("Behaviour")]
    [Tooltip("是否接管文本显示为 3D 气泡。勾选后原 UI 的 lineText 将被隐藏。")]
    public bool enableWorldBubble = true;

    // 运行时
    private SpeechBubble3D _bubble;
    private Transform _anchor;
    private object _currentNPC;                  // 通过反射读取 ui.CurrentNPC
    private string _lastLineText = "";
    private TextMeshProUGUI _lineTextTMP;
    private Text _lineTextUGUI;
    private PropertyInfo _propCurrentNPC;
    private FieldInfo _fieldCurrentNPC;

    void Awake()
    {
        if (!ui) ui = GetComponent<NPCDialogUI>();
        CacheLineText();
        // 反射拿 CurrentNPC（兼容“属性/字段”两种写法）
        _propCurrentNPC = typeof(NPCDialogUI).GetProperty("CurrentNPC", BindingFlags.Public | BindingFlags.Instance);
        _fieldCurrentNPC = typeof(NPCDialogUI).GetField("CurrentNPC", BindingFlags.Public | BindingFlags.Instance);
    }

    void CacheLineText()
    {
        // 兼容 TMP 和 UGUI 两种文本
        var t = typeof(NPCDialogUI);
        var f1 = t.GetField("lineText", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f1 != null)
        {
            _lineTextTMP = f1.GetValue(ui) as TextMeshProUGUI;
            if (!_lineTextTMP)
            {
                _lineTextUGUI = f1.GetValue(ui) as Text;
            }
        }
    }

    void Update()
    {
        if (!ui) return;
        if (!enableWorldBubble) return;

        // 面板根是否打开
        bool isOpen = false;
        {
            var fRoot = typeof(NPCDialogUI).GetField("root", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fRoot != null)
            {
                var go = fRoot.GetValue(ui) as GameObject;
                isOpen = go && go.activeInHierarchy;
            }
        }

        _currentNPC = GetCurrentNPCObj();
        if (!isOpen || _currentNPC == null)
        {
            HideBubble();
            return;
        }

        // 读取当前台词文字
        string line = ReadCurrentLineText();
        if (line == null) line = "";

        // 首次或 NPC 变化时重新定位锚点/气泡
        if (_bubble == null || _anchor == null)
        {
            ResolveAnchorAndBubble();
            if (_bubble != null)
                _bubble.SetText(line);
        }

        // 同步文本（发生变化才刷新）
        if (_bubble != null && _lastLineText != line)
        {
            _bubble.SetText(line);
            _lastLineText = line;
        }
    }

    string ReadCurrentLineText()
    {
        if (_lineTextTMP) return _lineTextTMP.text;
        if (_lineTextUGUI) return _lineTextUGUI.text;
        return "";
    }

    object GetCurrentNPCObj()
    {
        if (_propCurrentNPC != null)
            return _propCurrentNPC.GetValue(ui);
        if (_fieldCurrentNPC != null)
            return _fieldCurrentNPC.GetValue(ui);
        return null;
    }

    void ResolveAnchorAndBubble()
    {
        // 通过反射拿到 NPC 的 Transform / Collider
        Transform npcTransform = null;
        Collider npcCollider = null;

        if (_currentNPC != null)
        {
            var tNpc = _currentNPC.GetType();
            var pTr = tNpc.GetProperty("transform", BindingFlags.Public | BindingFlags.Instance);
            var pCol = tNpc.GetProperty("Collider", BindingFlags.Public | BindingFlags.Instance);
            if (pTr != null) npcTransform = pTr.GetValue(_currentNPC) as Transform;
            if (pCol != null) npcCollider = pCol.GetValue(_currentNPC) as Collider;
            if (!npcCollider)
            {
                // 尝试在 transform 上找一个
                if (npcTransform) npcCollider = npcTransform.GetComponentInChildren<Collider>();
            }
        }

        // 1) 优先找指定名称的子物体作为锚点
        _anchor = null;
        if (npcTransform && !string.IsNullOrEmpty(anchorChildName))
        {
            var child = npcTransform.Find(anchorChildName);
            if (child) _anchor = child;
        }

        // 2) 找不到则用碰撞体 bounds 顶部
        if (!_anchor && npcCollider)
        {
            var go = new GameObject("BubbleAnchor(auto)");
            go.transform.SetParent(npcCollider.transform, worldPositionStays: false);
            var b = npcCollider.bounds;
            go.transform.position = new Vector3(b.center.x, b.max.y, b.center.z);
            _anchor = go.transform;
        }

        // 3) 兜底：直接用 NPC transform
        if (!_anchor) _anchor = npcTransform;

        // 创建/初始化气泡
        if (_anchor)
        {
            if (_bubble == null)
            {
                if (bubblePrefab)
                {
                    _bubble = Instantiate(bubblePrefab);
                }
                else
                {
                    _bubble = new GameObject("SpeechBubble3D").AddComponent<SpeechBubble3D>();
                }
            }
            _bubble.transform.SetParent(null, worldPositionStays: true);
            _bubble.Init(_anchor, Camera.main, bubbleMaxWidth, bubbleOffset);

            // 隐藏原 UI 的行文本（设为透明以保持布局），只保留按钮/功能
            if (_lineTextTMP)
            {
                var c = _lineTextTMP.color; c.a = 0f; _lineTextTMP.color = c;
            }
            if (_lineTextUGUI)
            {
                var c2 = _lineTextUGUI.color; c2.a = 0f; _lineTextUGUI.color = c2;
            }
        }
    }

    void HideBubble()
    {
        if (_bubble) _bubble.Hide();
        // 面板关闭时恢复 UI 的行文本可见性（避免编辑器中看不到）
        if (_lineTextTMP) { var c = _lineTextTMP.color; c.a = 1f; _lineTextTMP.color = c; }
        if (_lineTextUGUI) { var c2 = _lineTextUGUI.color; c2.a = 1f; _lineTextUGUI.color = c2; }
        _lastLineText = "";
        _anchor = null;
    }
}