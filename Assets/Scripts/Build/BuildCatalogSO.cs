// Assets/Scripts/Build/BuildCatalogSO.cs
using UnityEngine;

public enum BuildCategory { Furniture, Outdoor, Wall, Surface }   // 简化的摆放分类
public enum BuildSnapMode { Ground, Wall, Surface }               // 对应射线命中对齐方式

[CreateAssetMenu(menuName = "Farm/Build Catalog", fileName = "BuildCatalogSO")]
public class BuildCatalogSO : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Header("Key")]
        public string itemId;                // 对应 ItemSO.id（手持物品ID）

        [Header("Prefab")]
        public GameObject prefab;            // 实际摆放的预制体（运行时实例化）

        [Header("Rules")]
        public BuildCategory category = BuildCategory.Furniture;
        public BuildSnapMode snapMode = BuildSnapMode.Ground;

        [Tooltip("允许摆放的表面层")]
        public LayerMask surfaceLayers = ~0;

        [Tooltip("网格对齐间距（Ground/Surface时适用）")]
        public float grid = 0.5f;

        [Tooltip("与命中点的高度（世界Up方向）偏移")]
        public float yOffset = 0.0f;

        [Tooltip("Y轴旋转离散步长（度），例如 90 表示只能 0/90/180/270 度")]
        public float yawStep = 90f;

        [Header("Overlap 检测")]
        public Vector3 checkBoxSize = new Vector3(0.5f, 0.5f, 0.5f);    // 半尺寸
        public LayerMask blockerLayers = ~0;                             // 碰撞阻挡层（检测失败即不可放置）

        [Header("可选：落地时自动对齐到地面（比如家具脚部落地）")]
        public bool alignToGroundAfterPlace = true;
        public float groundProbeDistance = 3f;

        [Header("可选：实例化后的父节点（为空则放到场景根）")]
        public Transform optionalParentAtRuntime;
    }

    public Entry[] entries;

    public Entry Get(string itemId)
    {
        if (entries == null) return null;
        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e != null && !string.IsNullOrEmpty(e.itemId) && e.itemId == itemId)
                return e;
        }
        return null;
    }
}
