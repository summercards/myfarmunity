using UnityEngine;

public class CropPersistence : MonoBehaviour
{
    [Tooltip("����������ĸ���Ʒ�ֳ����ģ��� apple / banana��")]
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
