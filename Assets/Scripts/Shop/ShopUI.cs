using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 负责：
/// - 根据 Catalog 生成“商店商品列表”和“玩家可出售列表”
/// - 绑定购买/贩卖按钮
/// - 同步金币显示
/// 说明：行UI预制体使用 ShopItemRow 脚本
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("References")]
    public GameObject root;                 // 整个面板的根（开关）
    public ShopCatalogSO catalog;
    public InventoryBridge inventoryBridge;
    public PlayerWallet wallet;

    [Header("Vendor Panel")]
    public Transform vendorListParent;
    public ShopItemRow rowPrefab;

    [Header("Player Panel (Sell)")]
    public Transform playerListParent;

    [Header("Top Bar")]
    public TextMeshProUGUI walletText;

    readonly List<ShopItemRow> _vendorRows = new();
    readonly List<ShopItemRow> _playerRows = new();

    void Awake()
    {
        if (root) root.SetActive(false);
    }

    public void Open()
    {
        if (!root) return;
        root.SetActive(true);
        RefreshAll();
        if (wallet) wallet.onCoinsChanged.AddListener(OnCoinsChanged);
        UpdateWalletText();
    }

    public void Close()
    {
        if (!root) return;
        root.SetActive(false);
        if (wallet) wallet.onCoinsChanged.RemoveListener(OnCoinsChanged);
    }

    void OnCoinsChanged(int c) => UpdateWalletText();

    void UpdateWalletText()
    {
        if (walletText && wallet) walletText.text = $"金币：{wallet.coins}";
    }

    public void RefreshAll()
    {
        if (!catalog || !rowPrefab) return;

        // 清空旧行
        foreach (var r in _vendorRows) if (r) Destroy(r.gameObject);
        foreach (var r in _playerRows) if (r) Destroy(r.gameObject);
        _vendorRows.Clear(); _playerRows.Clear();

        // 生成商店（购买）
        foreach (var e in catalog.entries)
        {
            var row = Instantiate(rowPrefab, vendorListParent);
            row.SetupForBuy(e.displayName, e.icon, e.buyPrice, onClick: () =>
            {
                int qty = Mathf.Max(1, row.GetQuantity());
                int cost = e.buyPrice * qty;
                if (!wallet || !wallet.TrySpend(cost))
                {
                    row.FlashTip("金币不足");
                    return;
                }

                bool ok = inventoryBridge
                    ? inventoryBridge.TryAdd(e.itemId, qty, GetPlayerTransform(), e.pickupPrefab)
                    : false;

                if (!ok)
                {
                    // 失败退款（除非已用生成兜底，因为 TryAdd 返回 true）
                    wallet.Add(cost);
                    row.FlashTip("添加失败");
                }

                RefreshPlayerSellList(); // 数量变化后刷新出售列表
            });

            _vendorRows.Add(row);
        }

        RefreshPlayerSellList();
        UpdateWalletText();
    }

    void RefreshPlayerSellList()
    {
        foreach (var r in _playerRows) if (r) Destroy(r.gameObject);
        _playerRows.Clear();

        foreach (var e in catalog.entries)
        {
            int have = inventoryBridge ? inventoryBridge.GetCount(e.itemId) : 0;
            if (have <= 0) continue;

            var row = Instantiate(rowPrefab, playerListParent);
            row.SetupForSell($"{e.displayName} x{have}", e.icon, e.sellPrice, onClick: () =>
            {
                int qty = Mathf.Clamp(row.GetQuantity(), 1, have);
                bool ok = inventoryBridge && inventoryBridge.TryRemove(e.itemId, qty);
                if (!ok)
                {
                    row.FlashTip("移除失败");
                    return;
                }
                wallet?.Add(e.sellPrice * qty);
                RefreshPlayerSellList(); // 更新剩余数量
            });

            _playerRows.Add(row);
        }
    }

    Transform GetPlayerTransform()
    {
        // 尝试找主摄像机的跟随对象（简化）；或直接找场景中的 Player 标签
        var player = GameObject.FindGameObjectWithTag("Player");
        return player ? player.transform : (Camera.main ? Camera.main.transform : null);
    }
}
