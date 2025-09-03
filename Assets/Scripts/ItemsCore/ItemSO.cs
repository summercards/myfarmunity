using UnityEngine;

public enum ItemCategory { Resource, Seed, Tool, Consumable, Crafted }
public enum ItemActionType { None, Eat, Plant, Place, ToolUse }

[CreateAssetMenu(menuName = "Game/Item")]
public class ItemSO : ScriptableObject
{
    public string id = "apple";
    public string displayName = "苹果";
    public Sprite icon;
    public ItemCategory category;
    public ItemActionType actionType = ItemActionType.None;

    // 手持外观（可选）
    [Header("Held (optional)")]
    public GameObject heldPrefab;        // 手里显示的模型（小模型/网格）
    public Vector3 heldLocalPosition;    // 在手骨上的局部偏移
    public Vector3 heldLocalEulerAngles; // 局部旋转
    public Vector3 heldLocalScale = Vector3.one;

    [Header("堆叠/价格")]
    public int maxStack = 99;
    public int buyPrice = 10;
    public int sellPrice = 5;

    [Header("种植（可留空）")]
    public bool plantable = false;
    public GameObject treePrefab;
    public float growSeconds = 6f;
    public float startScale = 0.4f;
    public float targetScale = 1f;
    public float interactRadius = 1.0f;
    public int yieldCount = 1;
}
