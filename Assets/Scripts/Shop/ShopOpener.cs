using UnityEngine;

/// <summary>
/// 把本脚本挂到一个全局物体（例如 Canvas 根或一个空物体）。
/// 在 Inspector 里把 ShopUI/ShopCatalog/InventoryBridge/PlayerWallet 拖进来。
/// 然后在 NPC 的 onFunction 事件里，指向本脚本的 OpenShop() 即可。
/// </summary>
public class ShopOpener : MonoBehaviour
{
    public ShopUI shopUI;
    public ShopCatalogSO defaultCatalog;
    public InventoryBridge inventoryBridge;
    public PlayerWallet wallet;

    public void OpenShop()
    {
        if (!shopUI)
        {
            Debug.LogWarning("[ShopOpener] 未绑定 ShopUI。");
            return;
        }

        // 注入依赖
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
