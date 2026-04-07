using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotSellHook : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Bind")]
    public GameObject sellBar;
    public Button btnSell1;
    public Button btnSellAll;
    public TMP_InputField qtyInput;
    public Button btnSellQty;
    public TextMeshProUGUI tip;

    [Header("Item Resolve")]
    public string itemIdOverride = "";
    public string iconChildName = "Icon";

    [Header("Runtime Refs (Optional)")]
    public MiniShop shopOverride;
    public InventoryBridge bridgeOverride;

    private static InventorySlotSellHook selected;
    private bool isShopOpen;

    private MiniShop shop;
    private InventoryBridge bridge;
    private ShopCatalogSO catalog;
    private Image iconImage;

    void Awake()
    {
        if (btnSell1 != null)
        {
            btnSell1.onClick.AddListener(() => Sell(1));
        }

        if (btnSellAll != null)
        {
            btnSellAll.onClick.AddListener(SellAll);
        }

        if (btnSellQty != null && qtyInput != null)
        {
            btnSellQty.onClick.AddListener(() =>
            {
                int quantity = 1;
                int.TryParse(qtyInput.text, out quantity);
                Sell(Mathf.Max(1, quantity));
            });
        }

        Transform iconTransform = transform.Find(iconChildName);
        if (iconTransform != null)
        {
            iconImage = iconTransform.GetComponent<Image>();
        }

        ResolveShopRefs();
        SetSellBar(false);
        isShopOpen = MiniShop.Active != null && MiniShop.Active.IsOpen;
    }

    void OnEnable()
    {
        MiniShop.OnActiveChanged += OnShopActiveChanged;
        RuntimeRefs.MiniShopUIChanged += HandleMiniShopChanged;
        RuntimeRefs.InventoryBridgeChanged += HandleInventoryBridgeChanged;

        ResolveShopRefs();
        isShopOpen = MiniShop.Active != null && MiniShop.Active.IsOpen;
    }

    void OnDisable()
    {
        MiniShop.OnActiveChanged -= OnShopActiveChanged;
        RuntimeRefs.MiniShopUIChanged -= HandleMiniShopChanged;
        RuntimeRefs.InventoryBridgeChanged -= HandleInventoryBridgeChanged;
    }

    void OnDestroy()
    {
        if (selected == this)
        {
            selected = null;
        }
    }

    private void HandleMiniShopChanged(MiniShop miniShop)
    {
        if (shopOverride != null)
        {
            return;
        }

        shop = miniShop;
        catalog = shop != null ? shop.catalog : null;
    }

    private void HandleInventoryBridgeChanged(InventoryBridge inventoryBridge)
    {
        if (bridgeOverride != null)
        {
            return;
        }

        bridge = inventoryBridge;
    }

    private void ResolveShopRefs()
    {
        shop = shopOverride != null ? shopOverride : RuntimeRefs.MiniShopUI;
        bridge = bridgeOverride != null ? bridgeOverride : RuntimeRefs.InventoryBridge;
        catalog = shop != null ? shop.catalog : null;
    }

    void OnShopActiveChanged(bool open)
    {
        isShopOpen = open;
        if (!open)
        {
            SetSellBar(false);
        }
        else if (selected == this)
        {
            SetSellBar(true);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (selected != null && selected != this)
        {
            selected.SetSellBar(false);
        }

        selected = this;

        if (isShopOpen && HasSomething())
        {
            SetSellBar(true);
        }
        else
        {
            SetSellBar(false);
        }
    }

    private bool HasSomething()
    {
        string id = ResolveItemId();
        if (string.IsNullOrEmpty(id) || bridge == null)
        {
            return false;
        }

        return bridge.GetCount(id) > 0;
    }

    private void SetSellBar(bool visible)
    {
        if (sellBar != null)
        {
            sellBar.SetActive(visible);
        }
        else
        {
            if (btnSell1 != null) btnSell1.gameObject.SetActive(visible);
            if (btnSellAll != null) btnSellAll.gameObject.SetActive(visible);
            if (qtyInput != null) qtyInput.gameObject.SetActive(visible);
            if (btnSellQty != null) btnSellQty.gameObject.SetActive(visible);
            if (tip != null) tip.gameObject.SetActive(visible);
        }

        if (!visible && tip != null)
        {
            tip.text = "";
        }
    }

    private void Sell(int quantity)
    {
        string itemId = ResolveItemId();
        if (string.IsNullOrEmpty(itemId))
        {
            Tip("无法识别物品");
            return;
        }

        if (shop == null || bridge == null)
        {
            Tip("商店未就绪");
            return;
        }

        int owned = bridge.GetCount(itemId);
        if (owned <= 0)
        {
            Tip("没有可卖");
            SetSellBar(false);
            return;
        }

        quantity = Mathf.Clamp(quantity, 1, owned);

        if (!shop.QuoteSell(itemId, quantity, out int total))
        {
            Tip("不可出售");
            return;
        }

        if (!bridge.TryRemove(itemId, quantity))
        {
            Tip("移除失败");
            return;
        }

        if (!shop.ConfirmSell(itemId, quantity))
        {
            Tip("结算失败");
            return;
        }

        Tip($"已卖{quantity}，+{total}");

        if (bridge.GetCount(itemId) <= 0)
        {
            SetSellBar(false);
        }
    }

    private void SellAll()
    {
        string id = ResolveItemId();
        if (string.IsNullOrEmpty(id))
        {
            Tip("无法识别物品");
            return;
        }

        int owned = bridge != null ? bridge.GetCount(id) : 0;
        if (owned <= 0)
        {
            Tip("没有可卖");
            return;
        }

        Sell(owned);
    }

    private string ResolveItemId()
    {
        if (!string.IsNullOrEmpty(itemIdOverride))
        {
            return itemIdOverride;
        }

        try
        {
            MonoBehaviour[] components = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                if (component == null)
                {
                    continue;
                }

                var type = component.GetType();

                var itemIdField = type.GetField("itemId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (itemIdField != null && itemIdField.FieldType == typeof(string))
                {
                    var value = itemIdField.GetValue(component) as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        return value;
                    }
                }

                var itemField = type.GetField("item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? type.GetField("itemSO", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (itemField != null)
                {
                    var obj = itemField.GetValue(component) as Object;
                    string mappedId = MapObjectToId(obj);
                    if (!string.IsNullOrEmpty(mappedId))
                    {
                        return mappedId;
                    }
                }
            }
        }
        catch
        {
            // Keep fail-safe behavior for mixed slot implementations.
        }

        if (iconImage != null && iconImage.sprite != null && catalog != null)
        {
            Sprite sprite = iconImage.sprite;
            var entry = catalog.entries.FirstOrDefault(x => x.icon == sprite);
            if (entry != null)
            {
                return entry.itemId;
            }
        }

        return null;
    }

    private string MapObjectToId(Object obj)
    {
        if (obj == null || bridge == null)
        {
            return null;
        }

        var field = typeof(InventoryBridge).GetField("manualMapping", BindingFlags.Public | BindingFlags.Instance);
        if (field == null)
        {
            return null;
        }

        var list = field.GetValue(bridge) as System.Collections.IEnumerable;
        if (list == null)
        {
            return null;
        }

        foreach (var item in list)
        {
            var type = item.GetType();
            var objectField = type.GetField("itemObject");
            var idField = type.GetField("itemId");
            if (objectField == null || idField == null)
            {
                continue;
            }

            var mappedObject = objectField.GetValue(item) as Object;
            if (mappedObject == obj)
            {
                return idField.GetValue(item) as string;
            }
        }

        return null;
    }

    private void Tip(string message)
    {
        if (tip == null)
        {
            return;
        }

        tip.text = message;
        CancelInvoke(nameof(ClearTip));
        Invoke(nameof(ClearTip), 1.2f);
    }

    private void ClearTip()
    {
        if (tip != null)
        {
            tip.text = "";
        }
    }
}
