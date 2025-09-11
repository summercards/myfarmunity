using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotSellHook : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Bind")]
    public GameObject sellBar;              // �Ƽ��� BtnSell1/BtnSellAll/Qty/BtnSellQty/Tip �ŵ�һ�������£�ͳһ����
    public Button btnSell1;
    public Button btnSellAll;
    public TMP_InputField qtyInput;
    public Button btnSellQty;
    public TextMeshProUGUI tip;

    [Header("ʶ����ƷID")]
    public string itemIdOverride = "";
    public string iconChildName = "Icon";

    // ״̬
    static InventorySlotSellHook _selected; // ��ǰ��ѡ�еĸ���
    bool _shopOpen;

    // ����
    MiniShop _shop;
    InventoryBridge _bridge;
    ShopCatalogSO _catalog;
    Image _iconImg;

    void Awake()
    {
        // �󶨰�ť
        if (btnSell1) btnSell1.onClick.AddListener(() => Sell(1));
        if (btnSellAll) btnSellAll.onClick.AddListener(SellAll);
        if (btnSellQty && qtyInput) btnSellQty.onClick.AddListener(() =>
        {
            int q = 1; int.TryParse(qtyInput.text, out q); Sell(Mathf.Max(1, q));
        });

        // ͼ��
        var t = transform.Find(iconChildName);
        if (t) _iconImg = t.GetComponent<Image>();

        // ���������̵����δ����
        _shop = FindInSceneIncludingInactive<MiniShop>();
        _bridge = FindInSceneIncludingInactive<InventoryBridge>();
        _catalog = _shop ? _shop.catalog : null;

        // ��ʼ��������
        SetSellBar(false);

        // �����̵��¼�
        MiniShop.OnActiveChanged += OnShopActiveChanged;
        _shopOpen = MiniShop.Active != null && MiniShop.Active.IsOpen;
    }

    void OnDestroy()
    {
        MiniShop.OnActiveChanged -= OnShopActiveChanged;
        if (_selected == this) _selected = null;
    }

    void OnShopActiveChanged(bool open)
    {
        _shopOpen = open;
        if (!open) SetSellBar(false);                 // �̵�ء����и�������
        else if (_selected == this) SetSellBar(true); // �̵꿪���Լ�ѡ�С���ʾ
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // �л�ѡ��
        if (_selected && _selected != this) _selected.SetSellBar(false);
        _selected = this;

        // �����̵꿪�š��Ҹø�������Ʒʱ��ʾ
        if (_shopOpen && HasSomething()) SetSellBar(true);
        else SetSellBar(false);
    }

    bool HasSomething()
    {
        string id = ResolveItemId();
        if (string.IsNullOrEmpty(id) || _bridge == null) return false;
        return _bridge.GetCount(id) > 0;
    }

    void SetSellBar(bool v)
    {
        if (sellBar) sellBar.SetActive(v);
        else
        {
            if (btnSell1) btnSell1.gameObject.SetActive(v);
            if (btnSellAll) btnSellAll.gameObject.SetActive(v);
            if (qtyInput) qtyInput.gameObject.SetActive(v);
            if (btnSellQty) btnSellQty.gameObject.SetActive(v);
            if (tip) tip.gameObject.SetActive(v);
        }
        if (!v && tip) tip.text = "";
    }

    // ===== ���� =====
    void Sell(int qty)
    {
        string itemId = ResolveItemId();
        if (string.IsNullOrEmpty(itemId)) { Tip("�޷�ʶ����Ʒ"); return; }
        if (_shop == null || _bridge == null) { Tip("����δ����"); return; }

        int have = _bridge.GetCount(itemId);
        if (have <= 0) { Tip("û�п���"); SetSellBar(false); return; }
        qty = Mathf.Clamp(qty, 1, have);

        if (!_shop.QuoteSell(itemId, qty, out int total)) { Tip("���ɳ���"); return; }

        if (!_bridge.TryRemove(itemId, qty)) { Tip("�Ƴ�ʧ��"); return; }

        if (!_shop.ConfirmSell(itemId, qty)) { Tip("����ʧ��"); return; }

        Tip($"����{qty}��+{total}");

        // �����⣬����
        if (_bridge.GetCount(itemId) <= 0) SetSellBar(false);
    }

    void SellAll()
    {
        string id = ResolveItemId();
        if (string.IsNullOrEmpty(id)) { Tip("�޷�ʶ����Ʒ"); return; }
        int have = _bridge ? _bridge.GetCount(id) : 0;
        if (have <= 0) { Tip("û�п���"); return; }
        Sell(have);
    }

    // ===== ʶ����ƷID��override �� ���� �� ͼ��ƥ�� =====
    string ResolveItemId()
    {
        if (!string.IsNullOrEmpty(itemIdOverride)) return itemIdOverride;

        // ���䣺�����ڱ� Slot ���� itemId / item / itemSO �ֶ�
        try
        {
            var comps = GetComponents<MonoBehaviour>();
            foreach (var c in comps)
            {
                if (!c) continue; var t = c.GetType();

                var fId = t.GetField("itemId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fId != null && fId.FieldType == typeof(string))
                {
                    var val = fId.GetValue(c) as string;
                    if (!string.IsNullOrEmpty(val)) return val;
                }

                var fItem = t.GetField("item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                          ?? t.GetField("itemSO", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fItem != null)
                {
                    var obj = fItem.GetValue(c) as UnityEngine.Object;
                    string id = MapObjectToId(obj);
                    if (!string.IsNullOrEmpty(id)) return id;
                }
            }
        }
        catch { }

        // ͼ����Ŀ¼ƥ��
        if (_iconImg && _iconImg.sprite && _catalog)
        {
            var sp = _iconImg.sprite;
            var e = _catalog.entries.FirstOrDefault(x => x.icon == sp);
            if (e != null) return e.itemId;
        }

        return null;
    }

    string MapObjectToId(UnityEngine.Object obj)
    {
        if (obj == null || _bridge == null) return null;
        var field = typeof(InventoryBridge).GetField("manualMapping", BindingFlags.Public | BindingFlags.Instance);
        if (field != null)
        {
            var list = field.GetValue(_bridge) as System.Collections.IEnumerable;
            if (list != null)
            {
                foreach (var it in list)
                {
                    var t = it.GetType();
                    var fObj = t.GetField("itemObject");
                    var fId = t.GetField("itemId");
                    if (fObj != null && fId != null)
                    {
                        var o = fObj.GetValue(it) as UnityEngine.Object;
                        if (o == obj) return fId.GetValue(it) as string;
                    }
                }
            }
        }
        return null;
    }

    // ����
    void Tip(string s)
    {
        if (!tip) return;
        tip.text = s;
        CancelInvoke(nameof(ClearTip));
        Invoke(nameof(ClearTip), 1.2f);
    }
    void ClearTip() { if (tip) tip.text = ""; }

    T FindInSceneIncludingInactive<T>() where T : Component
    {
        var a = Resources.FindObjectsOfTypeAll<T>();
        foreach (var x in a) if (x && x.gameObject.scene.IsValid()) return x;
        return null;
    }
}
