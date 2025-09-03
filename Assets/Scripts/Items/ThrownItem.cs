// Assets/Scripts/Items/ThrownItem.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class ThrownItem : MonoBehaviour
{
    public string itemId;
    public int amount = 1;

    [Header("Layers")]
    public string dynamicLayerName = "Dropped"; // 动态阶段层（和玩家不碰）
    public string pickupLayerName = "Pickup";  // 转换后层（Trigger）

    [Header("Settle")]
    public float minStationaryTime = 0.25f;
    public float stationarySpeedThreshold = 0.05f;
    public float maxLifeSeconds = 8f;
    public bool convertOnFirstCollision = false;

    [Header("Ground Snap")]
    public bool snapToGround = true;
    public float snapRayHeight = 1.0f;
    public float snapExtraOffset = 0.01f;
    public LayerMask snapMask = ~0;

    float _stillTime, _life;
    bool _collided;
    Rigidbody _rb;
    Collider _col;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();

        _col.isTrigger = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        int dyn = LayerMask.NameToLayer(dynamicLayerName);
        if (dyn >= 0)
        {
            gameObject.layer = dyn;
            foreach (Transform c in transform) c.gameObject.layer = dyn;
        }
    }

    void Update()
    {
        _life += Time.deltaTime;

        float speed = _rb.velocity.magnitude;
        if (speed < stationarySpeedThreshold) _stillTime += Time.deltaTime;
        else _stillTime = 0f;

        if ((_collided && convertOnFirstCollision) ||
            _stillTime >= minStationaryTime ||
            _life >= maxLifeSeconds)
        {
            ConvertToItemWorld();
        }
    }

    void OnCollisionEnter(Collision _) => _collided = true;

    void ConvertToItemWorld()
    {
        if (snapToGround) SnapAboveGround();

        Destroy(_rb);
        _col.isTrigger = true;

        int pick = LayerMask.NameToLayer(pickupLayerName);
        if (pick >= 0) gameObject.layer = pick;

        var iw = gameObject.AddComponent<ItemWorld>();
        iw.itemId = itemId;
        iw.amount = amount;

        Destroy(this);
    }

    void SnapAboveGround()
    {
        float half = _col.bounds.extents.y;
        Vector3 start = transform.position + Vector3.up * snapRayHeight;

        if (Physics.Raycast(start, Vector3.down, out var hit, snapRayHeight * 2f, snapMask, QueryTriggerInteraction.Ignore))
        {
            float y = hit.point.y + half + snapExtraOffset;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }
    }
}
