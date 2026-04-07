using System;
using UnityEngine;

/// <summary>
/// Wallet persistence:
/// - If SaveManager exists, wallet data is captured/restored by unified save flow.
/// - If SaveManager is absent, fallback to PlayerPrefs for standalone scenes.
/// </summary>
public class WalletPersistence : MonoBehaviour, ISaveParticipant, IPlayerSaveable
{
    [Serializable]
    private class WalletSaveData
    {
        public int coins;
    }

    [Header("Refs")]
    public PlayerWallet wallet;

    [Header("Save Slot")]
    public string saveSlot = "default";

    [Header("First Run")]
    public bool overrideInitial = false;
    public int initialCoinsIfNoSave = 0;

    [Header("Local Fallback")]
    public bool loadOnAwake = true;
    public bool saveOnCoinsChanged = true;
    public bool saveOnQuit = true;
    public bool useStandalonePrefsWhenNoSaveManager = true;

    public SaveSection Section => SaveSection.Player;
    public UnityEngine.Object Owner => this;
    public string ParticipantName => GetType().Name;

    private string Key => $"Wallet.Coins.{saveSlot}";
    private bool UseCentralSave => RuntimeRefs.SaveService != null;
    private bool _localLoadAttempted;

    void Awake()
    {
        if (!wallet) wallet = GetComponent<PlayerWallet>();
        if (!wallet)
        {
            Debug.LogError("[WalletPersistence] Missing PlayerWallet reference.");
            return;
        }
    }

    void Start()
    {
        InitializeLocalPersistenceIfNeeded();
    }

    void OnEnable()
    {
        if (wallet) wallet.onCoinsChanged.AddListener(OnCoinsChanged);
        RuntimeRefs.SaveServiceChanged += HandleSaveServiceChanged;
        RegisterToSaveService();
        InitializeLocalPersistenceIfNeeded();
    }

    void OnDisable()
    {
        if (wallet) wallet.onCoinsChanged.RemoveListener(OnCoinsChanged);
        RuntimeRefs.SaveServiceChanged -= HandleSaveServiceChanged;
        UnregisterFromSaveService();
    }

    void OnDestroy()
    {
        if (wallet) wallet.onCoinsChanged.RemoveListener(OnCoinsChanged);
    }

    void OnApplicationQuit()
    {
        if (saveOnQuit && wallet && ShouldUseLocalPrefsPersistence())
        {
            WriteToPlayerPrefs(wallet.coins);
        }
    }

    void OnCoinsChanged(int coins)
    {
        if (saveOnCoinsChanged && ShouldUseLocalPrefsPersistence())
        {
            WriteToPlayerPrefs(coins);
        }
    }

    private void LoadFromPlayerPrefsOrInit()
    {
        if (PlayerPrefs.HasKey(Key))
        {
            SetCoinsAndNotify(PlayerPrefs.GetInt(Key, 0));
            return;
        }

        int initial = overrideInitial ? initialCoinsIfNoSave : wallet.coins;
        WriteToPlayerPrefs(initial);
        SetCoinsAndNotify(initial);
    }

    private void WriteToPlayerPrefs(int coins)
    {
        PlayerPrefs.SetInt(Key, coins);
        PlayerPrefs.Save();
    }

    private void SetCoinsAndNotify(int coins)
    {
        wallet.coins = coins;
        wallet.onCoinsChanged?.Invoke(coins);
    }

    private bool ShouldUseLocalPrefsPersistence()
    {
        return useStandalonePrefsWhenNoSaveManager && !UseCentralSave;
    }

    private void HandleSaveServiceChanged(ISaveService _)
    {
        RegisterToSaveService();
        InitializeLocalPersistenceIfNeeded();
    }

    private void RegisterToSaveService()
    {
        RuntimeRefs.SaveService?.RegisterParticipant(this);
    }

    private void UnregisterFromSaveService()
    {
        RuntimeRefs.SaveService?.UnregisterParticipant(this);
    }

    private void InitializeLocalPersistenceIfNeeded()
    {
        if (_localLoadAttempted)
        {
            return;
        }

        if (loadOnAwake && ShouldUseLocalPrefsPersistence())
        {
            _localLoadAttempted = true;
            LoadFromPlayerPrefsOrInit();
        }
    }

    public object CaptureSaveData()
    {
        return wallet == null ? null : new WalletSaveData { coins = wallet.coins };
    }

    public void RestoreSaveData(string jsonData, GameTimeSystem timeSystem)
    {
        if (wallet == null || string.IsNullOrEmpty(jsonData))
        {
            return;
        }

        WalletSaveData data = JsonUtility.FromJson<WalletSaveData>(jsonData);
        if (data == null)
        {
            return;
        }

        SetCoinsAndNotify(data.coins);
    }

    public object GetSaveData()
    {
        return CaptureSaveData();
    }

    public void LoadSaveData(string jsonData, GameTimeSystem timeSystem)
    {
        RestoreSaveData(jsonData, timeSystem);
    }

    [ContextMenu("Save Now")]
    public void SaveNow()
    {
        if (!wallet) return;
        WriteToPlayerPrefs(wallet.coins);
        Debug.Log("[WalletPersistence] SaveNow()");
    }

    [ContextMenu("Reset Wallet Save (DeleteKey)")]
    public void ResetSave()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
        Debug.Log("[WalletPersistence] ResetSave()");
    }
}
