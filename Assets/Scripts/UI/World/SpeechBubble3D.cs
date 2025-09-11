// Assets/Scripts/UI/World/SpeechBubble3D.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[DisallowMultipleComponent]
public class SpeechBubble3D : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;
    public Vector3 worldOffset = new Vector3(0f, 1.8f, 0f);
    public Camera mainCamera;

    [Header("UI (Auto Build If Empty)")]
    public Canvas canvas;
    public RectTransform panel;
    public Image background;
    public RectTransform tail;
    public Graphic tailGraphic;
    public TextMeshProUGUI text;
    public CanvasGroup canvasGroup;

    [Header("Sprites / Look (Optional)")]
    public Sprite roundedSprite;
    public Sprite tailSprite;
    public Vector2 tailSize = new Vector2(28, 18);
    public float tailYOffset = -6f;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.66f);
    public bool enableOutline = true;

    [Header("Layout")]
    public float maxWidth = 300f;
    public float dynamicPixelsPerUnit = 12f;
    public Vector2 padding = new Vector2(16f, 12f);
    public float worldScale = 0.0018f;

    [Header("Fade (CanvasGroup)")]
    public bool enableFade = true;
    public float fadeInDuration = 0.15f;
    public float fadeOutDuration = 0.12f;

    [Header("Typewriter")]
    public bool enableTypewriter = true;
    public float charsPerSecond = 28f;

    private Coroutine _fadeCo;
    private Coroutine _typeCo;
    private string _fullText = "";

    public void Init(Transform t, Camera cam, float maxWidth = 300f, Vector3? offset = null)
    {
        target = t;
        mainCamera = cam != null ? cam : Camera.main;
        this.maxWidth = maxWidth;
        if (offset.HasValue) worldOffset = offset.Value;

        if (!canvas) BuildRuntimeUI();
        gameObject.SetActive(true);
        ForceUpdatePosition();
        if (enableFade) Show();
        else SetAlpha(1f);
    }

    public void BuildRuntimeUI()
    {
        if (!mainCamera) mainCamera = Camera.main;

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCamera;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = dynamicPixelsPerUnit;
        gameObject.AddComponent<GraphicRaycaster>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // Panel
        panel = new GameObject("Panel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        panel.SetParent(transform, false);
        panel.pivot = new Vector2(0.5f, 0f);
        background = panel.GetComponent<Image>();
        background.raycastTarget = false;
        background.color = backgroundColor;
        if (roundedSprite)
        {
            background.sprite = roundedSprite;
            background.type = Image.Type.Sliced;
        }

        // Text
        var content = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
        content.SetParent(panel, false);
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(1f, 1f);
        content.offsetMin = new Vector2(padding.x, padding.y);
        content.offsetMax = new Vector2(-padding.x, -padding.y);

        text = content.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        text.text = "";
        text.fontSize = 22;
        text.alignment = TextAlignmentOptions.Midline;
        text.color = Color.white;

        // Tail
        tail = new GameObject("Tail", typeof(RectTransform)).GetComponent<RectTransform>();
        tail.SetParent(panel, false);
        tail.anchorMin = new Vector2(0.5f, 0f);
        tail.anchorMax = new Vector2(0.5f, 0f);
        tail.pivot = new Vector2(0.5f, 1f);
        tail.sizeDelta = tailSize;
        tail.anchoredPosition = new Vector2(0f, tailYOffset);

        if (tailSprite)
        {
            var img = tail.gameObject.AddComponent<Image>();
            img.sprite = tailSprite;
            img.raycastTarget = false;
            img.color = backgroundColor;
            tailGraphic = img;
        }
        else
        {
            var tri = tail.gameObject.AddComponent<TriangleGraphic>();
            tri.direction = TriangleGraphic.Dir.Down;
            tri.color = backgroundColor;
            tailGraphic = tri;
        }

        // 初始尺寸与缩放
        panel.sizeDelta = new Vector2(maxWidth, 50);
        transform.localScale = Vector3.one * worldScale;
    }

    public void SetText(string s)
    {
        if (!text || !canvas) BuildRuntimeUI();
        _fullText = s ?? "";

        var preferred = text.GetPreferredValues(_fullText, maxWidth, 10000f);
        float width = Mathf.Min(preferred.x + padding.x * 2f, maxWidth);
        float height = Mathf.Max(preferred.y + padding.y * 2f, 28f);
        panel.sizeDelta = new Vector2(width, height);

        if (_typeCo != null) StopCoroutine(_typeCo);
        if (enableTypewriter) _typeCo = StartCoroutine(CoTypewriter(_fullText));
        else text.text = _fullText;
    }

    System.Collections.IEnumerator CoTypewriter(string content)
    {
        text.text = "";
        int total = content.Length;
        if (total == 0) yield break;
        float t = 0f;
        int last = 0;
        while (last < total)
        {
            t += Time.deltaTime * Mathf.Max(1f, charsPerSecond);
            int count = Mathf.Clamp(Mathf.FloorToInt(t), 0, total);
            if (count != last)
            {
                text.text = content.Substring(0, count);
                last = count;
            }
            yield return null;
        }
        _typeCo = null;
    }

    void LateUpdate()
    {
        if (target) ForceUpdatePosition();

        var cam = mainCamera ? mainCamera : Camera.main;
        if (cam)
        {
            Vector3 fwd = transform.position - cam.transform.position;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
        }
    }

    public void ForceUpdatePosition()
    {
        if (target) transform.position = target.position + worldOffset;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (!enableFade) { SetAlpha(1f); return; }
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CoFade(0f, 1f, fadeInDuration));
    }

    public void Hide()
    {
        if (!enableFade) { SetAlpha(0f); gameObject.SetActive(false); return; }
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CoFade(1f, 0f, fadeOutDuration, true));
    }

    System.Collections.IEnumerator CoFade(float a, float b, float dur, bool deactivateOnEnd = false)
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        dur = Mathf.Max(0.0001f, dur);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            SetAlpha(Mathf.Lerp(a, b, k));
            yield return null;
        }
        SetAlpha(b);
        if (deactivateOnEnd) gameObject.SetActive(false);
        _fadeCo = null;
    }

    void SetAlpha(float v)
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = v;
    }

    public void CompleteTypewriter()
    {
        if (_typeCo != null) StopCoroutine(_typeCo);
        _typeCo = null;
        text.text = _fullText;
    }
}