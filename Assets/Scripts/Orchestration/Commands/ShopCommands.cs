using UnityEngine;

namespace FarmGame.Orchestration
{
    public readonly struct BuyShopItemCommand
    {
        public ShopCatalogSO Catalog { get; }
        public string ItemId { get; }
        public int Quantity { get; }
        public Transform PlayerTransform { get; }

        public BuyShopItemCommand(ShopCatalogSO catalog, string itemId, int quantity, Transform playerTransform = null)
        {
            Catalog = catalog;
            ItemId = itemId;
            Quantity = quantity;
            PlayerTransform = playerTransform;
        }
    }
}
