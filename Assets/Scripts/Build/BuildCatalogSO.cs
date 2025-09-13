using UnityEngine;

public enum BuildCategory { Furniture, Outdoor, Wall, Surface }
public enum BuildSnapMode { Ground, Wall, Surface }

[CreateAssetMenu(menuName = "Farm/Build Catalog", fileName = "BuildCatalogSO")]
public class BuildCatalogSO : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Header("Key")]
        public string itemId;                 // 匹配 ItemSO.id

        [Header("Prefab")]
        public GameObject prefab;

        [Header("Rules")]
        public BuildCategory category = BuildCategory.Furniture;
        public BuildSnapMode snapMode = BuildSnapMode.Ground;

        [Tooltip("允许摆放的表面层")]
        public LayerMask surfaceLayers = ~0;

        [Tooltip("网格对齐间距（Ground/Surface时适用）")]
        public float grid = 0.5f;

        [Tooltip("与命中点的高度（世界Up方向）偏移")]
        public float yOffset = 0.0f;

        [Tooltip("Y轴旋转离散步长（度），如 90 表示只能 0/90/180/270")]
        public float yawStep = 90f;

        [Header("Overlap 检测")]
        [Tooltip("半尺寸；如果为 (0,0,0) 或启用自动测量，将自动用 Prefab 的包围盒尺寸")]
        public Vector3 checkBoxSize = Vector3.zero;

        [Tooltip("是否自动从 Prefab 的 Collider/Renderer 计算包围盒半尺寸")]
        public bool autoBoundsFromPrefab = true;

        [Tooltip("自动包围盒的放大系数（避免贴边时误判）")]
        public float boundsInflation = 0.02f;

        [Tooltip("阻挡层；命中任意这些层的碰撞体即判定为不可摆放（会自动忽略自身预览与玩家）")]
        public LayerMask blockerLayers = ~0;

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
