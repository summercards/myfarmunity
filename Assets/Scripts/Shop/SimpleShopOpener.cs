using UnityEngine;

/// <summary>
/// 把它挂到任意物体（Canvas 根或一个空物体）。
/// 在 NPC 的 onFunction 事件里，指向 OpenShop() 即可弹出商店。
/// </summary>
public class SimpleShopOpener : MonoBehaviour
{
    public SimpleShopUI shopUI;
    public ShopCatalogSO defaultCatalog;
    public InventoryBridge inventoryBridge;
    public PlayerWallet wallet;

    public void OpenShop()
    {
        if (!shopUI)
        {
            Debug.LogWarning("[SimpleShopOpener] 未绑定 SimpleShopUI");
            return;
        }
        if (defaultCatalog) shopUI.catalog = defaultCatalog;
        if (inventoryBridge) shopUI.inventoryBridge = inventoryBridge;
        if (wallet) shopUI.wallet = wallet;

        shopUI.Open();
    }

    public void CloseShop()
    {
        if (shopUI) shopUI.Close();
    }
}
