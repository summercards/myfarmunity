using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 超简商店（直加背包 + 距离自动关闭）
/// - 购买/出售一次 1 个；必须通过 InventoryBridge 直接写入/移除背包；
/// - 打开后有一段“保护期”（默认 0.2s）不做距离判定，避免一闪而过；
/// - 通过 SetContext(player,npc) 注入玩家与 NPC，用于自动关闭；
/// - 未注入 npc 时不会做距离关闭。
/// 依赖：ShopCatalogSO + PlayerWallet + InventoryBridge
/// </summary>
public class SimpleShopUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject root;                 // 整个面板（开关）
    public Transform listParent;            // 条目列表父物体
    public Button templateButton;           // 按钮模板（放1个，设为不激活）

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

    [Header("Auto Close By Distance")]
    public bool autoCloseWhenFar = true;
    [Tooltip("超过该距离自动关闭商店。建议 4~5。")]
    public float closeDistance = 4.0f;
    [Tooltip("打开后在这段时间内不做距离判断，避免一闪而过。")]
    public float autoCloseGrace = 0.2f;

    [Tooltip("玩家 Transform（留空会在 Open() 时自动用 tag=Player 寻找）")]
    public Transform player;
    [Tooltip("当前交互的 NPC Transform（由 Opener 在 Open() 前注入，留空则不做距离判断）")]
    public Transform npc;

    public bool IsOpen { get; private set; }

    enum Mode { Buy, Sell }
    Mode _mode = Mode.Buy;

    readonly List<GameObject> _spawned = new();
    float _openedAt = -1f;

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

    void Update()
    {
        if (!IsOpen || !autoCloseWhenFar) return;
        if (!player || !npc) return;

        // 保护期：避免一打开就因为轻微抖动或距离判定而关闭
        if (Time.time - _openedAt < autoCloseGrace) return;

        float d = Vector3.Distance(player.position, npc.position);
        if (d > closeDistance)
        {
            Close();
        }
    }

    public void SetContext(Transform playerT, Transform npcT)
    {
        player = playerT;
        npc = npcT;
    }

    public void Open()
    {
        if (!root) return;

        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
            else if (Camera.main) player = Camera.main.transform;
        }

        IsOpen = true;
        _openedAt = Time.time;

        root.SetActive(true);
        SetMode(Mode.Buy);
        RefreshList();
        UpdateWalletText();
    }

    public void Close()
    {
        IsOpen = false;
        if (root) root.SetActive(false);
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

        if (templateButton.gameObject.activeSelf) templateButton.gameObject.SetActive(false);

        foreach (var e in catalog.entries)
        {
            if (_mode == Mode.Sell)
            {
                if (!inventoryBridge) continue;

                int have = inventoryBridge.GetCount(e.itemId);
                if (have <= 0) continue;

                var btn = SpawnButton();
                string label = $"{e.displayName}  x{have}  单价:{e.sellPrice}  [卖1]";
                SetButtonLabel(btn, label);

                btn.onClick.AddListener(() =>
                {
                    if (inventoryBridge.TryRemove(e.itemId, 1))
                    {
                        wallet?.Add(e.sellPrice);
                        RefreshList();
                    }
                    else
                    {
                        Debug.Log("[Shop] 出售失败：移除背包物品失败");
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

                    if (inventoryBridge == null)
                    {
                        Debug.Log("[Shop] 购买失败：未绑定 InventoryBridge，无法写入背包。已退款。");
                        wallet.Add(e.buyPrice);
                        return;
                    }

                    bool added = inventoryBridge.TryAdd(e.itemId, 1, null, null); // 直加背包，禁用兜底
                    if (!added)
                    {
                        wallet.Add(e.buyPrice); // 回退
                        Debug.Log("[Shop] 购买失败：未能写入背包，已退款");
                        return;
                    }

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
