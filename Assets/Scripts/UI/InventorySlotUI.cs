using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Refs")]
    public Image frame;            // ��λ�װ�
    public Image icon;             // ��Ʒͼ��
    public Text countText;        // ������Legacy Text��
    public GameObject highlight;   // ѡ�и���

    [Header("Runtime (read-only)")]
    public int slotIndex = -1;

    private InventoryUI _root;

    public void Setup(InventoryUI root, int index)
    {
        _root = root;
        slotIndex = index;

        // ���ף�û�����þͰ�������
        if (!frame) frame = GetComponent<Image>();
        if (!icon) icon = transform.Find("Icon") ? transform.Find("Icon").GetComponent<Image>() : null;
        if (!countText) countText = transform.Find("Count") ? transform.Find("Count").GetComponent<Text>() : null;
        if (!highlight && transform.Find("Highlight")) highlight = transform.Find("Highlight").gameObject;

        // ��������
        if (frame) frame.raycastTarget = true;
        if (icon) { icon.preserveAspect = true; icon.raycastTarget = false; }

        // ���� �ֱ���֤������һ���ɼ���
        if (countText)
        {
            countText.gameObject.SetActive(true);
            countText.enabled = true;
            countText.raycastTarget = false;

            // ���ı��������������ӡ������ö�����Ƶ����½�
            var rt = countText.rectTransform;
            rt.anchorMin = Vector2.zero;      // ���쵽���ڵ��ı�
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(1f, 0f);
            rt.offsetMin = Vector2.zero;      // �����߾ࣨ��Ҫ�߾�ɰ������иĳ� new Vector2(6,6)/(-6,-6)��
            rt.offsetMax = Vector2.zero;

            countText.alignment = TextAnchor.LowerRight;     // ���¶���
            countText.fontSize = 24;
            countText.color = Color.white;

            // �Ӵֺ���ߣ����ⱻ������û
            var outline = countText.GetComponent<Outline>();
            if (!outline) outline = countText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            // �ŵ����ϲ㣨���� Icon/Highlight ��ס��
            countText.transform.SetAsLastSibling();
        }

        // ����������ײ㣬�����ס����
        if (highlight) highlight.transform.SetAsFirstSibling();

        Refresh();
    }

    public void Refresh()
    {
        if (_root == null || slotIndex < 0) return;

        var stack = _root.GetStack(slotIndex);

        // �ղ�
        if (stack == null || string.IsNullOrEmpty(stack.id) || stack.count <= 0)
        {
            if (icon)
            {
                icon.sprite = _root.emptySprite ? _root.emptySprite : null;
                var c = icon.color; c.a = _root.emptyIconAlpha; icon.color = c;
            }
            if (countText)
            {
                countText.text = "";                  // �ղ۲���ʾ����
                countText.transform.SetAsLastSibling();
            }
            if (highlight) highlight.SetActive(false);
            return;
        }

        // ����Ʒ
        if (icon)
        {
            var sp = _root.ResolveIcon(stack.id);
            icon.sprite = sp ? sp : (_root.emptySprite ? _root.emptySprite : null);
            icon.color = Color.white;
        }

        if (countText)
        {
            countText.text = stack.count.ToString(); // �� 1 Ҳ��ʾ
            countText.transform.SetAsLastSibling();  // �ٴ�ȷ��������
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
