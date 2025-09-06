using UnityEngine;

public class CropPersistence : MonoBehaviour
{
    [Tooltip("这棵作物是哪个物品种出来的（如 apple / banana）")]
    public string entryId;

    void OnEnable()
    {
        if (CropSaveManager.Instance != null)
            CropSaveManager.Instance.Register(this);
    }

    void OnDisable()
    {
        if (CropSaveManager.Instance != null)
            CropSaveManager.Instance.Unregister(this);
    }
}
