using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ����
/// - ���� Catalog ���ɡ��̵���Ʒ�б��͡���ҿɳ����б�
/// - �󶨹���/������ť
/// - ͬ�������ʾ
/// ˵������UIԤ����ʹ�� ShopItemRow �ű�
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("References")]
    public GameObject root;                 // �������ĸ������أ�
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
        if (walletText && wallet) walletText.text = $"��ң�{wallet.coins}";
    }

    public void RefreshAll()
    {
        if (!catalog || !rowPrefab) return;

        // ��վ���
        foreach (var r in _vendorRows) if (r) Destroy(r.gameObject);
        foreach (var r in _playerRows) if (r) Destroy(r.gameObject);
        _vendorRows.Clear(); _playerRows.Clear();

        // �����̵꣨����
        foreach (var e in catalog.entries)
        {
            var row = Instantiate(rowPrefab, vendorListParent);
            row.SetupForBuy(e.displayName, e.icon, e.buyPrice, onClick: () =>
            {
                int qty = Mathf.Max(1, row.GetQuantity());
                int cost = e.buyPrice * qty;
                if (!wallet || !wallet.TrySpend(cost))
                {
                    row.FlashTip("��Ҳ���");
                    return;
                }

                bool ok = inventoryBridge
                    ? inventoryBridge.TryAdd(e.itemId, qty, GetPlayerTransform(), e.pickupPrefab)
                    : false;

                if (!ok)
                {
                    // ʧ���˿�����������ɶ��ף���Ϊ TryAdd ���� true��
                    wallet.Add(cost);
                    row.FlashTip("���ʧ��");
                }

                RefreshPlayerSellList(); // �����仯��ˢ�³����б�
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
                    row.FlashTip("�Ƴ�ʧ��");
                    return;
                }
                wallet?.Add(e.sellPrice * qty);
                RefreshPlayerSellList(); // ����ʣ������
            });

            _playerRows.Add(row);
        }
    }

    Transform GetPlayerTransform()
    {
        // ��������������ĸ�����󣨼򻯣�����ֱ���ҳ����е� Player ��ǩ
        var player = GameObject.FindGameObjectWithTag("Player");
        return player ? player.transform : (Camera.main ? Camera.main.transform : null);
    }
}
