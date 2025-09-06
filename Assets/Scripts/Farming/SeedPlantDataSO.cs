using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Farming/Seed Plant Data", fileName = "SeedPlantDataSO")]
public class SeedPlantDataSO : ScriptableObject
{
    [Serializable]
    public class GrowthStage
    {
        [Tooltip("该阶段持续时间（秒），<=0 则使用 Default Stage Duration")]
        public float duration = 0f;
        [Tooltip("该阶段的外观（可空）。为空则不切换外观")]
        public GameObject visual;
    }

    [Serializable]
    public class Entry
    {
        [Header("Plant from Item")]
        public string plantItemId;              // 直接用原物品ID：apple / banana
        public GameObject cropPrefab;           // 作物根预制（上面挂 CropPlant 即可）

        [Header("Growth")]
        public GrowthStage[] stages;
        [Tooltip("当 stages[x].duration <= 0 时使用此默认时长（秒）")]
        public float defaultStageDuration = 3f; // ★ 新增

        [Header("Harvest (收获)")]
        [Tooltip("成熟收获时给到玩家的物品ID；留空=plantItemId")]
        public string produceId;
        public int produceMin = 1;
        public int produceMax = 3;

        [Tooltip("生成到地面的世界掉落物预制（例如 ItemWorld_banana）。留空则退回直接加进背包。")]
        public GameObject produceWorldPrefab;   // ★ 新增

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
