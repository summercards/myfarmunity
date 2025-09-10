using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 一体化商店：UI+打开+隐藏对话+距离自动关闭+给背包的售卖结算（不移除物品）
/// 依赖：ShopCatalogSO + PlayerWallet + InventoryBridge (+ 可选 NPCDialogUI)
///
/// 只需一个面板(root) + 一个网格容器(gridParent) + 一个格子模板(cellTemplate)
/// ——格子模板需要的子物体（按名字自动找，缺了也能跑，默认数量=1）：
///   Icon(Image) / Name(TextMeshProUGUI) / Price(TextMeshProUGUI) /
///   Qty(TMP_InputField) / BtnBuy(Button) / [可选]BtnMinus(Button) / BtnPlus(Button) / Tip(TextMeshProUGUI)
///
/// 使用：
/// 1) 把本脚本挂在你的商店面板 GameObject（比如 Panel_Shop）
/// 2) 绑定下列字段：root、gridParent、cellTemplate、catalog、wallet、inventoryBridge、[walletTextTMP/UGUI]
/// 3) NPC 的 onFunction → 指到本脚本的 OpenFromDialog()（会自动关闭对话并锁定当前NPC）
///
/// 售卖：你的背包UI调用 QuoteSell/ConfirmSell 两步：
///   if (shop.QuoteSell(itemId, qty, out var total) && Inventory.Remove(id, qty)) shop.ConfirmSell(itemId, qty);
/// </summary>
public class MiniShop : MonoBehaviour
{
    [Header("Root & Layout")]
    public GameObject root;                 // 整个商店面板（开关）
    public Transform gridParent;            // 网格父物体（建议挂 GridLayoutGroup）
    public GameObject cellTemplate;         // 格子模板(禁用状态)

    [Header("Top")]
    public Button closeButton;              // 关闭按钮（可空）
    public TextMeshProUGUI walletTextTMP;   // 金币显示（TMP/UGUI 任选其一）
    public Text walletTextUGUI;

    [Header("Data")]
    public ShopCatalogSO catalog;
    public PlayerWallet wallet;
    public InventoryBridge inventoryBridge;

    [Header("Dialog (Optional)")]
    public NPCDialogUI dialogUI;            // 打开时会 Close()，并优先取当前 NPC

    [Header("Auto Close By Distance")]
    public bool autoCloseWhenFar = true;
    public float closeDistance = 5f;        // 超过该距离关闭
    public float autoCloseGrace = 0.5f;     // 打开后的保护期
    public LayerMask npcMask = ~0;          // 兜底查 NPC 用

    // 运行时
    public bool IsOpen { get; private set; }
    public Transform player { get; private set; }
    public Transform npc { get; private set; }

    readonly List<GameObject> _spawned = new();
    float _openedAt = -1f;

    void Awake()
    {
        if (root) root.SetActive(false);
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
        if (Time.time - _openedAt < autoCloseGrace) return;

        float d = Vector3.Distance(player.position, npc.position);
        if (d > closeDistance) Close();
    }

    // ====== 打开入口（给 NPC 的 onFunction 直接调用）======
    public void OpenFromDialog()
    {
        // 1) 关闭对话并获取当前 NPC
        Transform fromDialog = null;
        if (dialogUI && dialogUI.IsOpen)
        {
            var curr = dialogUI.CurrentNPC;
            if (curr) fromDialog = curr.transform;
            dialogUI.Close();
        }

        // 2) 玩家 Transform（优先 PlayerInventoryHolder、再 tag=Player、最后 MainCamera）
        var holder = FindObjectOfType<PlayerInventoryHolder>();
        player = holder ? holder.transform : null;
        if (!player)
        {
            var tagP = GameObject.FindGameObjectWithTag("Player");
            player = tagP ? tagP.transform : (Camera.main ? Camera.main.transform : null);
        }

        // 3) NPC：优先对话拿到的，否则在半径内找最近的带 NPCInteractable 的物体
        npc = fromDialog ? fromDialog : FindClosestNpcWithInteractable(player, 6f);

        Open();
    }

    // ====== 基本开关 ======
    public void Open()
    {
        if (!root) return;

        IsOpen = true;
        _openedAt = Time.time;

        root.SetActive(true);
        BuildGrid();
        UpdateWalletText();
    }

    public void Close()
    {
        IsOpen = false;
        if (root) root.SetActive(false);
        ClearGrid();
    }

    // ====== 购买 ======
    void TryBuy(string itemId, int unitPrice, int qty, Action<string> tip)
    {
        if (!wallet) { tip?.Invoke("无钱包"); return; }
        if (!inventoryBridge) { tip?.Invoke("无背包"); return; }

        qty = Mathf.Max(1, qty);
        int total = unitPrice * qty;
        if (!wallet.TrySpend(total))
        {
            tip?.Invoke("金币不足");
            return;
        }

        bool ok = inventoryBridge.TryAdd(itemId, qty, null, null); // 直加背包
        if (!ok)
        {
            wallet.Add(total); // 退款
            tip?.Invoke("添加失败(已退)");
            return;
        }

        UpdateWalletText();
        tip?.Invoke("购买成功");
    }

    // ====== 售卖结算（给你的背包UI调用，不移除物品）======
    public bool QuoteSell(string itemId, int qty, out int total)
    {
        total = 0;
        if (!catalog) return false;
        var e = catalog.Get(itemId);
        if (e == null || e.sellPrice <= 0) return false;
        qty = Mathf.Max(1, qty);
        total = e.sellPrice * qty;
        return true;
    }

    public bool ConfirmSell(string itemId, int qty)
    {
        if (!QuoteSell(itemId, qty, out int total)) return false;
        if (!wallet) return false;
        wallet.Add(total);
        UpdateWalletText();
        return true;
    }

    // ====== 构建网格 ======
    void BuildGrid()
    {
        ClearGrid();
        if (!catalog || !gridParent || !cellTemplate)
        {
            Debug.LogWarning("[MiniShop] 缺少 catalog/gridParent/cellTemplate");
            return;
        }
        if (cellTemplate.activeSelf) cellTemplate.SetActive(false);

        foreach (var e in catalog.entries)
        {
            var go = Instantiate(cellTemplate, gridParent);
            go.SetActive(true);
            _spawned.Add(go);

            // 自动抓取子组件
            var icon = Find<Image>(go, "Icon");
            var tName = Find<TextMeshProUGUI>(go, "Name");
            var tPrice = Find<TextMeshProUGUI>(go, "Price");
            var tTip = Find<TextMeshProUGUI>(go, "Tip");
            var qtyIF = Find<TMP_InputField>(go, "Qty");
            var btnBuy = Find<Button>(go, "BtnBuy");
            var btnMinus = Find<Button>(go, "BtnMinus");
            var btnPlus = Find<Button>(go, "BtnPlus");

            if (icon) icon.sprite = e.icon;
            if (tName) tName.text = string.IsNullOrEmpty(e.displayName) ? e.itemId : e.displayName;
            if (tPrice) tPrice.text = $"单价：{e.buyPrice}";
            if (qtyIF) qtyIF.contentType = TMP_InputField.ContentType.IntegerNumber;
            if (qtyIF && string.IsNullOrEmpty(qtyIF.text)) qtyIF.text = "1";
            if (tTip) tTip.text = "";

            Func<int> GetQty = () =>
            {
                if (!qtyIF) return 1;
                return int.TryParse(qtyIF.text, out int n) ? Mathf.Max(1, n) : 1;
            };
            Action<string> Tip = (s) =>
            {
                if (!tTip) return;
                tTip.text = s;
                CancelInvoke(nameof(ClearAllTips));
                Invoke(nameof(ClearAllTips), 1.2f);
            };

            if (btnMinus) btnMinus.onClick.AddListener(() =>
            {
                int q = Mathf.Max(1, GetQty() - 1);
                if (qtyIF) qtyIF.text = q.ToString();
            });
            if (btnPlus) btnPlus.onClick.AddListener(() =>
            {
                int q = GetQty() + 1;
                if (qtyIF) qtyIF.text = q.ToString();
            });
            if (btnBuy) btnBuy.onClick.AddListener(() =>
            {
                TryBuy(e.itemId, e.buyPrice, GetQty(), Tip);
            });
        }
    }

    void ClearGrid()
    {
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i]) Destroy(_spawned[i]);
        _spawned.Clear();
    }

    void ClearAllTips()
    {
        foreach (var go in _spawned)
        {
            var tip = Find<TextMeshProUGUI>(go, "Tip");
            if (tip) tip.text = "";
        }
    }

    // ====== 辅助 ======
    void OnCoinsChanged(int _) => UpdateWalletText();

    void UpdateWalletText()
    {
        string s = wallet ? $"金币：{wallet.coins}" : "金币：—";
        if (walletTextTMP) walletTextTMP.text = s;
        else if (walletTextUGUI) walletTextUGUI.text = s;
    }

    T Find<T>(GameObject rootGo, string childName) where T : Component
    {
        if (!rootGo) return null;
        var t = rootGo.transform.Find(childName);
        return t ? t.GetComponent<T>() : null;
    }

    Transform FindClosestNpcWithInteractable(Transform around, float radius)
    {
        if (!around) return null;
        Collider[] cols = Physics.OverlapSphere(around.position, radius, npcMask, QueryTriggerInteraction.Collide);
        Transform best = null; float bestD = float.MaxValue;
        foreach (var c in cols)
        {
            var inter = c.GetComponentInParent<NPCInteractable>();
            if (!inter) continue;
            float d = Vector3.Distance(around.position, inter.transform.position);
            if (d < bestD) { bestD = d; best = inter.transform; }
        }
        return best;
    }
}
