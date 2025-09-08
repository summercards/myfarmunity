using UnityEngine;

/// <summary>
/// �����ҵ��������壨Canvas ����һ�������壩��
/// �� NPC �� onFunction �¼��ָ�� OpenShop() ���ɵ����̵ꡣ
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
            Debug.LogWarning("[SimpleShopOpener] δ�� SimpleShopUI");
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
