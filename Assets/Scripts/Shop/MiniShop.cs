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
    public NPCDialogUI dialogUI;

    [Header("Auto Close By Distance")]
    public bool autoCloseWhenFar = true;
    public float closeDistance = 5f;
    public float autoCloseGrace = 0.5f;
    public LayerMask npcMask = ~0;

    [Header("List Options")]
    [Tooltip("为真时，购买网格只展示 buyPrice>0 的条目。目录里即使配置了 sellPrice（用于回收），但 buyPrice<=0 也不会出现在购买列表。")]
    public bool showOnlyBuyable = true;

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
    void OnDisable() { if (wallet) wallet.onCoinsChanged.RemoveListener(OnCoinsChanged); }

    void Update()
    {
        if (!IsOpen || !autoCloseWhenFar) return;
        if (!player || !npc) return;
        if (Time.time - _openedAt < autoCloseGrace) return;
        if (Vector3.Distance(player.position, npc.position) > closeDistance) Close();
    }

    public void OpenFromDialog()
    {
        Transform fromDialog = null;
        if (dialogUI && dialogUI.IsOpen)
        {
            var curr = dialogUI.CurrentNPC;
            if (curr) fromDialog = curr.transform;
            dialogUI.Close();
        }
        var holder = FindObjectOfType<PlayerInventoryHolder>();
        player = holder ? holder.transform :
                 (GameObject.FindGameObjectWithTag("Player") ?
                   GameObject.FindGameObjectWithTag("Player").transform :
                   (Camera.main ? Camera.main.transform : null));
        npc = fromDialog ? fromDialog : FindClosestNpcWithInteractable(player, 6f);
        Open();
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

        if (Active == this) Active = null;
        OnActiveChanged?.Invoke(false);
    }

    // ===== 购买、网格与辅助 =====
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

    // ==== 关键：出售报价来自目录，即使不买也要有 sellPrice ====
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
            if (showOnlyBuyable && e.buyPrice <= 0) continue;   // 只展示可购买
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
    { string s = wallet ? $"金币：{wallet.coins}" : "金币：—"; if (walletTextTMP) walletTextTMP.text = s; else if (walletTextUGUI) walletTextUGUI.text = s; }

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
}
