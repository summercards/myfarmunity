using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Refs")]
    public Image frame;            // 槽位底板
    public Image icon;             // 物品图标
    public Text countText;        // 数量（Legacy Text）
    public GameObject highlight;   // 选中高亮

    [Header("Runtime (read-only)")]
    public int slotIndex = -1;

    private InventoryUI _root;

    public void Setup(InventoryUI root, int index)
    {
        _root = root;
        slotIndex = index;

        // 兜底：没拖引用就按名字找
        if (!frame) frame = GetComponent<Image>();
        if (!icon) icon = transform.Find("Icon") ? transform.Find("Icon").GetComponent<Image>() : null;
        if (!countText) countText = transform.Find("Count") ? transform.Find("Count").GetComponent<Text>() : null;
        if (!highlight && transform.Find("Highlight")) highlight = transform.Find("Highlight").gameObject;

        // 基础设置
        if (frame) frame.raycastTarget = true;
        if (icon) { icon.preserveAspect = true; icon.raycastTarget = false; }

        // ★★★ 粗暴保证“数字一定可见”
        if (countText)
        {
            countText.gameObject.SetActive(true);
            countText.enabled = true;
            countText.raycastTarget = false;

            // 让文本“铺满整个格子”，再用对齐控制到右下角
            var rt = countText.rectTransform;
            rt.anchorMin = Vector2.zero;      // 拉伸到父节点四边
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(1f, 0f);
            rt.offsetMin = Vector2.zero;      // 不留边距（需要边距可把这两行改成 new Vector2(6,6)/(-6,-6)）
            rt.offsetMax = Vector2.zero;

            countText.alignment = TextAnchor.LowerRight;     // 右下对齐
            countText.fontSize = 24;
            countText.color = Color.white;

            // 加粗黑描边，避免被背景淹没
            var outline = countText.GetComponent<Outline>();
            if (!outline) outline = countText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            // 放到最上层（不被 Icon/Highlight 挡住）
            countText.transform.SetAsLastSibling();
        }

        // 高亮丢到最底层，避免盖住数字
        if (highlight) highlight.transform.SetAsFirstSibling();

        Refresh();
    }

    public void Refresh()
    {
        if (_root == null || slotIndex < 0) return;

        var stack = _root.GetStack(slotIndex);

        // 空槽
        if (stack == null || string.IsNullOrEmpty(stack.id) || stack.count <= 0)
        {
            if (icon)
            {
                icon.sprite = _root.emptySprite ? _root.emptySprite : null;
                var c = icon.color; c.a = _root.emptyIconAlpha; icon.color = c;
            }
            if (countText)
            {
                countText.text = "";                  // 空槽不显示数字
                countText.transform.SetAsLastSibling();
            }
            if (highlight) highlight.SetActive(false);
            return;
        }

        // 有物品
        if (icon)
        {
            var sp = _root.ResolveIcon(stack.id);
            icon.sprite = sp ? sp : (_root.emptySprite ? _root.emptySprite : null);
            icon.color = Color.white;
        }

        if (countText)
        {
            countText.text = stack.count.ToString(); // ★ 1 也显示
            countText.transform.SetAsLastSibling();  // 再次确保在最上
        }

        if (highlight) highlight.SetActive(_root.IsActiveId(stack.id));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_root == null) return;
        if (eventData.button == PointerEventData.InputButton.Left)
            _root.OnSlotLeftClick(slotIndex);
    }
}
