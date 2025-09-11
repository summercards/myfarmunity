// Assets/Scripts/UI/World/SpeechBubble3D.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 立体对话气泡（世界空间 Canvas + TMP 文本）。
/// - 将自身跟随 target，并自动面向主相机（绕 Y 轴看向）。
/// - 仅负责渲染文本与跟随，不处理输入。
/// - 可直接挂空物体上使用；也支持作为“预制体”被实例化。
/// </summary>
[DisallowMultipleComponent]
public class SpeechBubble3D : MonoBehaviour
{
    [Header("Follow")]
    [Tooltip("跟随的目标（通常是 NPC 或其子物体 BubbleAnchor）。")]
    public Transform target;

    [Tooltip("世界坐标偏移（相对 target 之上）。")]
    public Vector3 worldOffset = new Vector3(0f, 1.8f, 0f);

    [Tooltip("用于朝向的相机。为空将自动使用 Camera.main。")]
    public Camera mainCamera;

    [Header("UI (Auto Build If Empty)")]
    public Canvas canvas;                    // WorldSpace Canvas
    public RectTransform panel;              // 背景面板
    public Image background;                 // 背景图片
    public TextMeshProUGUI text;             // 文本

    [Header("Layout")]
    [Tooltip("最大宽度（像素），自动换行。")]
    public float maxWidth = 380f;
    [Tooltip("动态像素单位（越大越清晰，代价是同等尺寸看起来更小）。")]
    public float dynamicPixelsPerUnit = 10f;
    [Tooltip("文本内边距（左右、上下）。")]
    public Vector2 padding = new Vector2(16f, 12f);

    /// <summary>初始化（作为预制体实例化时可手动调用）。</summary>
    public void Init(Transform t, Camera cam, float maxWidth = 380f, Vector3? offset = null)
    {
        target = t;
        mainCamera = cam != null ? cam : Camera.main;
        this.maxWidth = maxWidth;
        if (offset.HasValue) worldOffset = offset.Value;

        if (!canvas) BuildRuntimeUI();
        gameObject.SetActive(true);
        ForceUpdatePosition();
    }

    /// <summary>运行时自动构建最简 UI 层级，避免必须做美术预制体。</summary>
    public void BuildRuntimeUI()
    {
        if (!mainCamera) mainCamera = Camera.main;

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCamera;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = dynamicPixelsPerUnit;

        gameObject.AddComponent<GraphicRaycaster>();

        panel = new GameObject("Panel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        panel.SetParent(transform, false);
        panel.pivot = new Vector2(0.5f, 0f); // 底边对齐，偏移往上长
        background = panel.GetComponent<Image>();
        background.raycastTarget = false;
        background.color = new Color(0f, 0f, 0f, 0.66f); // 半透明黑

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
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.Midline;
        text.color = Color.white;

        // 初始面板尺寸
        panel.sizeDelta = new Vector2(maxWidth, 50);
    }

    /// <summary>设置对话内容，并按内容自适应大小（宽度不超过 maxWidth）。</summary>
    public void SetText(string s)
    {
        if (!text) BuildRuntimeUI();
        if (!canvas) BuildRuntimeUI();

        text.text = s ?? "";

        // 计算文本的合适尺寸（受 maxWidth 限制）。
        var preferred = text.GetPreferredValues(text.text, maxWidth, 10000f);
        float width = Mathf.Min(preferred.x + padding.x * 2f, maxWidth);
        float height = Mathf.Max(preferred.y + padding.y * 2f, 28f);
        panel.sizeDelta = new Vector2(width, height);
    }

    void LateUpdate()
    {
        if (target)
        {
            ForceUpdatePosition();
        }

        // 朝向相机（只绕 Y 轴转，避免倾斜）。
        var cam = mainCamera ? mainCamera : Camera.main;
        if (cam)
        {
            Vector3 fwd = transform.position - cam.transform.position;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
        }
    }

    /// <summary>强制更新跟随位置。</summary>
    public void ForceUpdatePosition()
    {
        if (target)
        {
            transform.position = target.position + worldOffset;
        }
    }

    /// <summary>隐藏。</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}