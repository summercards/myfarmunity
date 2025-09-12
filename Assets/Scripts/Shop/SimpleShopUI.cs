using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;

public class SimpleShopUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject root;                 // 商店根 GameObject（通常就是本物体）
    public Transform listParent;            // 列表父物体
    public Button templateButton;           // 条目模板（运行时克隆，模板本身保持隐藏）

    public Button toggleModeButton;         // 买<->卖 切换按钮
    public TextMeshProUGUI toggleModeLabelTMP;
    public Text toggleModeLabelUGUI;

    public Button closeButton;              // 关闭按钮
    public TextMeshProUGUI walletTextTMP;   // 金币文本（TMP或UGUI二选一）
    public Text walletTextUGUI;

    [Header("Data")]
    public ShopCatalogSO catalog;
    public PlayerWallet wallet;
    public InventoryBridge inventoryBridge;

    [Header("Auto Close By Distance")]
    [Tooltip("打开后与 NPC 的水平距离超过该值将自动关闭商店")]
    public bool autoCloseWhenFar = true;
    public float closeDistance = 4.0f;
    [Tooltip("打开后的宽限时间，避免刚打开立刻被误判过远")]
    public float autoCloseGrace = 0.2f;

    [Tooltip("玩家 Transform（为空时在 Open() 自动找 Tag=Player 或主相机）")]
    public Transform player;
    [Tooltip("当前 NPC Transform（可在打开前通过 SetContext 传入；若为空，将在 Open() 内从 NPCDialogUI 解析）")]
    public Transform npc;

    [Header("Shop Line")]
    [Tooltip("打开商店时在 NPC 头顶显示的台词")]
    public string shopOpenLine = "欢迎光临！需要点什么？";

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
        if (templateButton && templateButton.gameObject.activeSelf)
            templateButton.gameObject.SetActive(false);
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

        Vector3 a = player.position; a.y = 0;
        Vector3 b = npc.position; b.y = 0;
        if (Vector3.Distance(a, b) > closeDistance)
        {
            Close();
        }
    }

    /// <summary>外部可在打开前把 Player/NPC 传进来（可选）</summary>
    public void SetContext(Transform playerT, Transform npcT)
    {
        player = playerT;
        npc = npcT;
    }

    // ================== 关键：通用打开方法（供按钮直接调用） ==================

    /// <summary>
    /// 在“对话面板”上下文中打开商店：
    /// - 自动解析当前 NPC（来自 NPCDialogUI.CurrentNPC）与 Player；
    /// - 自动在 NPC 头顶切换“商店台词”；
    /// 适合在每个 NPC 的按钮 OnClick 直接指向“目标商店面板”的这个方法。
    /// </summary>
    public void OpenForCurrentDialogNPC()
    {
        // 解析 Player（若未提供）
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
            else if (Camera.main) player = Camera.main.transform;
        }

        // 解析 NPC（优先用已有 npc，其次从 NPCDialogUI 反射）
        if (!npc)
        {
            var ui = Object.FindObjectOfType<NPCDialogUI>();
            if (ui != null)
            {
                var prop = typeof(NPCDialogUI).GetProperty("CurrentNPC", BindingFlags.Public | BindingFlags.Instance);
                var fld = typeof(NPCDialogUI).GetField("CurrentNPC", BindingFlags.Public | BindingFlags.Instance);
                object npcObj = prop != null ? prop.GetValue(ui) : (fld != null ? fld.GetValue(ui) : null);
                if (npcObj != null)
                {
                    var tNpc = npcObj.GetType();
                    var pTr = tNpc.GetProperty("transform", BindingFlags.Public | BindingFlags.Instance);
                    npc = pTr != null ? pTr.GetValue(npcObj) as Transform : null;
                }
            }
        }

        // 直接走本类 Open()（里边会完成台词切换与 UI 打开）
        Open();
    }

    /// <summary>
    /// 与 OpenForCurrentDialogNPC 相同，但可自定义“商店台词”。
    /// 方便你在不同 NPC 上配置不同文案（按钮 OnClick 传字符串即可）。
    /// </summary>
    public void OpenForCurrentDialogNPCWithLine(string line)
    {
        if (!string.IsNullOrEmpty(line)) shopOpenLine = line;
        OpenForCurrentDialogNPC();
    }

    // ================== 核心打开/关闭 ==================

    public void Open()
    {
        if (!root) return;

        // 兜底：解析 Player
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
            else if (Camera.main) player = Camera.main.transform;
        }

        // 兜底：解析 NPC（如果此时还没有）
        if (!npc)
        {
            var ui = Object.FindObjectOfType<NPCDialogUI>();
            if (ui != null)
            {
                var prop = typeof(NPCDialogUI).GetProperty("CurrentNPC", BindingFlags.Public | BindingFlags.Instance);
                var fld = typeof(NPCDialogUI).GetField("CurrentNPC", BindingFlags.Public | BindingFlags.Instance);
                object npcObj = prop != null ? prop.GetValue(ui) : (fld != null ? fld.GetValue(ui) : null);
                if (npcObj != null)
                {
                    var tNpc = npcObj.GetType();
                    var pTr = tNpc.GetProperty("transform", BindingFlags.Public | BindingFlags.Instance);
                    npc = pTr != null ? pTr.GetValue(npcObj) as Transform : null;
                }
            }
        }

        IsOpen = true;
        _openedAt = Time.time;

        root.SetActive(true);
        SetMode(Mode.Buy);
        RefreshList();
        UpdateWalletText();

        // —— 打开商店：切换到“商店台词” ——
        var bridge = Object.FindObjectOfType<NPCDialogWorldBridge>();
        if (bridge != null && npc != null)
        {
            var anchor = npc.Find("BubbleAnchor");
            bridge.ShowStandalone(anchor ? anchor : npc, shopOpenLine);
        }
    }

    public void Close()
    {
        IsOpen = false;
        if (root) root.SetActive(false);
        ClearList();

        // —— 结束商店台词 ——
        var bridge2 = Object.FindObjectOfType<NPCDialogWorldBridge>();
        if (bridge2 != null) bridge2.EndStandalone();
    }

    // ================== 其余保持不变 ==================

    void ToggleMode()
    {
        SetMode(_mode == Mode.Buy ? Mode.Sell : Mode.Buy);
        RefreshList();
    }

    void SetMode(Mode m)
    {
        _mode = m;
        SetLabel(toggleModeLabelTMP, toggleModeLabelUGUI, _mode == Mode.Buy ? "购买" : "卖出");
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
                string label = $"{e.displayName}  ×{have}   卖出: {e.sellPrice}";
                SetButtonLabel(btn, label);

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    if (inventoryBridge.TryRemove(e.itemId, 1))
                    {
                        wallet?.Add(e.sellPrice);
                        RefreshList();
                        UpdateWalletText();
                    }
                });
            }
            else // Buy
            {
                var btn = SpawnButton();
                string label = $"{e.displayName}   购买: {e.buyPrice}";
                SetButtonLabel(btn, label);

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    if (wallet == null || !wallet.TrySpend(e.buyPrice))
                    {
                        // 金币不足
                        return;
                    }

                    if (inventoryBridge == null || !inventoryBridge.TryAdd(e.itemId, 1, null, null))
                    {
                        // 背包没加成功，退钱
                        wallet.Add(e.buyPrice);
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
