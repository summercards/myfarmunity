using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ShopItemRow : MonoBehaviour
{
    [Header("Bind")]
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TMP_InputField qtyInput;
    public Button actionButton;         // ���� �� ����
    public TextMeshProUGUI actionLabel; // ������/�����ۡ�
    public TextMeshProUGUI tipText;     // ��ʱ��ʾ����ѡ��

    Action _onClick;

    public void SetupForBuy(string displayName, Sprite sp, int price, Action onClick)
    {
        if (icon) icon.sprite = sp;
        if (nameText) nameText.text = displayName;
        if (priceText) priceText.text = $"�۸�{price}";
        if (actionLabel) actionLabel.text = "����";
        if (qtyInput) qtyInput.text = "1";
        _onClick = onClick;
        if (actionButton)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => _onClick?.Invoke());
        }
        if (tipText) tipText.text = "";
    }

    public void SetupForSell(string displayName, Sprite sp, int price, Action onClick)
    {
        if (icon) icon.sprite = sp;
        if (nameText) nameText.text = displayName;
        if (priceText) priceText.text = $"���ۣ�{price}";
        if (actionLabel) actionLabel.text = "����";
        if (qtyInput) qtyInput.text = "1";
        _onClick = onClick;
        if (actionButton)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => _onClick?.Invoke());
        }
        if (tipText) tipText.text = "";
    }

    public int GetQuantity()
    {
        if (!qtyInput) return 1;
        if (int.TryParse(qtyInput.text, out int n)) return Mathf.Max(1, n);
        return 1;
    }

    public void FlashTip(string s)
    {
        if (!tipText) return;
        tipText.text = s;
        CancelInvoke(nameof(ClearTip));
        Invoke(nameof(ClearTip), 1.2f);
    }

    void ClearTip() { if (tipText) tipText.text = ""; }
}
