using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

/// <summary>
/// 从“对话面板按钮”打开商店，并在**点击瞬间**切换为“商店台词”：
/// - 读取 NPCDialogUI 的 CurrentNPC（不改你原脚本，用反射获取）
/// - 把 player & npc 传给 SimpleShopUI.SetContext(...)；
/// - 调用 SimpleShopUI.Open()；
/// - 在 Open 之前先对 NPCDialogWorldBridge 调 ShowStandalone(..., shopOpenLine)，
///   确保即使随后对话面板被隐藏，头顶气泡也已切到商店文案。
/// </summary>
[DisallowMultipleComponent]
public class ShopFromDialogBridge : MonoBehaviour
{
    [Header("Refs")]
    public NPCDialogUI npcDialogUI;                 // Panel_NPCDialog 上的组件
    public NPCDialogWorldBridge dialogWorldBridge;  // 同物体上的桥接器（建议直接拖引用）
    public SimpleShopUI shopUI;                     // 商店 UI 组件
    public Transform player;                        // 玩家（可空：自动找 Tag=Player 或主相机）

    [Header("Button（可选）")]
    public Button openShopButton;                   // 若拖入，这里会自动绑定点击事件

    [Header("Shop Line")]
    [TextArea] public string shopOpenLine = "欢迎光临！需要点什么？";

    // 反射缓存：读取 CurrentNPC
    PropertyInfo _propCurrentNPC;
    FieldInfo _fieldCurrentNPC;

    void Awake()
    {
        if (!npcDialogUI) npcDialogUI = GetComponent<NPCDialogUI>();
        if (!dialogWorldBridge) dialogWorldBridge = GetComponent<NPCDialogWorldBridge>();

        var t = typeof(NPCDialogUI);
        _propCurrentNPC = t.GetProperty("CurrentNPC", BindingFlags.Public | BindingFlags.Instance);
        _fieldCurrentNPC = t.GetField("CurrentNPC", BindingFlags.Public | BindingFlags.Instance);

        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
            else if (Camera.main) player = Camera.main.transform;
        }

        if (openShopButton)
        {
            openShopButton.onClick.RemoveAllListeners();
            openShopButton.onClick.AddListener(OpenShopForCurrentNPC);
        }
    }

    /// <summary>把这个函数绑到“打开商店”按钮的 OnClick()</summary>
    public void OpenShopForCurrentNPC()
    {
        if (!shopUI || !npcDialogUI)
        {
            Debug.LogWarning("[ShopFromDialogBridge] 缺少引用：shopUI 或 npcDialogUI。");
            return;
        }

        // 1) 解析当前 NPC
        Transform npcTr = ResolveCurrentNPCTransform();
        if (!npcTr)
        {
            Debug.LogWarning("[ShopFromDialogBridge] 找不到当前 NPC Transform。");
            return;
        }

        // 2) 先切到商店台词（在面板关闭前执行，确保桥接器存在）
        var bridge = dialogWorldBridge ? dialogWorldBridge : Object.FindObjectOfType<NPCDialogWorldBridge>();
        if (bridge)
        {
            var anchor = npcTr.Find("BubbleAnchor");
            bridge.ShowStandalone(anchor ? anchor : npcTr, shopOpenLine);
        }

        // 3) 把玩家 & NPC 上下文传给商店，再打开商店
        shopUI.SetContext(player, npcTr);
        shopUI.Open();
    }

    Transform ResolveCurrentNPCTransform()
    {
        object npcObj = null;
        if (_propCurrentNPC != null) npcObj = _propCurrentNPC.GetValue(npcDialogUI);
        else if (_fieldCurrentNPC != null) npcObj = _fieldCurrentNPC.GetValue(npcDialogUI);
        if (npcObj == null) return null;

        var tNpc = npcObj.GetType();
        var pTr = tNpc.GetProperty("transform", BindingFlags.Public | BindingFlags.Instance);
        return pTr != null ? pTr.GetValue(npcObj) as Transform : null;
    }
}
