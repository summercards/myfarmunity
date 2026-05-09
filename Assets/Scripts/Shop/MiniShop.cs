using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmGame.Core.Contracts;
using FarmGame.Orchestration;

public class MiniShop : MonoBehaviour
{
    public static MiniShop Active { get; private set; }
    public static event Action<bool> OnActiveChanged;

    [Header("Root & Layout")]
    public GameObject root;
    public Transform gridParent;       // 商品容器
    public GameObject cellTemplate;    // 单元模板（需处于隐藏状态）

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

    // ====== 新增：最简单粗暴的“手动距离关闭” ======
    [Header("Manual Distance Close (Simple)")]
    [Tooltip("勾上后仅根据这两个对象的距离来强制关闭商店")]
    public bool manualDistanceClose = false;
    [Tooltip("把 Player 或玩家根节点拖进来")]
    public Transform manualPlayer;
    [Tooltip("把正在交互的 NPC 或其 BubbleAnchor 拖进来")]
    public Transform manualNpc;
    [Tooltip("当 manualPlayer 与 manualNpc 距离超过该值时，强制关闭商店")]
    public float manualCloseDistance = 5f;
    // =================================================

    // ====== 新增：网格布局 & 背景底 ======
    [Header("Grid Layout (New)")]
    [Tooltip("启用后，gridParent 会自动使用 GridLayoutGroup 排列")]
    public bool useGridLayout = true;
    [Tooltip("列数（>0 生效；优先使用列约束）。例如 2=两列，3=三列。")]
    public int columns = 2;
    [Tooltip("行数（>0 生效；当 columns<=0 时用行约束）")]
    public int rows = 0;
    [Tooltip("每个商品卡片的宽高")]
    public Vector2 cellSize = new Vector2(180, 220);
    [Tooltip("卡片之间的水平/垂直间距")]
    public Vector2 spacing = new Vector2(12, 12);
    [Tooltip("容器的内边距：左/上/右/下")]
    public int paddingLeft = 8, paddingTop = 8, paddingRight = 8, paddingBottom = 8;

    [Header("List Background (New)")]
    [Tooltip("商品列表的底板（可选）。建议是一个带 Image 的 RectTransform。")]
    public RectTransform listBackground;
    [Tooltip("在计算内容尺寸的基础上，额外留出的(宽,高)边距")]
    public Vector2 bgExtraPadding = new Vector2(16, 16);
    // =================================================

    public bool IsOpen { get; private set; }
    public Transform player { get; private set; }
    public Transform npc { get; private set; }

    readonly List<GameObject> _spawned = new();
    float _openedAt = -1f;
    NPCDialogUI _cachedDialogUI;
    PlayerInventoryHolder _cachedPlayerHolder;
    NPCDialogWorldBridge _cachedWorldBridge;
    IDisposable _walletEventSubscription;

    void Awake()
    {
        if (root) root.SetActive(false);
        if (closeButton) closeButton.onClick.AddListener(Close);

        if (!wallet) wallet = RuntimeRefs.PlayerWallet;
        if (!inventoryBridge) inventoryBridge = RuntimeRefs.InventoryBridge;
        _cachedDialogUI = dialogUI ? dialogUI : RuntimeRefs.DialogUI;
        _cachedPlayerHolder = RuntimeRefs.InventoryHolder;
        _cachedWorldBridge = RuntimeRefs.DialogWorldBridge;
    }

    void OnEnable()
    {
        RuntimeRefs.RegisterMiniShopUI(this);
        if (wallet) wallet.onCoinsChanged.AddListener(OnCoinsChanged);
        RuntimeRefs.DialogUIChanged += HandleDialogUIChanged;
        RuntimeRefs.InventoryHolderChanged += HandleInventoryHolderChanged;
        RuntimeRefs.DialogWorldBridgeChanged += HandleDialogWorldBridgeChanged;
        RuntimeRefs.PlayerWalletChanged += HandlePlayerWalletChanged;
        RuntimeRefs.InventoryBridgeChanged += HandleInventoryBridgeChanged;
        SubscribeWalletChangedEvent();
    }

    void OnDisable()
    {
        RuntimeRefs.UnregisterMiniShopUI(this);
        if (wallet) wallet.onCoinsChanged.RemoveListener(OnCoinsChanged);
        RuntimeRefs.DialogUIChanged -= HandleDialogUIChanged;
        RuntimeRefs.InventoryHolderChanged -= HandleInventoryHolderChanged;
        RuntimeRefs.DialogWorldBridgeChanged -= HandleDialogWorldBridgeChanged;
        RuntimeRefs.PlayerWalletChanged -= HandlePlayerWalletChanged;
        RuntimeRefs.InventoryBridgeChanged -= HandleInventoryBridgeChanged;
        _walletEventSubscription?.Dispose();
        _walletEventSubscription = null;
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
        // ====== 手动距离关闭（优先级最高） ======
        if (manualDistanceClose && manualPlayer && manualNpc)
        {
            if (Vector3.Distance(manualPlayer.position, manualNpc.position) > manualCloseDistance)
            {
                Close();
                return;
            }
        }
        // ==========================================================

        // 原有自动关闭逻辑（保留）
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
        if (!dialogUI) dialogUI = _cachedDialogUI;

        // 解析当前 NPC & 桥接器（此时面板仍激活）
        Transform npcFromDialog = null;
        NPCDialogWorldBridge bridge = null;
        if (dialogUI)
        {
            if (dialogUI.CurrentNPC != null) npcFromDialog = dialogUI.CurrentNPC.SubjectTransform;
            bridge = dialogUI.GetComponent<NPCDialogWorldBridge>();
        }

        // Player
        var holder = _cachedPlayerHolder;
        player = holder ? holder.transform : RuntimeRefs.PlayerTransform;

        // NPC：优先对话中的，没有就近找
        npc = npcFromDialog ? npcFromDialog : FindClosestDialogSubjectTransform(player, 6f);

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

    bool TryBuyThroughCommand(ShopCatalogSO.ShopEntrySO entry, int qty, Action<string> tip)
    {
        var bus = GameRuntimeContext.CommandBus;
        if (bus == null || !bus.HasHandler<BuyShopItemCommand>())
        {
            return false;
        }

        Transform playerContext = player ? player : RuntimeRefs.PlayerTransform;
        CommandResult result = bus.Execute(new BuyShopItemCommand(catalog, entry.itemId, qty, playerContext));
        tip?.Invoke(result.Success ? "购买成功" : result.Message);
        if (result.Success)
        {
            UpdateWalletText();
        }

        return true;
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

        // 预计算条目数量（考虑 showOnlyBuyable 过滤）
        int itemCount = 0;
        foreach (var e in catalog.entries)
            if (!showOnlyBuyable || e.buyPrice > 0) itemCount++;

        // 布局：需要的话添加/配置 GridLayoutGroup，并调整底板尺寸
        if (useGridLayout) SetupGridLayout(itemCount);

        // 生成格子
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
            if (qtyIF)
            {
                qtyIF.contentType = TMP_InputField.ContentType.IntegerNumber;
                if (string.IsNullOrEmpty(qtyIF.text)) qtyIF.text = "1";
            }
            if (tTip) tTip.text = "";

            Func<int> GetQty = () => { if (!qtyIF) return 1; return int.TryParse(qtyIF.text, out int n) ? Mathf.Max(1, n) : 1; };
            Action<string> Tip = (s) => { if (!tTip) return; tTip.text = s; CancelInvoke(nameof(ClearAllTips)); Invoke(nameof(ClearAllTips), 1.2f); };

            if (btnMinus) btnMinus.onClick.AddListener(() =>
            { int q = Mathf.Max(1, GetQty() - 1); if (qtyIF) qtyIF.text = q.ToString(); });

            if (btnPlus) btnPlus.onClick.AddListener(() =>
            { int q = GetQty() + 1; if (qtyIF) qtyIF.text = q.ToString(); });

            if (btnBuy) btnBuy.onClick.AddListener(() =>
            {
                int qty = GetQty();
                if (!TryBuyThroughCommand(e, qty, Tip))
                {
                    TryBuy(e.itemId, e.buyPrice, qty, Tip);
                }
            });
        }
    }

    // —— 新增：根据 Inspector 设置配置 GridLayoutGroup，并计算底板尺寸 ——
    void SetupGridLayout(int itemCount)
    {
        if (!gridParent) return;

        // 禁用可能存在的其他 LayoutGroup，避免冲突
        var vg = gridParent.GetComponent<VerticalLayoutGroup>(); if (vg) vg.enabled = false;
        var hg = gridParent.GetComponent<HorizontalLayoutGroup>(); if (hg) hg.enabled = false;

        // 确保有 GridLayoutGroup
        var gl = gridParent.GetComponent<GridLayoutGroup>();
        if (!gl) gl = gridParent.gameObject.AddComponent<GridLayoutGroup>();

        gl.cellSize = cellSize;
        gl.spacing = spacing;
        gl.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
        gl.startAxis = GridLayoutGroup.Axis.Horizontal;     // 先排满一行再换行
        gl.childAlignment = TextAnchor.UpperLeft;

        // 约束优先级：优先使用“列数”；若列数<=0且行数>0，则用“行数”
        if (columns > 0)
        {
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = Mathf.Max(1, columns);
        }
        else if (rows > 0)
        {
            gl.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            gl.constraintCount = Mathf.Max(1, rows);
        }
        else
        {
            gl.constraint = GridLayoutGroup.Constraint.Flexible; // 都没填则不做约束
        }

        // 自动计算底板大小
        if (listBackground)
        {
            var rtGrid = gridParent as RectTransform;
            if (!rtGrid) rtGrid = gridParent.GetComponent<RectTransform>();
            if (rtGrid)
            {
                // 估算最终占用的列/行数
                int useCols, useRows;
                if (columns > 0)
                {
                    useCols = Mathf.Min(columns, Mathf.Max(1, itemCount));
                    useRows = Mathf.CeilToInt(itemCount / Mathf.Max(1f, columns));
                }
                else if (rows > 0)
                {
                    useRows = Mathf.Min(rows, Mathf.Max(1, itemCount));
                    useCols = Mathf.CeilToInt(itemCount / Mathf.Max(1f, rows));
                }
                else
                {
                    // 都不约束，默认单列
                    useCols = 1;
                    useRows = itemCount;
                }

                float contentW = paddingLeft + paddingRight
                                 + useCols * cellSize.x
                                 + Mathf.Max(0, useCols - 1) * spacing.x;

                float contentH = paddingTop + paddingBottom
                                 + useRows * cellSize.y
                                 + Mathf.Max(0, useRows - 1) * spacing.y;

                var bgSize = new Vector2(contentW, contentH) + bgExtraPadding * 2f;
                listBackground.sizeDelta = bgSize;

                // 让底板和网格对齐在一起（假设同父级，左上对齐）
                // 如需更复杂的定位，可在 Inspector 自己调整锚点/对齐
            }
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

    void OnCoinsChanged(int _) => UpdateWalletText();

    void SubscribeWalletChangedEvent()
    {
        _walletEventSubscription?.Dispose();
        _walletEventSubscription = null;

        var eventBus = GameRuntimeContext.EventBus;
        if (eventBus != null)
        {
            _walletEventSubscription = eventBus.Subscribe<WalletChangedEvent>(HandleWalletChangedEvent);
        }
    }

    void HandleWalletChangedEvent(WalletChangedEvent walletChanged)
    {
        UpdateWalletText();
    }

    void UpdateWalletText()
    {
        string s = wallet ? $"金币：{wallet.coins}" : "金币：—";
        if (walletTextTMP) walletTextTMP.text = s;
        else if (walletTextUGUI) walletTextUGUI.text = s;
    }

    T Find<T>(GameObject rootGo, string childName) where T : Component
    {
        var t = rootGo.transform.Find(childName);
        return t ? t.GetComponent<T>() : null;
    }

    Transform FindClosestDialogSubjectTransform(Transform around, float radius)
    {
        if (!around) return null;
        Collider[] cols = Physics.OverlapSphere(around.position, radius, npcMask, QueryTriggerInteraction.Collide);
        Transform best = null; float bestD = float.MaxValue;
        foreach (var c in cols)
        {
            if (!TryGetDialogSubject(c, out IDialogSubject subject))
            {
                continue;
            }

            Transform subjectTransform = subject.SubjectTransform;
            if (!subjectTransform && subject is Component comp)
            {
                subjectTransform = comp.transform;
            }

            if (!subjectTransform)
            {
                continue;
            }

            float d = Vector3.Distance(around.position, subjectTransform.position);
            if (d < bestD) { bestD = d; best = subjectTransform; }
        }
        return best;
    }

    bool TryGetDialogSubject(Component source, out IDialogSubject subject)
    {
        subject = null;
        if (!source) return false;

        MonoBehaviour[] candidates = source.GetComponentsInParent<MonoBehaviour>(true);
        foreach (MonoBehaviour candidate in candidates)
        {
            if (candidate is IDialogSubject dialogSubject)
            {
                subject = dialogSubject;
                return true;
            }
        }

        return false;
    }

    // ====== 气泡台词控制 ======
    void ShowShopBubble(string line, Transform targetNpc, NPCDialogWorldBridge preferredBridge = null)
    {
        var bridge = preferredBridge ? preferredBridge : _cachedWorldBridge;
        if (!bridge || !targetNpc) return;

        var anchor = targetNpc.Find("BubbleAnchor");
        bridge.ShowStandalone(anchor ? anchor : targetNpc,
            string.IsNullOrEmpty(line) ? shopOpenLine : line);
    }

    void EndShopBubble()
    {
        if (_cachedWorldBridge) _cachedWorldBridge.EndStandalone();
    }

    void HandleDialogUIChanged(NPCDialogUI dialogUi)
    {
        if (!dialogUI)
        {
            _cachedDialogUI = dialogUi;
        }
    }

    void HandleInventoryHolderChanged(PlayerInventoryHolder holder)
    {
        _cachedPlayerHolder = holder;
    }

    void HandleDialogWorldBridgeChanged(NPCDialogWorldBridge bridge)
    {
        _cachedWorldBridge = bridge;
    }

    void HandlePlayerWalletChanged(PlayerWallet playerWallet)
    {
        if (wallet != null)
        {
            wallet.onCoinsChanged.RemoveListener(OnCoinsChanged);
        }

        wallet = playerWallet;

        if (wallet != null && isActiveAndEnabled)
        {
            wallet.onCoinsChanged.AddListener(OnCoinsChanged);
            UpdateWalletText();
        }
    }

    void HandleInventoryBridgeChanged(InventoryBridge bridge)
    {
        inventoryBridge = bridge;
    }
}
