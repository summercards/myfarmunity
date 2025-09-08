using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Shop Catalog", fileName = "SC_DefaultCatalog")]
public class ShopCatalogSO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("��ƷΨһID�����㱳���ڵ�ID����һ�£����Զ���һ���̶�ID����")]
        public string itemId;

        [Tooltip("չʾ������")]
        public string displayName;

        [Tooltip("ͼ��")]
        public Sprite icon;

        [Header("�۸񣨵�λ����ң�")]
        public int buyPrice = 10;
        public int sellPrice = 5;

        [Header("���ף����޷�ֱ����ӵ����������������ʰȡԤ�������Ҽ�")]
        public GameObject pickupPrefab;
    }

    public List<Entry> entries = new List<Entry>();

    public Entry Get(string id)
    {
        return entries.Find(e => e.itemId == id);
    }
}
