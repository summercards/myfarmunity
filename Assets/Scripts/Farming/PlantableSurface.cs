using UnityEngine;

/// <summary>
/// �������ڡ�������ֲ���ĵ��棨���丸���壩�ϼ��ɣ�
/// �����Զ�Ҫ�� Collider������ Unity ��������͵͵��һ���� Collider �ѵ�����ס��
/// </summary>
[DisallowMultipleComponent]
public class PlantableSurface : MonoBehaviour
{
    [Header("������Щ��Ʒ������������")]
    public bool allowAnyItem = true;
    public string[] allowedItemIds;

    [Header("��������")]
    [Tooltip("����������Up�����нǣ��ȣ��������Ͳ�������")]
    public float maxSlope = 60f;

    [Header("����������")]
    public bool alignToNormal = true;
    [Tooltip("����������0=�رգ������� 0.5 / 1.0")] public float snapGrid = 0f;
    [Tooltip("����̧��һ����⴩ģ")] public float yOffset = 0.02f;

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
