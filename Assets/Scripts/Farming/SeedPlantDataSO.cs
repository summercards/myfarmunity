using UnityEngine;

[CreateAssetMenu(menuName = "Farm/SeedPlantData", fileName = "SeedPlantDataSO")]
public class SeedPlantDataSO : ScriptableObject
{
    [System.Serializable]
    public class Stage
    {
        public float duration = 0f;     // 该阶段时长（<=0 使用 defaultStageDuration）
        public GameObject visual;       // 阶段展示模型（可空，空则用缩放补间）
    }

    [System.Serializable]
    public class Entry
    {
        [Header("Plant from Item")]
        public string plantItemId;          // 用这个物品能种
        public GameObject cropPrefab;       // 生成的作物预制体

        [Header("Growth")]
        public float defaultStageDuration = 4f;
        public Stage[] stages;

        [Header("Harvest (一次性/树通用)")]
        public string produceId;            // 不填就用 plantItemId
        public int produceMin = 1;
        public int produceMax = 1;
        public GameObject produceWorldPrefab; // 掉落到地面的预制体（建议你的 apple/banana world 预制体）

        [Header("Planting")]
        public float plantCooldown = 0.2f;
        public float spawnYOffset = 0.02f;

        [Header("Tree / Persistent")]
        public bool keepAfterHarvest = false;     // 勾选=“树”→收成后不销毁

        [Header("Periodic Produce (树可选)")]
        public bool periodicProduce = false;      // 勾选=周期性产出
        public float produceInterval = 10f;       // 产出间隔（秒）
        public int producePerTick = 1;            // 每次产出几个
        public int maxOnGround = 3;               // 脚下最多堆积（简单限流）
        public float dropRadius = 0.5f;           // 掉落半径（树脚周围随机）
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
