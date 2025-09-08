using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WalletHUD : MonoBehaviour
{
    [Header("引用")]
    public PlayerWallet wallet;

    [Tooltip("优先使用 TMP；如果没用 TMP，就把 UGUI Text 拖到 uguiLabel。")]
    public TextMeshProUGUI tmpLabel;
    public Text uguiLabel;

    [Header("格式")]
    public string prefix = "金币：";   // 前缀
    public bool thousandSeparator = true;   // 是否千分位

    void OnEnable()
    {
        if (wallet) wallet.onCoinsChanged.AddListener(OnCoinsChanged);
        Refresh();
    }

    void OnDisable()
    {
        if (wallet) wallet.onCoinsChanged.RemoveListener(OnCoinsChanged);
    }

    void OnCoinsChanged(int _) => Refresh();

    void Refresh()
    {
        if (!wallet)
        {
            SetText(prefix + "—");
            return;
        }
        string num = thousandSeparator ? wallet.coins.ToString("N0") : wallet.coins.ToString();
        SetText(prefix + num);
    }

    void SetText(string s)
    {
        if (tmpLabel) { tmpLabel.text = s; return; }
        if (uguiLabel) uguiLabel.text = s;
    }
}
