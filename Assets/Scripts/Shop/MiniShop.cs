using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// һ�廯�̵꣺UI+��+���ضԻ�+�����Զ��ر�+���������������㣨���Ƴ���Ʒ��
/// ������ShopCatalogSO + PlayerWallet + InventoryBridge (+ ��ѡ NPCDialogUI)
///
/// ֻ��һ�����(root) + һ����������(gridParent) + һ������ģ��(cellTemplate)
/// ��������ģ����Ҫ�������壨�������Զ��ң�ȱ��Ҳ���ܣ�Ĭ������=1����
///   Icon(Image) / Name(TextMeshProUGUI) / Price(TextMeshProUGUI) /
///   Qty(TMP_InputField) / BtnBuy(Button) / [��ѡ]BtnMinus(Button) / BtnPlus(Button) / Tip(TextMeshProUGUI)
///
/// ʹ�ã�
/// 1) �ѱ��ű���������̵���� GameObject������ Panel_Shop��
/// 2) �������ֶΣ�root��gridParent��cellTemplate��catalog��wallet��inventoryBridge��[walletTextTMP/UGUI]
/// 3) NPC �� onFunction �� ָ�����ű��� OpenFromDialog()�����Զ��رնԻ���������ǰNPC��
///
/// ��������ı���UI���� QuoteSell/ConfirmSell ������
///   if (shop.QuoteSell(itemId, qty, out var total) && Inventory.Remove(id, qty)) shop.ConfirmSell(itemId, qty);
/// </summary>
public class MiniShop : MonoBehaviour
{
    [Header("Root & Layout")]
    public GameObject root;                 // �����̵���壨���أ�
    public Transform gridParent;            // �������壨����� GridLayoutGroup��
    public GameObject cellTemplate;         // ����ģ��(����״̬)

    [Header("Top")]
    public Button closeButton;              // �رհ�ť���ɿգ�
    public TextMeshProUGUI walletTextTMP;   // �����ʾ��TMP/UGUI ��ѡ��һ��
    public Text walletTextUGUI;

    [Header("Data")]
    public ShopCatalogSO catalog;
    public PlayerWallet wallet;
    public InventoryBridge inventoryBridge;

    [Header("Dialog (Optional)")]
    public NPCDialogUI dialogUI;            // ��ʱ�� Close()��������ȡ��ǰ NPC

    [Header("Auto Close By Distance")]
    public bool autoCloseWhenFar = true;
    public float closeDistance = 5f;        // �����þ���ر�
    public float autoCloseGrace = 0.5f;     // �򿪺�ı�����
    public LayerMask npcMask = ~0;          // ���ײ� NPC ��

    // ����ʱ
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

    // ====== ����ڣ��� NPC �� onFunction ֱ�ӵ��ã�======
    public void OpenFromDialog()
    {
        // 1) �رնԻ�����ȡ��ǰ NPC
        Transform fromDialog = null;
        if (dialogUI && dialogUI.IsOpen)
        {
            var curr = dialogUI.CurrentNPC;
            if (curr) fromDialog = curr.transform;
            dialogUI.Close();
        }

        // 2) ��� Transform������ PlayerInventoryHolder���� tag=Player����� MainCamera��
        var holder = FindObjectOfType<PlayerInventoryHolder>();
        player = holder ? holder.transform : null;
        if (!player)
        {
            var tagP = GameObject.FindGameObjectWithTag("Player");
            player = tagP ? tagP.transform : (Camera.main ? Camera.main.transform : null);
        }

        // 3) NPC�����ȶԻ��õ��ģ������ڰ뾶��������Ĵ� NPCInteractable ������
        npc = fromDialog ? fromDialog : FindClosestNpcWithInteractable(player, 6f);

        Open();
    }

    // ====== �������� ======
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

    // ====== ���� ======
    void TryBuy(string itemId, int unitPrice, int qty, Action<string> tip)
    {
        if (!wallet) { tip?.Invoke("��Ǯ��"); return; }
        if (!inventoryBridge) { tip?.Invoke("�ޱ���"); return; }

        qty = Mathf.Max(1, qty);
        int total = unitPrice * qty;
        if (!wallet.TrySpend(total))
        {
            tip?.Invoke("��Ҳ���");
            return;
        }

        bool ok = inventoryBridge.TryAdd(itemId, qty, null, null); // ֱ�ӱ���
        if (!ok)
        {
            wallet.Add(total); // �˿�
            tip?.Invoke("���ʧ��(����)");
            return;
        }

        UpdateWalletText();
        tip?.Invoke("����ɹ�");
    }

    // ====== �������㣨����ı���UI���ã����Ƴ���Ʒ��======
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

    // ====== �������� ======
    void BuildGrid()
    {
        ClearGrid();
        if (!catalog || !gridParent || !cellTemplate)
        {
            Debug.LogWarning("[MiniShop] ȱ�� catalog/gridParent/cellTemplate");
            return;
        }
        if (cellTemplate.activeSelf) cellTemplate.SetActive(false);

        foreach (var e in catalog.entries)
        {
            var go = Instantiate(cellTemplate, gridParent);
            go.SetActive(true);
            _spawned.Add(go);

            // �Զ�ץȡ�����
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
            if (tPrice) tPrice.text = $"���ۣ�{e.buyPrice}";
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

    // ====== ���� ======
    void OnCoinsChanged(int _) => UpdateWalletText();

    void UpdateWalletText()
    {
        string s = wallet ? $"��ң�{wallet.coins}" : "��ң���";
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
