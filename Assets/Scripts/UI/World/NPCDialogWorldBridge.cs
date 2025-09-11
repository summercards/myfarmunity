using UnityEngine;
using TMPro;
using System.Reflection;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class NPCDialogWorldBridge : MonoBehaviour
{
    [Header("References")]
    public NPCDialogUI ui;
    public string anchorChildName = "BubbleAnchor";

    [Header("Bubble")]
    public SpeechBubble3D bubblePrefab;
    public Vector3 bubbleOffset = new Vector3(0f, 1.8f, 0f);
    [Range(160f, 800f)]
    public float bubbleMaxWidth = 320f;

    [Header("Behaviour")]
    public bool enableWorldBubble = true;

    [Header("Auto Hide By Distance")]
    public bool enableAutoHideByDistance = true;
    public float hideDistance = 4.0f;
    public string playerTag = "Player";
    public bool alsoCloseUIDialog = true;

    private SpeechBubble3D _bubble;
    private Transform _anchor;
    private object _currentNPC;
    private string _lastLineText = "";
    private TextMeshProUGUI _lineTextTMP;
    private Text _lineTextUGUI;
    private PropertyInfo _propCurrentNPC;
    private FieldInfo _fieldCurrentNPC;
    private Transform _player;

    // standalone shop-line
    private bool _standaloneMode = false;
    private Transform _standaloneNPC;
    private string _standaloneLine = "";

    void Awake()
    {
        if (!ui) ui = GetComponent<NPCDialogUI>();
        CacheLineText();

        _propCurrentNPC = typeof(NPCDialogUI).GetProperty("CurrentNPC", BindingFlags.Public | BindingFlags.Instance);
        _fieldCurrentNPC = typeof(NPCDialogUI).GetField("CurrentNPC", BindingFlags.Public | BindingFlags.Instance);

        if (!string.IsNullOrEmpty(playerTag))
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) _player = go.transform;
        }
        if (!_player && Camera.main) _player = Camera.main.transform;
    }

    void CacheLineText()
    {
        var t = typeof(NPCDialogUI);
        var f1 = t.GetField("lineText", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f1 != null)
        {
            _lineTextTMP = f1.GetValue(ui) as TextMeshProUGUI;
            if (!_lineTextTMP)
                _lineTextUGUI = f1.GetValue(ui) as Text;
        }
    }

    void Update()
    {
        if (!enableWorldBubble) return;

        bool isOpen = false;
        GameObject rootGO = null;
        {
            var fRoot = typeof(NPCDialogUI).GetField("root", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fRoot != null)
            {
                rootGO = fRoot.GetValue(ui) as GameObject;
                isOpen = rootGO && rootGO.activeInHierarchy;
            }
        }

        // distance auto hide
        if (enableAutoHideByDistance && _player && (_anchor || _standaloneNPC))
        {
            var refTr = _anchor ? _anchor : _standaloneNPC;
            if (refTr)
            {
                Vector3 a = _player.position; a.y = 0;
                Vector3 b = refTr.position;   b.y = 0;
                float planar = Vector3.Distance(a, b);
                if (planar > hideDistance)
                {
                    HideBubble();
                    if (alsoCloseUIDialog && rootGO) rootGO.SetActive(false);
                    _standaloneMode = false;
                    return;
                }
            }
        }

        if (_standaloneMode)
        {
            if (_bubble == null || _anchor == null)
            {
                _anchor = ResolveAnchor(_standaloneNPC);
                EnsureBubble();
                _bubble.transform.SetParent(null, true);
                _bubble.Init(_anchor, Camera.main, bubbleMaxWidth, bubbleOffset);
                _bubble.SetText(_standaloneLine);
                MakeUILineTransparent();
            }
            return;
        }

        _currentNPC = GetCurrentNPCObj();
        if (!isOpen || _currentNPC == null)
        {
            HideBubble();
            return;
        }

        string line = ReadCurrentLineText(); if (line == null) line = "";

        if (_bubble == null || _anchor == null)
        {
            _anchor = ResolveAnchor(GetNPCTransform(_currentNPC));
            EnsureBubble();
            _bubble.transform.SetParent(null, true);
            _bubble.Init(_anchor, Camera.main, bubbleMaxWidth, bubbleOffset);
            _bubble.SetText(line);
            MakeUILineTransparent();
        }

        if (_bubble != null && _lastLineText != line)
        {
            _bubble.SetText(line);
            _lastLineText = line;
        }
    }

    // API for shop/custom lines
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
        _bubble.Init(_anchor, Camera.main, bubbleMaxWidth, bubbleOffset);
        _bubble.SetText(_standaloneLine);
        MakeUILineTransparent();
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

    string ReadCurrentLineText()
    {
        if (_lineTextTMP) return _lineTextTMP.text;
        if (_lineTextUGUI) return _lineTextUGUI.text;
        return "";
    }

    object GetCurrentNPCObj()
    {
        if (_propCurrentNPC != null) return _propCurrentNPC.GetValue(ui);
        if (_fieldCurrentNPC != null) return _fieldCurrentNPC.GetValue(ui);
        return null;
    }

    Transform GetNPCTransform(object npcObj)
    {
        if (npcObj == null) return null;
        var tNpc = npcObj.GetType();
        var pTr = tNpc.GetProperty("transform", BindingFlags.Public | BindingFlags.Instance);
        return pTr != null ? pTr.GetValue(npcObj) as Transform : null;
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
                float height = sz.y;
                float volume = sz.x * sz.y * sz.z;
                float score = height * 2f + volume;
                if (score < best && height > 0.3f && volume < 50f)
                {
                    best = score;
                    npcCollider = c;
                }
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
        {
            _bubble = bubblePrefab ? Instantiate(bubblePrefab) : new GameObject("SpeechBubble3D").AddComponent<SpeechBubble3D>();
        }
    }

    void MakeUILineTransparent()
    {
        if (_lineTextTMP)
        {
            var c = _lineTextTMP.color; c.a = 0f; _lineTextTMP.color = c;
        }
        if (_lineTextUGUI)
        {
            var c2 = _lineTextUGUI.color; c2.a = 0f; _lineTextUGUI.color = c2;
        }
    }

    void RestoreUILineVisibility()
    {
        if (_lineTextTMP) { var c = _lineTextTMP.color; c.a = 1f; _lineTextTMP.color = c; }
        if (_lineTextUGUI) { var c2 = _lineTextUGUI.color; c2.a = 1f; _lineTextUGUI.color = c2; }
    }

    void HideBubble()
    {
        if (_bubble) _bubble.Hide();
        RestoreUILineVisibility();
        _lastLineText = "";
        _anchor = null;
    }
}