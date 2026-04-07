using UnityEngine;
using TMPro;
using System.Reflection;
using UnityEngine.UI;
using FarmGame.Core.Contracts;

[DisallowMultipleComponent]
public class NPCDialogWorldBridge : MonoBehaviour, IDialogWorldBridge
{
    [Header("References")]
    public NPCDialogUI ui;
    public string anchorChildName = "BubbleAnchor";

    [Header("Bubble")]
    public SpeechBubble3D bubblePrefab;
    public Vector3 bubbleOffset = new Vector3(0f, 1.8f, 0f);
    [Range(160f, 800f)] public float bubbleMaxWidth = 320f;

    [Header("Behaviour")]
    public bool enableWorldBubble = true;

    [Header("Auto Hide By Distance")]
    public bool enableAutoHideByDistance = true;
    public float hideDistance = 4.0f;
    public string playerTag = "Player";
    public bool alsoCloseUIDialog = true;

    [Header("UI Line Override (调试用)")]
    public TextMeshProUGUI lineTextOverrideTMP;
    public Text lineTextOverrideUGUI;

    SpeechBubble3D _bubble;
    Transform _anchor;
    IDialogSubject _currentSubject; // Phase 2: 从 NPC 改为 IDialogSubject
    string _lastLineText = "";
    TextMeshProUGUI _lineTextTMP;
    Text _lineTextUGUI;
    Transform _player;
    Camera _cachedCamera;

    bool _standaloneMode = false;
    Transform _standaloneNPC;
    string _standaloneLine = "";

    void Awake()
    {
        if (!ui) ui = GetComponent<NPCDialogUI>();
        if (!ui) ui = RuntimeRefs.DialogUI;
        RuntimeRefs.RegisterDialogWorldBridge(this);
        CacheLineText();
        _cachedCamera = ResolveBubbleCamera();
        _player = RuntimeRefs.PlayerTransform;
    }

    void OnEnable()
    {
        // 商店打开时关闭独立气泡 -> 返回主对话气泡
        MiniShop.OnActiveChanged += OnShopActiveChanged;
        RuntimeRefs.PlayerTransformChanged += HandlePlayerTransformChanged;
        RuntimeRefs.DialogUIChanged += HandleDialogUIChanged;
    }

    void OnDisable()
    {
        MiniShop.OnActiveChanged -= OnShopActiveChanged;
        RuntimeRefs.PlayerTransformChanged -= HandlePlayerTransformChanged;
        RuntimeRefs.DialogUIChanged -= HandleDialogUIChanged;
    }

    void OnDestroy()
    {
        RuntimeRefs.UnregisterDialogWorldBridge(this);
    }

    void OnShopActiveChanged(bool active)
    {
        if (!active) EndStandalone();
    }

    void HandlePlayerTransformChanged(Transform playerTransform)
    {
        _player = playerTransform;
    }

    void HandleDialogUIChanged(NPCDialogUI dialogUI)
    {
        if (ui == null)
        {
            ui = dialogUI;
            CacheLineText();
        }
    }

    void CacheLineText()
    {
        if (lineTextOverrideTMP) { _lineTextTMP = lineTextOverrideTMP; _lineTextUGUI = null; return; }
        if (lineTextOverrideUGUI) { _lineTextUGUI = lineTextOverrideUGUI; _lineTextTMP = null; return; }
        if (ui == null) return;

        var t = typeof(NPCDialogUI);
        var f1 = t.GetField("lineText", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f1 != null)
        {
            _lineTextTMP = f1.GetValue(ui) as TextMeshProUGUI;
            if (!_lineTextTMP) _lineTextUGUI = f1.GetValue(ui) as Text;
            if (_lineTextTMP || _lineTextUGUI) return;
        }

        GameObject rootGO = null;
        var fRoot = t.GetField("root", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (fRoot != null)
        {
            rootGO = fRoot.GetValue(ui) as GameObject;
        }

        if (rootGO)
        {
            int best = -1;
            foreach (var tmp in rootGO.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                int len = tmp.text != null ? tmp.text.Length : 0;
                if (len > best) { best = len; _lineTextTMP = tmp; _lineTextUGUI = null; }
            }
            foreach (var ug in rootGO.GetComponentsInChildren<Text>(true))
            {
                int len = ug.text != null ? ug.text.Length : 0;
                if (len > best) { best = len; _lineTextUGUI = ug; _lineTextTMP = null; }
            }
        }
    }

    void Update()
    {
        if (!enableWorldBubble) return;
        if (!ui) ui = RuntimeRefs.DialogUI;
        if (_player == null) _player = RuntimeRefs.PlayerTransform;
        _cachedCamera = ResolveBubbleCamera();

        bool isOpen = ui != null && ui.IsOpen;
        GameObject rootGO = ui != null ? ui.root : null;

        // 距离检测：如果过远则隐藏独立气泡/关闭UI对话框
        if (enableAutoHideByDistance && _player && (_anchor || _standaloneNPC))
        {
            var refTr = _anchor ? _anchor : _standaloneNPC;
            if (refTr)
            {
                Vector3 a = _player.position; a.y = 0;
                Vector3 b = refTr.position; b.y = 0;
                if (Vector3.Distance(a, b) > hideDistance)
                {
                    HideBubble();
                    if (alsoCloseUIDialog && rootGO) rootGO.SetActive(false);
                    _standaloneMode = false;
                    return;
                }
            }
        }

        // 独立模式：显示固定气泡（非UI对话框模式）
        if (_standaloneMode)
        {
            if (_bubble == null || _anchor == null)
            {
                _anchor = ResolveAnchor(_standaloneNPC);
                EnsureBubble();
                _bubble.transform.SetParent(null, true);
                _cachedCamera = ResolveBubbleCamera();
                _bubble.Init(_anchor, _cachedCamera, bubbleMaxWidth, bubbleOffset);
                _bubble.SetText(_standaloneLine);
                MakeUILineTransparent();
            }
            return;
        }

        // UI模式：跟随UI对话框当前对话的NPC（或Actor）
        _currentSubject = GetCurrentSubject();
        if (!isOpen || _currentSubject == null)
        {
            // UI关闭时清除气泡
            HideBubble();
            return;
        }

        string line = ReadCurrentLineText() ?? "";
        if (_bubble == null || _anchor == null)
        {
            _anchor = ResolveAnchor(_currentSubject.SubjectTransform);
            EnsureBubble();
            _bubble.transform.SetParent(null, true);
            _cachedCamera = ResolveBubbleCamera();
            _bubble.Init(_anchor, _cachedCamera, bubbleMaxWidth, bubbleOffset);
            _bubble.SetText(line);
            MakeUILineTransparent();
        }

        if (_bubble != null && _lastLineText != line)
        {
            _bubble.SetText(line);
            _lastLineText = line;
        }
    }

    // ===== 气泡 API =====

    /// <summary>
    /// 显示独立气泡（不跟随UI对话框）
    /// </summary>
    public void ShowStandalone(Transform npcRootOrAnchor, string line)
    {
        if (!npcRootOrAnchor) return;
        _standaloneMode = true;
        _standaloneNPC = npcRootOrAnchor;
        _standaloneLine = line ?? "";
        _lastLineText = "";
        _anchor = ResolveAnchor(_standaloneNPC);
        EnsureBubble();
        _bubble.transform.SetParent(null, true);
        _cachedCamera = ResolveBubbleCamera();
        _bubble.Init(_anchor, _cachedCamera, bubbleMaxWidth, bubbleOffset);
        _bubble.SetText(_standaloneLine);
        MakeUILineTransparent();
    }

    /// <summary>
    /// 绑定到NPC变换（Phase 2: 更新为支持IDialogSubject）
    /// </summary>
    public void Bind(Transform newAnchor)
    {
        if (!newAnchor) return;

        // 退出独立模式，进入绑定模式
        _standaloneMode = false;
        _standaloneNPC = null;

        _anchor = ResolveAnchor(newAnchor);

        // 如果气泡已存在，立即更新位置（避免在NPC对话框打开时看不到气泡）
        if (_bubble != null)
        {
            _cachedCamera = ResolveBubbleCamera();
            _bubble.Init(_anchor, _cachedCamera, bubbleMaxWidth, bubbleOffset);
        }

        _lastLineText = ""; // 避免气泡不刷新
    }

    /// <summary>
    /// Phase 2: 新增方法 - 直接绑定到IDialogSubject（支持Actor）
    /// </summary>
    public void BindToSubject(IDialogSubject subject)
    {
        if (subject == null) return;

        // 退出独立模式，进入绑定模式
        _standaloneMode = false;
        _standaloneNPC = null;

        _anchor = ResolveAnchor(subject.SubjectTransform);

        // 如果气泡已存在，立即更新位置
        if (_bubble != null)
        {
            _cachedCamera = ResolveBubbleCamera();
            _bubble.Init(_anchor, _cachedCamera, bubbleMaxWidth, bubbleOffset);
        }

        _lastLineText = ""; // 避免气泡不刷新
    }

    /// <summary>
    /// 绑定到NPC（保留兼容性，Phase 2: 改为使用BindToSubject）
    /// </summary>
    [System.Obsolete("Use BindToSubject(IDialogSubject) instead for Phase 2 Actor support")]
    public void BindToNPC(MonoBehaviour npcMono)
    {
        if (npcMono == null) return;
        var t = npcMono.transform;

        // 寻找锚点（优先特定字段BubbleAnchor/WorldAnchor/bubbleAnchor）
        var tp = npcMono.GetType();
        Transform anchor = null;
        var f = tp.GetField("BubbleAnchor") ?? tp.GetField("WorldAnchor") ?? tp.GetField("bubbleAnchor");
        if (f != null) anchor = f.GetValue(npcMono) as Transform;
        var p = tp.GetProperty("BubbleAnchor") ?? tp.GetProperty("WorldAnchor");
        if (p != null && anchor == null) anchor = p.GetValue(npcMono) as Transform;
        if (anchor == null) anchor = t;

        Bind(anchor);
    }


    public void SetStandaloneLine(string line)
    {
        _standaloneLine = line ?? "";
        if (_standaloneMode && _bubble) _bubble.SetText(_standaloneLine);
    }

    public void EndStandalone()
    {
        _standaloneMode = false;
        HideBubble();
    }

    // ===== 内部方法 =====

    string ReadCurrentLineText()
    {
        if (_lineTextTMP) return _lineTextTMP.text;
        if (_lineTextUGUI) return _lineTextUGUI.text;
        return "";
    }

    IDialogSubject GetCurrentSubject()
    {
        return ui != null ? ui.CurrentNPC : null;
    }

    Transform ResolveAnchor(Transform npcTransform)
    {
        if (!npcTransform) return null;

        if (!string.IsNullOrEmpty(anchorChildName))
        {
            var child = npcTransform.Find(anchorChildName);
            if (child) return child;
        }

        Collider npcCollider = npcTransform.GetComponent<Collider>();
        if (!npcCollider)
        {
            var all = npcTransform.GetComponentsInChildren<Collider>();
            float best = float.PositiveInfinity;
            foreach (var c in all)
            {
                var sz = c.bounds.size;
                float h = sz.y, vol = sz.x * sz.y * sz.z;
                float score = h * 2f + vol;
                if (score < best && h > 0.3f && vol < 50f) { best = score; npcCollider = c; }
            }
        }
        if (npcCollider)
        {
            var go = new GameObject("BubbleAnchor(auto)");
            go.transform.SetParent(npcCollider.transform, false);
            var b = npcCollider.bounds;
            go.transform.position = new Vector3(b.center.x, b.max.y, b.center.z);
            return go.transform;
        }
        return npcTransform;
    }

    void EnsureBubble()
    {
        if (_bubble == null)
            _bubble = bubblePrefab ? Instantiate(bubblePrefab)
                                   : new GameObject("SpeechBubble3D").AddComponent<SpeechBubble3D>();
    }

    Camera ResolveBubbleCamera()
    {
        if (CameraModeManager.instance != null && CameraModeManager.instance.activeCamera != null)
        {
            return CameraModeManager.instance.activeCamera;
        }

        if (RuntimeRefs.TpsCamera != null)
        {
            return RuntimeRefs.TpsCamera;
        }

        if (RuntimeRefs.FixedCamera != null)
        {
            return RuntimeRefs.FixedCamera;
        }

        return Camera.main;
    }

    void MakeUILineTransparent()
    {
        if (_lineTextTMP) { var c = _lineTextTMP.color; c.a = 0f; _lineTextTMP.color = c; }
        if (_lineTextUGUI) { var c = _lineTextUGUI.color; c.a = 0f; _lineTextUGUI.color = c; }
    }

    void RestoreUILineVisibility()
    {
        if (_lineTextTMP) { var c = _lineTextTMP.color; c.a = 1f; _lineTextTMP.color = c; }
        if (_lineTextUGUI) { var c = _lineTextUGUI.color; c.a = 1f; _lineTextUGUI.color = c; }
    }

    void HideBubble()
    {
        if (_bubble) _bubble.Hide();
        RestoreUILineVisibility();
        _lastLineText = "";
        _anchor = null;
    }
}
