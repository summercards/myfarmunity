// Assets/Scripts/Build/PlacedObject.cs
using UnityEngine;

/// <summary>
/// 挂在被摆放出来的物体上，用于保存/加载
/// </summary>
[DisallowMultipleComponent]
public class PlacedObject : MonoBehaviour
{
    [Tooltip("对应 ItemSO.id，用于存档还原")]
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
