using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ������̵꣨�ϸ�ֱ�ӱ����棩��
/// - ����/����һ�� 1 ����
/// - ����ʱ����ͨ�� InventoryBridge ֱ��д�뱳����
///   ��δ�󶨻����ʧ�ܣ������˿��Ҳ������κ�ģ�ͣ�
/// - �����л�ģʽ���ڡ�����/���ۡ����л���
/// ������ShopCatalogSO + PlayerWallet + InventoryBridge
/// </summary>
public class SimpleShopUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject root;                 // ������壨���أ�
    public Transform listParent;            // ��Ŀ�б�����
    public Button templateButton;           // ��ťģ�壨�����з�1������Ϊ�����

    public Button toggleModeButton;         // �л�ģʽ��ť������<->���ۣ�
    public TextMeshProUGUI toggleModeLabelTMP;
    public Text toggleModeLabelUGUI;

    public Button closeButton;              // �رհ�ť����ѡ��
    public TextMeshProUGUI walletTextTMP;   // ����ı���TMP �� UGUI ��ѡһ��
    public Text walletTextUGUI;

    [Header("Data")]
    public ShopCatalogSO catalog;
    public PlayerWallet wallet;
    public InventoryBridge inventoryBridge; // ����󶨣����򲻿ɹ���/����

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
        SetLabel(toggleModeLabelTMP, toggleModeLabelUGUI, _mode == Mode.Buy ? "�е�������" : "�е�������");
    }

    void OnCoinsChanged(int _) => UpdateWalletText();

    void UpdateWalletText()
    {
        if (!wallet) { SetLabel(walletTextTMP, walletTextUGUI, "��ң���"); return; }
        SetLabel(walletTextTMP, walletTextUGUI, $"��ң�{wallet.coins}");
    }

    void RefreshList()
    {
        if (!catalog || !listParent || !templateButton) return;

        ClearList();

        // ȷ��ģ�岻����
        if (templateButton.gameObject.activeSelf) templateButton.gameObject.SetActive(false);

        foreach (var e in catalog.entries)
        {
            if (_mode == Mode.Sell)
            {
                if (!inventoryBridge) continue; // û���޷���������

                int have = inventoryBridge.GetCount(e.itemId);
                if (have <= 0) continue;

                var btn = SpawnButton();
                string label = $"{e.displayName}  x{have}  ����:{e.sellPrice}  [��1]";
                SetButtonLabel(btn, label);

                btn.onClick.AddListener(() =>
                {
                    // �ϸ�ֻ��ֱ�Ӵӱ����Ƴ�
                    if (inventoryBridge.TryRemove(e.itemId, 1))
                    {
                        wallet?.Add(e.sellPrice);
                        RefreshList();
                    }
                    else
                    {
                        Debug.Log("[Shop] ����ʧ�ܣ��Ƴ�������Ʒʧ�ܣ������κ����ɣ���");
                    }
                });
            }
            else // Buy
            {
                var btn = SpawnButton();
                string label = $"{e.displayName}  �۸�:{e.buyPrice}  [��1]";
                SetButtonLabel(btn, label);

                btn.onClick.AddListener(() =>
                {
                    if (wallet == null || !wallet.TrySpend(e.buyPrice))
                    {
                        Debug.Log("[Shop] ��Ҳ���");
                        return;
                    }

                    // �ϸ�ֱ�ӣ���ֹ�κζ�������ģ��
                    if (inventoryBridge == null)
                    {
                        Debug.Log("[Shop] ����ʧ�ܣ�δ�� InventoryBridge���޷�д�뱳�������˿");
                        wallet.Add(e.buyPrice);
                        return;
                    }

                    bool added = inventoryBridge.TryAdd(e.itemId, 1, null, null); // �� null ���ö���
                    if (!added)
                    {
                        // ���˽�ң�������ģ��
                        wallet.Add(e.buyPrice);
                        Debug.Log("[Shop] ����ʧ�ܣ�δ��д�뱳����������ģ�ͣ����˿��");
                        return;
                    }

                    // �ɹ�
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
