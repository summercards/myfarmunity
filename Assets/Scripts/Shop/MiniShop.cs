using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MiniShop : MonoBehaviour
{
    public static MiniShop Active { get; private set; }
    public static event Action<bool> OnActiveChanged;

    [Header("Root & Layout")]
    public GameObject root;
    public Transform gridParent;
    public GameObject cellTemplate;

    [Header("Top")]
    public Button closeButton;
    public TextMeshProUGUI walletTextTMP;
    public Text walletTextUGUI;

    [Header("Data")]
    public ShopCatalogSO catalog;
    public PlayerWallet wallet;
    public InventoryBridge inventoryBridge;

    [Header("Dialog (Optional)")]
    public NPCDialogUI dialogUI; // 可不拖，运行时会自动兜底查找

    [Header("Auto Close By Distance")]
    public bool autoCloseWhenFar = true;
    public float closeDistance = 5f;
    public float autoCloseGrace = 0.5f;
    public LayerMask npcMask = ~0;

    [Header("List Options")]
    [Tooltip("为真时，购买网格只展示 buyPrice>0 的条目。")]
    public bool showOnlyBuyable = true;

    [Header("Shop Line")]
    [Tooltip("打开商店时在 NPC 头顶显示的台词（可用 OpenFromDialogWithLine 覆盖）")]
    public string shopOpenLine = "欢迎光临！需要点什么？";

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
    void OnEnable() { if (wallet) wallet.onCoinsChanged.AddListener(OnCoinsChanged); }
    void OnDisable()
    {
        if (wallet) wallet.onCoinsChanged.RemoveListener(OnCoinsChanged);
        // 保险：面板被禁用也要结束气泡（例如切场景/父级隐藏）
        EndShopBubble();
    }
    void OnDestroy()
    {
        // 保险：销毁时也结束气泡
        EndShopBubble();
    }

    void Update()
    {
        if (!IsOpen || !autoCloseWhenFar) return;
        if (!player || !npc) return;
        if (Time.time - _openedAt < autoCloseGrace) return;
        if (Vector3.Distance(player.position, npc.position) > closeDistance) Close();
    }

    /// <summary>（可选）外部手动指定 Player/NPC 上下文</summary>
    public void SetContext(Transform playerT, Transform npcT)
    {
        player = playerT;
        npc = npcT;
    }

    /// <summary>
    /// 从“NPC 对话面板”上下文打开：自动解析当前 NPC 与 Player，
    /// 在关闭面板之前就切换头顶“商店台词”，然后打开商店。
    /// </summary>
    public void OpenFromDialog()
    {
        // 兜底：找对话 UI
        if (!dialogUI) dialogUI = FindObjectOfType<NPCDialogUI>();

        // 解析当前 NPC & 桥接器（此时面板仍激活）
        Transform npcFromDialog = null;
        NPCDialogWorldBridge bridge = null;
        if (dialogUI)
        {
            if (dialogUI.CurrentNPC) npcFromDialog = dialogUI.CurrentNPC.transform;
            bridge = dialogUI.GetComponent<NPCDialogWorldBridge>();
        }

        // Player
        var holder = FindObjectOfType<PlayerInventoryHolder>();
        player = holder ? holder.transform :
                 (GameObject.FindGameObjectWithTag("Player") ?
                   GameObject.FindGameObjectWithTag("Player").transform :
                   (Camera.main ? Camera.main.transform : null));

        // NPC：优先对话中的，没有就近找
        npc = npcFromDialog ? npcFromDialog : FindClosestNpcWithInteractable(player, 6f);

        // **先**切商店台词（在关闭面板前执行）
        ShowShopBubble(shopOpenLine, npc, bridge);

        // 再关对话面板（如需要）
        if (dialogUI && dialogUI.IsOpen) dialogUI.Close();

        // 打开商店
        Open();
    }

    /// <summary>同 OpenFromDialog，但可为该次打开覆盖一条自定义台词</summary>
    public void OpenFromDialogWithLine(string line)
    {
        if (!string.IsNullOrEmpty(line)) shopOpenLine = line;
        OpenFromDialog();
    }

    public void Open()
    {
        if (!root) return;
        IsOpen = true;
        _openedAt = Time.time;
        root.SetActive(true);
        BuildGrid();
        UpdateWalletText();

        Active = this;
        OnActiveChanged?.Invoke(true);
    }

    public void Close()
    {
        IsOpen = false;
        if (root) root.SetActive(false);
        ClearGrid();

        // 结束头顶气泡（无论何种关闭路径）
        EndShopBubble();

        if (Active == this) Active = null;
        OnActiveChanged?.Invoke(false);

        // 可选：清理上下文，避免下一次误判距离
        // player = null; npc = null;
    }

    // ===== 购买、网格与辅助（原逻辑保持） =====
    void TryBuy(string itemId, int unitPrice, int qty, Action<string> tip)
    {
        if (!wallet) { tip?.Invoke("无钱包"); return; }
        if (!inventoryBridge) { tip?.Invoke("无背包"); return; }
        qty = Mathf.Max(1, qty);
        int total = unitPrice * qty;
        if (!wallet.TrySpend(total)) { tip?.Invoke("金币不足"); return; }
        bool ok = inventoryBridge.TryAdd(itemId, qty, null, null);
        if (!ok) { wallet.Add(total); tip?.Invoke("添加失败(已退)"); return; }
        UpdateWalletText(); tip?.Invoke("购买成功");
    }

    public bool QuoteSell(string itemId, int qty, out int total)
    {
        total = 0;
        if (!catalog) return false;
        var e = catalog.Get(itemId);
        if (e == null || e.sellPrice <= 0) return false;
        qty = Mathf.Max(1, qty);
        total = e.sellPrice * qty; return true;
    }
    public bool ConfirmSell(string itemId, int qty)
    {
        if (!QuoteSell(itemId, qty, out int total)) return false;
        if (!wallet) return false;
        wallet.Add(total); UpdateWalletText(); return true;
    }

    void BuildGrid()
    {
        ClearGrid();
        if (!catalog || !gridParent || !cellTemplate) { Debug.LogWarning("[MiniShop] 缺引用"); return; }
        if (cellTemplate.activeSelf) cellTemplate.SetActive(false);

        foreach (var e in catalog.entries)
        {
            if (showOnlyBuyable && e.buyPrice <= 0) continue;
            var go = Instantiate(cellTemplate, gridParent);
            go.SetActive(true); _spawned.Add(go);

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
            if (qtyIF) { qtyIF.contentType = TMP_InputField.ContentType.IntegerNumber; if (string.IsNullOrEmpty(qtyIF.text)) qtyIF.text = "1"; }
            if (tTip) tTip.text = "";

            Func<int> GetQty = () => { if (!qtyIF) return 1; return int.TryParse(qtyIF.text, out int n) ? Mathf.Max(1, n) : 1; };
            Action<string> Tip = (s) => { if (!tTip) return; tTip.text = s; CancelInvoke(nameof(ClearAllTips)); Invoke(nameof(ClearAllTips), 1.2f); };

            if (btnMinus) btnMinus.onClick.AddListener(() => { int q = Mathf.Max(1, GetQty() - 1); if (qtyIF) qtyIF.text = q.ToString(); });
            if (btnPlus) btnPlus.onClick.AddListener(() => { int q = GetQty() + 1; if (qtyIF) qtyIF.text = q.ToString(); });
            if (btnBuy) btnBuy.onClick.AddListener(() => { TryBuy(e.itemId, e.buyPrice, GetQty(), Tip); });
        }
    }

    void ClearGrid() { for (int i = 0; i < _spawned.Count; i++) if (_spawned[i]) Destroy(_spawned[i]); _spawned.Clear(); }
    void ClearAllTips() { foreach (var go in _spawned) { var tip = Find<TextMeshProUGUI>(go, "Tip"); if (tip) tip.text = ""; } }
    void OnCoinsChanged(int _) => UpdateWalletText();
    void UpdateWalletText()
    {
        string s = wallet ? $"金币：{wallet.coins}" : "金币：—";
        if (walletTextTMP) walletTextTMP.text = s;
        else if (walletTextUGUI) walletTextUGUI.text = s;
    }

    T Find<T>(GameObject rootGo, string childName) where T : Component
    { var t = rootGo.transform.Find(childName); return t ? t.GetComponent<T>() : null; }

    Transform FindClosestNpcWithInteractable(Transform around, float radius)
    {
        if (!around) return null;
        Collider[] cols = Physics.OverlapSphere(around.position, radius, npcMask, QueryTriggerInteraction.Collide);
        Transform best = null; float bestD = float.MaxValue;
        foreach (var c in cols)
        {
            var inter = c.GetComponentInParent<NPCInteractable>(); if (!inter) continue;
            float d = Vector3.Distance(around.position, inter.transform.position);
            if (d < bestD) { bestD = d; best = inter.transform; }
        }
        return best;
    }

    // ====== 气泡台词控制 ======
    void ShowShopBubble(string line, Transform targetNpc, NPCDialogWorldBridge preferredBridge = null)
    {
        var bridge = preferredBridge ? preferredBridge : FindObjectOfType<NPCDialogWorldBridge>();
        if (!bridge || !targetNpc) return;

        var anchor = targetNpc.Find("BubbleAnchor");
        bridge.ShowStandalone(anchor ? anchor : targetNpc,
            string.IsNullOrEmpty(line) ? shopOpenLine : line);
    }

    void EndShopBubble()
    {
        var bridge = FindObjectOfType<NPCDialogWorldBridge>();
        if (bridge) bridge.EndStandalone();
    }
}
