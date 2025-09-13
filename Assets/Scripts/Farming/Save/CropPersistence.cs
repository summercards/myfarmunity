using UnityEngine;

[DisallowMultipleComponent]
public class CropPersistence : MonoBehaviour
{
    [Tooltip("对应 SeedPlantDataSO 的 plantItemId，例如 apple / banana / banana_seed")]
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
            // 管理器可能还没初始化，下一帧再试一次
            Invoke(nameof(TryRegister), 0f);
        }
    }

    // 只在真正销毁时反注册；SetActive(false) 不会把它从保存队列里移除
    void OnDestroy()
    {
        var mgr = CropSaveManager.Instance;
        if (mgr != null) mgr.Unregister(this);
        _registered = false;
    }
}
