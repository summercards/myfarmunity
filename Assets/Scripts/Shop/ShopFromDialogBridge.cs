using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 从对话按钮打开商店的桥接组件。
/// </summary>
[DisallowMultipleComponent]
public class ShopFromDialogBridge : MonoBehaviour
{
    [Header("Refs")]
    [Header("Behavior")]
    [Tooltip("在打开商店前是否先关闭对话框，这样气泡切换更自然")]
    public bool closeDialogWhenOpenShop = true;
    public NPCDialogUI npcDialogUI;
    public NPCDialogWorldBridge dialogWorldBridge;
    public SimpleShopUI shopUI;
    public Transform player;

    [Header("Button配置与绑定")]
    public Button openShopButton;

    [Header("Shop Line")]
    [TextArea] public string shopOpenLine = "欢迎光临，需要点什么吗？";

    void Awake()
    {
        if (!npcDialogUI) npcDialogUI = GetComponent<NPCDialogUI>();
        if (!npcDialogUI) npcDialogUI = RuntimeRefs.DialogUI;
        if (!dialogWorldBridge) dialogWorldBridge = GetComponent<NPCDialogWorldBridge>();
        if (!dialogWorldBridge) dialogWorldBridge = RuntimeRefs.DialogWorldBridge;

        if (!player)
        {
            player = RuntimeRefs.PlayerTransform;
        }

        if (openShopButton)
        {
            openShopButton.onClick.RemoveAllListeners();
            openShopButton.onClick.AddListener(OpenShopForCurrentNPC);
        }
    }

    public void OpenShopForCurrentNPC()
    {
        if (!shopUI || !npcDialogUI)
        {
            Debug.LogWarning("[ShopFromDialogBridge] 缺少引用，shopUI 或 npcDialogUI 为空");
            return;
        }

        Transform npcTr = ResolveCurrentNPCTransform();
        if (!npcTr)
        {
            Debug.LogWarning("[ShopFromDialogBridge] 无法获取当前对话 NPC Transform");
            return;
        }

        if (closeDialogWhenOpenShop && npcDialogUI != null && npcDialogUI.IsOpen)
        {
            npcDialogUI.Close();
        }

        var bridge = dialogWorldBridge ? dialogWorldBridge : RuntimeRefs.DialogWorldBridge;
        if (bridge)
        {
            var anchor = npcTr.Find("BubbleAnchor");
            bridge.ShowStandalone(anchor ? anchor : npcTr, shopOpenLine);
        }

        shopUI.Open();
    }

    Transform ResolveCurrentNPCTransform()
    {
        return npcDialogUI != null ? npcDialogUI.CurrentNPC?.SubjectTransform : null;
    }
}
