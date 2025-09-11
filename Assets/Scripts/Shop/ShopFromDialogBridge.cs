using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

/// <summary>
/// 把“NPC 对话面板里的【打开商店】按钮”与 SimpleShopUI 连接起来：
/// - 从 NPCDialogUI 里取到 CurrentNPC（反射，不改你原文件）
/// - 传给 SimpleShopUI.SetContext(player, npc)
/// - 调用 SimpleShopUI.Open()（它内部会播“商店台词”，我们只保证 npc 传对）
/// </summary>
[DisallowMultipleComponent]
public class ShopFromDialogBridge : MonoBehaviour
{
    [Header("Refs")]
    public NPCDialogUI npcDialogUI;       // 对话面板（Panel_NPCDialog 上的那个）
    public SimpleShopUI shopUI;           // 你的商店 UI 根组件
    public Transform player;              // 玩家（可空：为空时自动找 Player tag 或主相机）

    [Header("Button (可选)")]
    public Button openShopButton;         // 如果你想直接在这里绑定按钮，也可以把按钮拖进来

    // 反射缓存
    PropertyInfo _propCurrentNPC;
    FieldInfo _fieldCurrentNPC;

    void Awake()
    {
        if (!npcDialogUI) npcDialogUI = GetComponent<NPCDialogUI>();
        var t = typeof(NPCDialogUI);
        _propCurrentNPC = t.GetProperty("CurrentNPC", BindingFlags.Public | BindingFlags.Instance);
        _fieldCurrentNPC = t.GetField("CurrentNPC", BindingFlags.Public | BindingFlags.Instance);

        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
            else if (Camera.main) player = Camera.main.transform;
        }

        // 可选：如果把按钮拖进来了，这里顺便帮你绑好
        if (openShopButton)
        {
            openShopButton.onClick.RemoveAllListeners();
            openShopButton.onClick.AddListener(OpenShopForCurrentNPC);
        }
    }

    /// <summary>在按钮的 OnClick() 里调用这个方法即可。</summary>
    public void OpenShopForCurrentNPC()
    {
        if (!shopUI || !npcDialogUI) return;

        // 1) 取到当前 NPC 的 Transform
        Transform npcTransform = ResolveCurrentNPCTransform();
        if (!npcTransform)
        {
            Debug.LogWarning("[ShopFromDialogBridge] 找不到当前 NPC 的 Transform，无法打开商店。");
            return;
        }

        // 2) 给商店传上下文（玩家 & NPC）
        shopUI.SetContext(player, npcTransform);

        // 3) 打开商店（SimpleShopUI.Open 内部会自动播“商店台词”）
        shopUI.Open();

        // 4) 可选：如果你希望打开商店时把对话面板也关掉：
        // var rootField = typeof(NPCDialogUI).GetField("root", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        // var rootGO = rootField != null ? (GameObject)rootField.GetValue(npcDialogUI) : null;
        // if (rootGO) rootGO.SetActive(false);
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
