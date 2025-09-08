using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// ����ҽ�ҳ־û��� PlayerPrefs��
/// - ����ʱ��ȡ����δ�浵���� Inspector ��ǰֵ���� initialCoinsIfNoSave��������д�룻
/// - ÿ�ν�ұ仯ʱ�Զ����棻
/// - �ṩ�������ֶ�����ӿڣ�
/// �÷������ں��� PlayerWallet �Ķ��󣨻��������壩���� PlayerWallet �Ͻ�����
/// </summary>
public class WalletPersistence : MonoBehaviour
{
    [Header("Refs")]
    public PlayerWallet wallet;          // ������ PlayerWallet ��ͬһ�����壻Ϊ�ջ��Զ� GetComponent

    [Header("Save Slot")]
    [Tooltip("ͬһ�豸�Ͽ��ò�ͬ�浵�ۡ�Ĭ�� default��")]
    public string saveSlot = "default";

    [Header("First Run")]
    [Tooltip("�״�û�д浵ʱ�ĳ�ʼ��ң�������ʹ�� wallet.coins �� Inspector ֵ����")]
    public bool overrideInitial = false;
    public int initialCoinsIfNoSave = 0;

    string Key => $"Wallet.Coins.{saveSlot}";

    void Awake()
    {
        if (!wallet) wallet = GetComponent<PlayerWallet>();
        if (!wallet)
        {
            Debug.LogError("[WalletPersistence] δ�ҵ� PlayerWallet ���á����� Inspector ��� PlayerWallet �Ͻ�����");
            return;
        }

        // ��ȡ���ʼ��
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
            SetCoinsAndNotify(init); // ֪ͨ UI ����ֵ
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
        // �ٱ���һ��
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
        // ���� UI ˢ�£�UnityEvent ��δ�󶨼���ʱҲ��ȫ��
        if (wallet.onCoinsChanged != null) wallet.onCoinsChanged.Invoke(v);
    }

    // ===== �ֶ����� =====

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
