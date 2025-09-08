using UnityEngine;

/// <summary>
/// 打开商店：
/// - 优先沿用 SimpleShopUI 上已经手动绑定的 player / npc（不覆盖）；
/// - 如果没绑定 npc，优先从 NPCDialogUI.CurrentNPC 获取，并 Close() 对话；
/// - 还找不到时才在半径内搜索含 NPCInteractable 的最近物体；
/// - 玩家 Transform 智能查找：显式指定 > SimpleShopUI已绑定 > PlayerInventoryHolder > tag=Player > MainCamera；
/// - 依赖注入 catalog / bridge / wallet。
/// </summary>
public class SimpleShopOpener : MonoBehaviour
{
    [Header("Refs")]
    public SimpleShopUI shopUI;
    public ShopCatalogSO defaultCatalog;
    public InventoryBridge inventoryBridge;
    public PlayerWallet wallet;

    [Header("Dialog (Optional)")]
    [Tooltip("场景中的 NPCDialogUI；建议拖与 PlayerInteractor.sharedDialogUI 相同的那一个。")]
    public NPCDialogUI dialogUI;

    [Header("Context (Optional)")]
    [Tooltip("显式指定玩家 Transform（优先级最高）；通常可留空。")]
    public Transform player;
    [Tooltip("显式指定 NPC（如果你想固定某个NPC）。")]
    public Transform overrideNPC;

    [Header("Fallback Search")]
    [Tooltip("当无法从对话面板获得 NPC 时，在该半径内搜索最近的 NPCInteractable。")]
    public float findNpcRadius = 6.0f;
    [Tooltip("搜索 NPC 时使用的 LayerMask（可只勾 Interactable 层）。")]
    public LayerMask npcMask = ~0;

    public void OpenShop()
    {
        if (!shopUI)
        {
            Debug.LogWarning("[SimpleShopOpener] 未绑定 SimpleShopUI");
            return;
        }

        // 1) 注入依赖
        if (defaultCatalog) shopUI.catalog = defaultCatalog;
        if (inventoryBridge) shopUI.inventoryBridge = inventoryBridge;
        if (wallet) shopUI.wallet = wallet;

        // 2) 决定玩家（不覆盖 SimpleShopUI 上已设置的 player）
        Transform p = player;
        if (!p && shopUI.player) p = shopUI.player;                                  // 保留你手动绑定的
        if (!p) p = TryFindPlayerByHolder();                                          // 玩家背包持有者
        if (!p) p = TryFindPlayerByTag();                                             // tag=Player
        if (!p) p = Camera.main ? Camera.main.transform : null;                       // 兜底

        // 3) 决定 NPC（不覆盖 SimpleShopUI 上已设置的 npc）
        Transform n = overrideNPC ? overrideNPC : null;                               // 显式强制
        if (!n && shopUI.npc) n = shopUI.npc;                                         // 保留你手动绑定的

        // 优先从对话拿当前 NPC，并关闭对话
        if (!n && dialogUI && dialogUI.IsOpen)
        {
            var curr = dialogUI.CurrentNPC;
            if (curr) n = curr.transform;
            dialogUI.Close();
        }

        // 兜底：搜索附近含 NPCInteractable 的最近物体
        if (!n) n = FindClosestNpcWithInteractable(p);

        // 4) 注入上下文并打开
        shopUI.SetContext(p, n);
        shopUI.Open();
    }

    public void CloseShop()
    {
        if (shopUI) shopUI.Close();
    }

    // ---------- 辅助 ----------
    Transform TryFindPlayerByHolder()
    {
        // 尝试找 PlayerInventoryHolder（你项目里有）
        var holder = FindObjectOfType<PlayerInventoryHolder>();
        return holder ? holder.transform : null;
    }

    Transform TryFindPlayerByTag()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        return go ? go.transform : null;
    }

    Transform FindClosestNpcWithInteractable(Transform around)
    {
        if (!around) return null;

        Collider[] cols = Physics.OverlapSphere(
            around.position, findNpcRadius, npcMask, QueryTriggerInteraction.Collide);

        Transform best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < cols.Length; i++)
        {
            var inter = cols[i].GetComponentInParent<NPCInteractable>();
            if (!inter) continue;

            float d = Vector3.Distance(around.position, inter.transform.position);
            if (d < bestDist)
            {
                best = inter.transform;
                bestDist = d;
            }
        }
        return best;
    }
}
