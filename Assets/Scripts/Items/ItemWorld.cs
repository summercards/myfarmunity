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
        // 默认把 Collider 设成触发器
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
        gameObject.layer = LayerMask.NameToLayer("Pickup"); // 若没有该层，稍后你会创建
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

    /// <summary> 尝试由某个背包持有者拾取，返回实际拾取数量（可能小于 amount） </summary>
    public int TryPickUp(PlayerInventoryHolder holder)
    {
        if (!holder || string.IsNullOrEmpty(itemId) || amount <= 0) return 0;
        int added = holder.AddItem(itemId, amount);
        amount -= added;
        if (amount <= 0) Destroy(gameObject);
        return added;
    }
}
