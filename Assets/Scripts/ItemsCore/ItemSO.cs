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
