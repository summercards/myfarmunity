using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Shop Catalog", fileName = "SC_DefaultCatalog")]
public class ShopCatalogSO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("物品唯一ID（和你背包内的ID保持一致，或自定义一个固定ID）。")]
        public string itemId;

        [Tooltip("展示用名称")]
        public string displayName;

        [Tooltip("图标")]
        public Sprite icon;

        [Header("价格（单位：金币）")]
        public int buyPrice = 10;
        public int sellPrice = 5;

        [Header("兜底：若无法直接添加到背包，则生成这个拾取预制体给玩家捡")]
        public GameObject pickupPrefab;
    }

    public List<Entry> entries = new List<Entry>();

    public Entry Get(string id)
    {
        return entries.Find(e => e.itemId == id);
    }
}
