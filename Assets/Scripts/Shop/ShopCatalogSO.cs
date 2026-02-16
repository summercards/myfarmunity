using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 商店目录 - 包含所有可买卖的商品
/// </summary>
[CreateAssetMenu(menuName = "Game/Shop Catalog", fileName = "SC_DefaultCatalog")]
public class ShopCatalogSO : ScriptableObject
{
    [Serializable]
    public class ShopEntrySO
    {
        [Tooltip("商品唯一ID")]
        public string itemId;

        [Tooltip("商品显示名称")]
        public string displayName;

        [Tooltip("商品图标")]
        public Sprite icon;

        [Header("价格")]
        public int buyPrice = 10;
        public int sellPrice = 5;

        [Tooltip("拾取预制体（可选）")]
        public GameObject pickupPrefab;
    }

    [Tooltip("商品列表")]
    public List<ShopEntrySO> entries = new List<ShopEntrySO>();

    /// <summary> 根据ID获取商品</summary>
    public ShopEntrySO Get(string id)
    {
        return entries.Find(e => e.itemId == id);
    }
}
