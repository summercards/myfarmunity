using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WalletHUD : MonoBehaviour
{
    [Header("����")]
    public PlayerWallet wallet;

    [Tooltip("����ʹ�� TMP�����û�� TMP���Ͱ� UGUI Text �ϵ� uguiLabel��")]
    public TextMeshProUGUI tmpLabel;
    public Text uguiLabel;

    [Header("��ʽ")]
    public string prefix = "��ң�";   // ǰ׺
    public bool thousandSeparator = true;   // �Ƿ�ǧ��λ

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
            SetText(prefix + "��");
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
