using UnityEngine;

[DisallowMultipleComponent]
public class CropPersistence : MonoBehaviour
{
    [Tooltip("��Ӧ SeedPlantDataSO �� plantItemId������ apple / banana / banana_seed")]
    public string entryId;

    bool _registered;

    void Awake() { TryRegister(); }
    void OnEnable() { TryRegister(); }
    void Start() { TryRegister(); }

    void TryRegister()
    {
        if (_registered) return;
        var mgr = CropSaveManager.Instance;
        if (mgr != null)
        {
            mgr.Register(this);
            _registered = true;
        }
        else
        {
            // ���������ܻ�û��ʼ������һ֡����һ��
            Invoke(nameof(TryRegister), 0f);
        }
    }

    // ֻ����������ʱ��ע�᣻SetActive(false) ��������ӱ���������Ƴ�
    void OnDestroy()
    {
        var mgr = CropSaveManager.Instance;
        if (mgr != null) mgr.Unregister(this);
        _registered = false;
    }
}
