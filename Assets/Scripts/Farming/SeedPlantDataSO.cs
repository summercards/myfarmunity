using UnityEngine;

[CreateAssetMenu(menuName = "Farm/SeedPlantData", fileName = "SeedPlantDataSO")]
public class SeedPlantDataSO : ScriptableObject
{
    [System.Serializable]
    public class Stage
    {
        public float duration = 0f;     // �ý׶�ʱ����<=0 ʹ�� defaultStageDuration��
        public GameObject visual;       // �׶�չʾģ�ͣ��ɿգ����������Ų��䣩
    }

    [System.Serializable]
    public class Entry
    {
        [Header("Plant from Item")]
        public string plantItemId;          // �������Ʒ����
        public GameObject cropPrefab;       // ���ɵ�����Ԥ����

        [Header("Growth")]
        public float defaultStageDuration = 4f;
        public Stage[] stages;

        [Header("Harvest (һ����/��ͨ��)")]
        public string produceId;            // ������� plantItemId
        public int produceMin = 1;
        public int produceMax = 1;
        public GameObject produceWorldPrefab; // ���䵽�����Ԥ���壨������� apple/banana world Ԥ���壩

        [Header("Planting")]
        public float plantCooldown = 0.2f;
        public float spawnYOffset = 0.02f;

        [Header("Tree / Persistent")]
        public bool keepAfterHarvest = false;     // ��ѡ=���������ճɺ�����

        [Header("Periodic Produce (����ѡ)")]
        public bool periodicProduce = false;      // ��ѡ=�����Բ���
        public float produceInterval = 10f;       // ����������룩
        public int producePerTick = 1;            // ÿ�β�������
        public int maxOnGround = 3;               // �������ѻ�����������
        public float dropRadius = 0.5f;           // ����뾶��������Χ�����
    }

    public Entry[] entries;

    public Entry GetByPlantItemId(string id)
    {
        if (entries == null) return null;
        for (int i = 0; i < entries.Length; i++)
            if (entries[i] != null && entries[i].plantItemId == id) return entries[i];
        return null;
    }
}
