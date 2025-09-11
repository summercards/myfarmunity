using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ̵ֱ꣨ӱ + Զرգ
/// - /һ 1 ͨ InventoryBridge ֱд/Ƴ
/// - 򿪺һΡڡĬ 0.2sжһ
/// - ͨ SetContext(player,npc) ע NPCԶرգ
/// - δע npc ʱرա
/// ShopCatalogSO + PlayerWallet + InventoryBridge
/// </summary>
public class SimpleShopUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject root;                 // 壨أ
    public Transform listParent;            // Ŀб
    public Button templateButton;           // ťģ壨1Ϊ

    public Button toggleModeButton;         // лģʽť<->ۣ
    public TextMeshProUGUI toggleModeLabelTMP;
    public Text toggleModeLabelUGUI;

    public Button closeButton;              // رհťѡ
    public TextMeshProUGUI walletTextTMP;   // ıTMP  UGUI ѡһ
    public Text walletTextUGUI;

    [Header("Data")]
    public ShopCatalogSO catalog;
    public PlayerWallet wallet;
    public InventoryBridge inventoryBridge; // 󶨣򲻿ɹ/

    [Header("Auto Close By Distance")]
    public bool autoCloseWhenFar = true;
    [Tooltip("þԶر̵ꡣ 4~5")]
    public float closeDistance = 4.0f;
    [Tooltip("򿪺ʱڲжϣһ")]
    public float autoCloseGrace = 0.2f;

    [Tooltip(" Transformջ Open() ʱԶ tag=Player Ѱң")]
    public Transform player;
    [Tooltip("ǰ NPC Transform Opener  Open() ǰע룬жϣ")]
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

        // ڣһ򿪾Ϊ΢жر
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

// —— 商店台词：在NPC头顶播放一句（桥接器负责显示到3D气泡） —— 
var bridge = Object.FindObjectOfType<NPCDialogWorldBridge>();
if (bridge && npc)
{
    var anchor = npc.Find("BubbleAnchor");
    bridge.ShowStandalone(anchor ? anchor : npc, "欢迎光临！需要点什么？");
}

    }

    public void Close()
    {
        IsOpen = false;
        if (root) root.SetActive(false);
        ClearList();

// —— 结束商店台词 —— 
var bridge2 = Object.FindObjectOfType<NPCDialogWorldBridge>();
if (bridge2) bridge2.EndStandalone();

    }

    void ToggleMode()
    {
        SetMode(_mode == Mode.Buy ? Mode.Sell : Mode.Buy);
        RefreshList();
    }

    void SetMode(Mode m)
    {
        _mode = m;
        SetLabel(toggleModeLabelTMP, toggleModeLabelUGUI, _mode == Mode.Buy ? "е" : "е");
    }

    void OnCoinsChanged(int _) => UpdateWalletText();

    void UpdateWalletText()
    {
        if (!wallet) { SetLabel(walletTextTMP, walletTextUGUI, "ң"); return; }
        SetLabel(walletTextTMP, walletTextUGUI, $"ң{wallet.coins}");
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
                string label = $"{e.displayName}  x{have}  :{e.sellPrice}  [1]";
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
                        Debug.Log("[Shop] ʧܣƳƷʧ");
                    }
                });
            }
            else // Buy
            {
                var btn = SpawnButton();
                string label = $"{e.displayName}  ۸:{e.buyPrice}  [1]";
                SetButtonLabel(btn, label);

                btn.onClick.AddListener(() =>
                {
                    if (wallet == null || !wallet.TrySpend(e.buyPrice))
                    {
                        Debug.Log("[Shop] Ҳ");
                        return;
                    }

                    if (inventoryBridge == null)
                    {
                        Debug.Log("[Shop] ʧܣδ InventoryBridge޷д뱳˿");
                        wallet.Add(e.buyPrice);
                        return;
                    }

                    bool added = inventoryBridge.TryAdd(e.itemId, 1, null, null); // ֱӱö
                    if (!added)
                    {
                        wallet.Add(e.buyPrice); // 
                        Debug.Log("[Shop] ʧܣδд뱳˿");
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
