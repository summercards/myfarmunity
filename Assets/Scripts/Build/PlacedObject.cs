// Assets/Scripts/Build/PlacedObject.cs
using UnityEngine;

/// <summary>
/// ���ڱ��ڷų����������ϣ����ڱ���/����
/// </summary>
[DisallowMultipleComponent]
public class PlacedObject : MonoBehaviour
{
    [Tooltip("��Ӧ ItemSO.id�����ڴ浵��ԭ")]
    public string itemId;

    void OnEnable()
    {
        if (BuildSaveManager.Instance != null)
            BuildSaveManager.Instance.Register(this);
    }

    void OnDisable()
    {
        if (BuildSaveManager.Instance != null)
            BuildSaveManager.Instance.Unregister(this);
    }
}
