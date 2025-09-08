using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 把玩家金币持久化到 PlayerPrefs：
/// - 启动时读取；若未存档则用 Inspector 当前值（或 initialCoinsIfNoSave）并立刻写入；
/// - 每次金币变化时自动保存；
/// - 提供重置与手动保存接口；
/// 用法：挂在含有 PlayerWallet 的对象（或任意物体），把 PlayerWallet 拖进来。
/// </summary>
public class WalletPersistence : MonoBehaviour
{
    [Header("Refs")]
    public PlayerWallet wallet;          // 建议与 PlayerWallet 在同一个物体；为空会自动 GetComponent

    [Header("Save Slot")]
    [Tooltip("同一设备上可用不同存档槽。默认 default。")]
    public string saveSlot = "default";

    [Header("First Run")]
    [Tooltip("首次没有存档时的初始金币（留空则使用 wallet.coins 的 Inspector 值）。")]
    public bool overrideInitial = false;
    public int initialCoinsIfNoSave = 0;

    string Key => $"Wallet.Coins.{saveSlot}";

    void Awake()
    {
        if (!wallet) wallet = GetComponent<PlayerWallet>();
        if (!wallet)
        {
            Debug.LogError("[WalletPersistence] 未找到 PlayerWallet 引用。请在 Inspector 里把 PlayerWallet 拖进来。");
            return;
        }

        // 读取或初始化
        if (PlayerPrefs.HasKey(Key))
        {
            int saved = PlayerPrefs.GetInt(Key, 0);
            SetCoinsAndNotify(saved);
            // Debug.Log($"[WalletPersistence] Loaded coins = {saved}");
        }
        else
        {
            int init = overrideInitial ? initialCoinsIfNoSave : wallet.coins;
            PlayerPrefs.SetInt(Key, init);
            PlayerPrefs.Save();
            SetCoinsAndNotify(init); // 通知 UI 初次值
            // Debug.Log($"[WalletPersistence] First run, init coins = {init}");
        }
    }

    void OnEnable()
    {
        if (wallet) wallet.onCoinsChanged.AddListener(OnCoinsChanged);
    }

    void OnDisable()
    {
        if (wallet) wallet.onCoinsChanged.RemoveListener(OnCoinsChanged);
    }

    void OnApplicationQuit()
    {
        // 再保险一次
        if (wallet)
        {
            PlayerPrefs.SetInt(Key, wallet.coins);
            PlayerPrefs.Save();
        }
    }

    void OnCoinsChanged(int v)
    {
        PlayerPrefs.SetInt(Key, v);
        PlayerPrefs.Save();
        // Debug.Log($"[WalletPersistence] Saved coins = {v}");
    }

    void SetCoinsAndNotify(int v)
    {
        wallet.coins = v;
        // 触发 UI 刷新（UnityEvent 在未绑定监听时也安全）
        if (wallet.onCoinsChanged != null) wallet.onCoinsChanged.Invoke(v);
    }

    // ===== 手动工具 =====

    [ContextMenu("Save Now")]
    public void SaveNow()
    {
        if (!wallet) return;
        PlayerPrefs.SetInt(Key, wallet.coins);
        PlayerPrefs.Save();
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
