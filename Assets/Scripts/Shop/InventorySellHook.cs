using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 挂到“背包格子/物品行”上：
/// - 点击“卖出”按钮：先和 MiniShop 报价，再用 InventoryBridge 从背包移除，最后由 MiniShop 加钱。
/// - qtyInput 可不绑定（默认卖 1 个）；
/// - haveText 可不绑定（显示拥有数量）；
/// - 你也可以在背包 UI 生成格子时，调用 SetItem(itemId) 动态设置物品ID。
/// </summary>
public class InventorySellHook : MonoBehaviour
{
    [Header("Refs")]
    public MiniShop shop;                   // 拖 Panel_Shop 上的 MiniShop
    public InventoryBridge bridge;          // 拖场景里的 InventoryBridge

    [Header("Item")]
    public string itemId;                   // 该格子的物品ID（背包UI生成时可用 SetItem 赋值）

    [Header("UI (Optional)")]
    public TMP_InputField qtyInput;         // 数量输入（可空，默认为 1）
    public Button sellButton;               // “卖出”按钮（必须）
    public TextMeshProUGUI tip;             // 提示文本（可空）
    public TextMeshProUGUI haveText;        // “拥有：x”显示（可空）

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
            haveText.text = $"拥有：{bridge.GetCount(itemId)}";
    }

    void OnSell()
    {
        if (string.IsNullOrEmpty(itemId)) { Tip("没有物品ID"); return; }
        if (!shop) { Tip("未绑定 MiniShop"); return; }
        if (!bridge) { Tip("未绑定 InventoryBridge"); return; }

        int have = bridge.GetCount(itemId);
        if (have <= 0) { Tip("没有可卖"); return; }

        int qty = Mathf.Clamp(GetQty(), 1, have);

        // 先向商店“报价”确认是否可卖 & 卖价
        if (!shop.QuoteSell(itemId, qty, out int total))
        {
            Tip("该物品不可出售");
            return;
        }

        // 从背包移除（真正的“卖出”发生在背包）
        bool removed = bridge.TryRemove(itemId, qty);
        if (!removed)
        {
            Tip("移除失败");
            return;
        }

        // 结算加钱（不移除物品；移除已由上一步完成）
        bool paid = shop.ConfirmSell(itemId, qty);
        if (!paid)
        {
            Tip("结算失败");
            return;
        }

        Tip($"已卖 {qty}，+{total}");
        RefreshHave(); // 更新“拥有数”
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
