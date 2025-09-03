// Assets/Scripts/Items/ItemWorld.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemWorld : MonoBehaviour
{
    [Header("Item")]
    public string itemId = "apple";
    public int amount = 1;

    [Header("Visual")]
    public bool autoRotate = true;
    public float rotateSpeed = 90f;

    Collider _col;

    void Reset()
    {
        // Ĭ�ϰ� Collider ��ɴ�����
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
        gameObject.layer = LayerMask.NameToLayer("Pickup"); // ��û�иò㣬�Ժ���ᴴ��
    }

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col) _col.isTrigger = true;
    }

    void Update()
    {
        if (autoRotate) transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    /// <summary> ������ĳ������������ʰȡ������ʵ��ʰȡ����������С�� amount�� </summary>
    public int TryPickUp(PlayerInventoryHolder holder)
    {
        if (!holder || string.IsNullOrEmpty(itemId) || amount <= 0) return 0;
        int added = holder.AddItem(itemId, amount);
        amount -= added;
        if (amount <= 0) Destroy(gameObject);
        return added;
    }
}
