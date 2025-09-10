using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// �ҵ�����������/��Ʒ�С��ϣ�
/// - �������������ť���Ⱥ� MiniShop ���ۣ����� InventoryBridge �ӱ����Ƴ�������� MiniShop ��Ǯ��
/// - qtyInput �ɲ��󶨣�Ĭ���� 1 ������
/// - haveText �ɲ��󶨣���ʾӵ����������
/// - ��Ҳ�����ڱ��� UI ���ɸ���ʱ������ SetItem(itemId) ��̬������ƷID��
/// </summary>
public class InventorySellHook : MonoBehaviour
{
    [Header("Refs")]
    public MiniShop shop;                   // �� Panel_Shop �ϵ� MiniShop
    public InventoryBridge bridge;          // �ϳ������ InventoryBridge

    [Header("Item")]
    public string itemId;                   // �ø��ӵ���ƷID������UI����ʱ���� SetItem ��ֵ��

    [Header("UI (Optional)")]
    public TMP_InputField qtyInput;         // �������루�ɿգ�Ĭ��Ϊ 1��
    public Button sellButton;               // ����������ť�����룩
    public TextMeshProUGUI tip;             // ��ʾ�ı����ɿգ�
    public TextMeshProUGUI haveText;        // ��ӵ�У�x����ʾ���ɿգ�

    void Awake()
    {
        if (sellButton) sellButton.onClick.AddListener(OnSell);
        RefreshHave();
    }

    public void SetItem(string id)
    {
        itemId = id;
        RefreshHave();
    }

    int GetQty()
    {
        if (!qtyInput) return 1;
        if (int.TryParse(qtyInput.text, out int n)) return Mathf.Max(1, n);
        return 1;
    }

    void RefreshHave()
    {
        if (haveText && bridge && !string.IsNullOrEmpty(itemId))
            haveText.text = $"ӵ�У�{bridge.GetCount(itemId)}";
    }

    void OnSell()
    {
        if (string.IsNullOrEmpty(itemId)) { Tip("û����ƷID"); return; }
        if (!shop) { Tip("δ�� MiniShop"); return; }
        if (!bridge) { Tip("δ�� InventoryBridge"); return; }

        int have = bridge.GetCount(itemId);
        if (have <= 0) { Tip("û�п���"); return; }

        int qty = Mathf.Clamp(GetQty(), 1, have);

        // �����̵ꡰ���ۡ�ȷ���Ƿ���� & ����
        if (!shop.QuoteSell(itemId, qty, out int total))
        {
            Tip("����Ʒ���ɳ���");
            return;
        }

        // �ӱ����Ƴ��������ġ������������ڱ�����
        bool removed = bridge.TryRemove(itemId, qty);
        if (!removed)
        {
            Tip("�Ƴ�ʧ��");
            return;
        }

        // �����Ǯ�����Ƴ���Ʒ���Ƴ�������һ����ɣ�
        bool paid = shop.ConfirmSell(itemId, qty);
        if (!paid)
        {
            Tip("����ʧ��");
            return;
        }

        Tip($"���� {qty}��+{total}");
        RefreshHave(); // ���¡�ӵ������
    }

    void Tip(string s)
    {
        if (!tip) return;
        tip.text = s;
        CancelInvoke(nameof(ClearTip));
        Invoke(nameof(ClearTip), 1.2f);
    }

    void ClearTip()
    {
        if (tip) tip.text = "";
    }
}
