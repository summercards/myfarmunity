using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 超简版商店（严格直加背包版）：
/// - 购买/出售一次 1 个；
/// - 购买时必须通过 InventoryBridge 直接写入背包；
///   若未绑定或添加失败，立即退款且不生成任何模型；
/// - 按“切换模式”在【购买/出售】间切换。
/// 依赖：ShopCatalogSO + PlayerWallet + InventoryBridge
/// </summary>
public class SimpleShopUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject root;                 // 整个面板（开关）
    public Transform listParent;            // 条目列表父物体
    public Button templateButton;           // 按钮模板（场景中放1个，设为不激活）

    public Button toggleModeButton;         // 切换模式按钮（购买<->出售）
    public TextMeshProUGUI toggleModeLabelTMP;
    public Text toggleModeLabelUGUI;

    public Button closeButton;              // 关闭按钮（可选）
    public TextMeshProUGUI walletTextTMP;   // 金币文本（TMP 或 UGUI 二选一）
    public Text walletTextUGUI;

    [Header("Data")]
    public ShopCatalogSO catalog;
    public PlayerWallet wallet;
    public InventoryBridge inventoryBridge; // 必须绑定；否则不可购买/出售

    enum Mode { Buy, Sell }
    Mode _mode = Mode.Buy;

    readonly List<GameObject> _spawned = new();

    void Awake()
    {
        if (root) root.SetActive(false);
        if (toggleModeButton) toggleModeButton.onClick.AddListener(ToggleMode);
        if (closeButton) closeButton.onClick.AddListener(Close);
    }

    void OnEnable()
    {
        if (wallet) wallet.onCoinsChanged.AddListener(OnCoinsChanged);
    }

    void OnDisable()
    {
        if (wallet) wallet.onCoinsChanged.RemoveListener(OnCoinsChanged);
    }

    void OnDestroy()
    {
        if (wallet) wallet.onCoinsChanged.RemoveListener(OnCoinsChanged);
    }

    public void Open()
    {
        if (!root) return;
        root.SetActive(true);
        SetMode(Mode.Buy);
        RefreshList();
        UpdateWalletText();
    }

    public void Close()
    {
        if (!root) return;
        root.SetActive(false);
        ClearList();
    }

    void ToggleMode()
    {
        SetMode(_mode == Mode.Buy ? Mode.Sell : Mode.Buy);
        RefreshList();
    }

    void SetMode(Mode m)
    {
        _mode = m;
        SetLabel(toggleModeLabelTMP, toggleModeLabelUGUI, _mode == Mode.Buy ? "切到：出售" : "切到：购买");
    }

    void OnCoinsChanged(int _) => UpdateWalletText();

    void UpdateWalletText()
    {
        if (!wallet) { SetLabel(walletTextTMP, walletTextUGUI, "金币：—"); return; }
        SetLabel(walletTextTMP, walletTextUGUI, $"金币：{wallet.coins}");
    }

    void RefreshList()
    {
        if (!catalog || !listParent || !templateButton) return;

        ClearList();

        // 确保模板不激活
        if (templateButton.gameObject.activeSelf) templateButton.gameObject.SetActive(false);

        foreach (var e in catalog.entries)
        {
            if (_mode == Mode.Sell)
            {
                if (!inventoryBridge) continue; // 没桥无法计算数量

                int have = inventoryBridge.GetCount(e.itemId);
                if (have <= 0) continue;

                var btn = SpawnButton();
                string label = $"{e.displayName}  x{have}  单价:{e.sellPrice}  [卖1]";
                SetButtonLabel(btn, label);

                btn.onClick.AddListener(() =>
                {
                    // 严格：只能直接从背包移除
                    if (inventoryBridge.TryRemove(e.itemId, 1))
                    {
                        wallet?.Add(e.sellPrice);
                        RefreshList();
                    }
                    else
                    {
                        Debug.Log("[Shop] 出售失败：移除背包物品失败（不做任何生成）。");
                    }
                });
            }
            else // Buy
            {
                var btn = SpawnButton();
                string label = $"{e.displayName}  价格:{e.buyPrice}  [买1]";
                SetButtonLabel(btn, label);

                btn.onClick.AddListener(() =>
                {
                    if (wallet == null || !wallet.TrySpend(e.buyPrice))
                    {
                        Debug.Log("[Shop] 金币不足");
                        return;
                    }

                    // 严格直加：禁止任何兜底生成模型
                    if (inventoryBridge == null)
                    {
                        Debug.Log("[Shop] 购买失败：未绑定 InventoryBridge，无法写入背包。已退款。");
                        wallet.Add(e.buyPrice);
                        return;
                    }

                    bool added = inventoryBridge.TryAdd(e.itemId, 1, null, null); // 传 null 禁用兜底
                    if (!added)
                    {
                        // 回退金币，不生成模型
                        wallet.Add(e.buyPrice);
                        Debug.Log("[Shop] 购买失败：未能写入背包（不生成模型，已退款）。");
                        return;
                    }

                    // 成功
                    UpdateWalletText();
                    if (_mode == Mode.Sell) RefreshList();
                });
            }
        }
    }

    void ClearList()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i]) Destroy(_spawned[i]);
        }
        _spawned.Clear();
    }

    Button SpawnButton()
    {
        var go = Instantiate(templateButton.gameObject, listParent);
        go.SetActive(true);
        _spawned.Add(go);
        return go.GetComponent<Button>();
    }

    void SetButtonLabel(Button b, string text)
    {
        if (!b) return;
        var tmp = b.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp) { tmp.text = text; return; }
        var ugui = b.GetComponentInChildren<Text>(true);
        if (ugui) ugui.text = text;
    }

    void SetLabel(TextMeshProUGUI tmp, Text ugui, string s)
    {
        if (tmp) { tmp.text = s; return; }
        if (ugui) ugui.text = s;
    }
}
