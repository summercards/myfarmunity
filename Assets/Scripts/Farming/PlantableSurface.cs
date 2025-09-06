using UnityEngine;

/// <summary>
/// 把它挂在“允许种植”的地面（或其父物体）上即可；
/// 不再自动要求 Collider，避免 Unity 给父物体偷偷加一个新 Collider 把地面遮住。
/// </summary>
[DisallowMultipleComponent]
public class PlantableSurface : MonoBehaviour
{
    [Header("允许哪些物品可以在这里种")]
    public bool allowAnyItem = true;
    public string[] allowedItemIds;

    [Header("地形限制")]
    [Tooltip("法线与世界Up的最大夹角（度）。过陡就不允许种")]
    public float maxSlope = 60f;

    [Header("对齐与吸附")]
    public bool alignToNormal = true;
    [Tooltip("网格吸附（0=关闭），常用 0.5 / 1.0")] public float snapGrid = 0f;
    [Tooltip("最终抬高一点避免穿模")] public float yOffset = 0.02f;

    public bool IsItemAllowed(string itemId)
    {
        if (allowAnyItem) return true;
        if (string.IsNullOrEmpty(itemId) || allowedItemIds == null) return false;
        foreach (var id in allowedItemIds) if (id == itemId) return true;
        return false;
    }

    public bool IsSlopeOK(Vector3 normal)
    {
        float ang = Vector3.Angle(normal, Vector3.up);
        return ang <= Mathf.Max(0f, maxSlope);
    }

    public Vector3 SnapPosition(Vector3 worldPos)
    {
        if (snapGrid <= 0f) return worldPos + Vector3.up * yOffset;
        float g = snapGrid;
        worldPos.x = Mathf.Round(worldPos.x / g) * g;
        worldPos.z = Mathf.Round(worldPos.z / g) * g;
        return worldPos + Vector3.up * yOffset; 
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
#endif
}
