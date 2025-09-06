using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Farming/Seed Plant Data", fileName = "SeedPlantDataSO")]
public class SeedPlantDataSO : ScriptableObject
{
    [Serializable]
    public class GrowthStage
    {
        [Tooltip("�ý׶γ���ʱ�䣨�룩��<=0 ��ʹ�� Default Stage Duration")]
        public float duration = 0f;
        [Tooltip("�ý׶ε���ۣ��ɿգ���Ϊ�����л����")]
        public GameObject visual;
    }

    [Serializable]
    public class Entry
    {
        [Header("Plant from Item")]
        public string plantItemId;              // ֱ����ԭ��ƷID��apple / banana
        public GameObject cropPrefab;           // �����Ԥ�ƣ������ CropPlant ���ɣ�

        [Header("Growth")]
        public GrowthStage[] stages;
        [Tooltip("�� stages[x].duration <= 0 ʱʹ�ô�Ĭ��ʱ�����룩")]
        public float defaultStageDuration = 3f; // �� ����

        [Header("Harvest (�ջ�)")]
        [Tooltip("�����ջ�ʱ������ҵ���ƷID������=plantItemId")]
        public string produceId;
        public int produceMin = 1;
        public int produceMax = 3;

        [Tooltip("���ɵ���������������Ԥ�ƣ����� ItemWorld_banana�����������˻�ֱ�Ӽӽ�������")]
        public GameObject produceWorldPrefab;   // �� ����

        [Header("Planting")]
        public float plantCooldown = 0.2f;
        public float spawnYOffset = 0.02f;

        public string GetFinalProduceId() =>
            string.IsNullOrEmpty(produceId) ? plantItemId : produceId;
    }

    public List<Entry> entries = new();

    public Entry GetByPlantItemId(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var e in entries) if (e != null && e.plantItemId == id) return e;
        return null;
    }
}
