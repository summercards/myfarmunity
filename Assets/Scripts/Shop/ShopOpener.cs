using UnityEngine;

/// <summary>
/// �ѱ��ű��ҵ�һ��ȫ�����壨���� Canvas ����һ�������壩��
/// �� Inspector ��� ShopUI/ShopCatalog/InventoryBridge/PlayerWallet �Ͻ�����
/// Ȼ���� NPC �� onFunction �¼��ָ�򱾽ű��� OpenShop() ���ɡ�
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
            Debug.LogWarning("[ShopOpener] δ�� ShopUI��");
            return;
        }

        // ע������
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
