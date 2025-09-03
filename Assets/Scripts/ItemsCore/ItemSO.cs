using UnityEngine;

public enum ItemCategory { Resource, Seed, Tool, Consumable, Crafted }
public enum ItemActionType { None, Eat, Plant, Place, ToolUse }

[CreateAssetMenu(menuName = "Game/Item")]
public class ItemSO : ScriptableObject
{
    public string id = "apple";
    public string displayName = "ƻ��";
    public Sprite icon;
    public ItemCategory category;
    public ItemActionType actionType = ItemActionType.None;

    // �ֳ���ۣ���ѡ��
    [Header("Held (optional)")]
    public GameObject heldPrefab;        // ������ʾ��ģ�ͣ�Сģ��/����
    public Vector3 heldLocalPosition;    // ���ֹ��ϵľֲ�ƫ��
    public Vector3 heldLocalEulerAngles; // �ֲ���ת
    public Vector3 heldLocalScale = Vector3.one;

    [Header("�ѵ�/�۸�")]
    public int maxStack = 99;
    public int buyPrice = 10;
    public int sellPrice = 5;

    [Header("��ֲ�������գ�")]
    public bool plantable = false;
    public GameObject treePrefab;
    public float growSeconds = 6f;
    public float startScale = 0.4f;
    public float targetScale = 1f;
    public float interactRadius = 1.0f;
    public int yieldCount = 1;
}
