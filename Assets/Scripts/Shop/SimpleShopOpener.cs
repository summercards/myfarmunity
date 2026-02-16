using UnityEngine;
using FarmGame.NPCSystem;

/// <summary>
/// ���̵꣺
/// - �������� SimpleShopUI ���Ѿ��ֶ��󶨵� player / npc�������ǣ���
/// - ���û�� npc�����ȴ� NPCDialogUI.CurrentNPC ��ȡ���� Close() �Ի���
/// - ���Ҳ���ʱ���ڰ뾶�������� NPCInteractable ��������壻
/// - ��� Transform ���ܲ��ң���ʽָ�� > SimpleShopUI�Ѱ� > PlayerInventoryHolder > tag=Player > MainCamera��
/// - ����ע�� catalog / bridge / wallet��
/// </summary>
public class SimpleShopOpener : MonoBehaviour
{
    [Header("Refs")]
    public SimpleShopUI shopUI;
    public ShopCatalogSO defaultCatalog;
    public InventoryBridge inventoryBridge;
    public PlayerWallet wallet;

    [Header("Dialog (Optional)")]
    [Tooltip("�����е� NPCDialogUI���������� PlayerInteractor.sharedDialogUI ��ͬ����һ����")]
    public NPCDialogUI dialogUI;

    [Header("Context (Optional)")]
    [Tooltip("��ʽָ����� Transform�����ȼ���ߣ���ͨ�������ա�")]
    public Transform player;
    [Tooltip("��ʽָ�� NPC���������̶�ĳ��NPC����")]
    public Transform overrideNPC;

    [Header("Fallback Search")]
    [Tooltip("���޷��ӶԻ������ NPC ʱ���ڸð뾶����������� NPCInteractable��")]
    public float findNpcRadius = 6.0f;
    [Tooltip("���� NPC ʱʹ�õ� LayerMask����ֻ�� Interactable �㣩��")]
    public LayerMask npcMask = ~0;

    public void OpenShop()
    {
        if (!shopUI)
        {
            Debug.LogWarning("[SimpleShopOpener] δ�� SimpleShopUI");
            return;
        }

        // 1) ע������
        if (defaultCatalog) shopUI.catalog = defaultCatalog;
        if (inventoryBridge) shopUI.inventoryBridge = inventoryBridge;
        if (wallet) shopUI.wallet = wallet;

        // 2) ������ң������� SimpleShopUI �������õ� player��
        Transform p = player;
        if (!p && shopUI.player) p = shopUI.player;                                  // �������ֶ��󶨵�
        if (!p) p = TryFindPlayerByHolder();                                          // ��ұ���������
        if (!p) p = TryFindPlayerByTag();                                             // tag=Player
        if (!p) p = Camera.main ? Camera.main.transform : null;                       // ����

        // 3) ���� NPC�������� SimpleShopUI �������õ� npc��
        Transform n = overrideNPC ? overrideNPC : null;                               // ��ʽǿ��
        if (!n && shopUI.npc) n = shopUI.npc;                                         // �������ֶ��󶨵�

        // ���ȴӶԻ��õ�ǰ NPC�����رնԻ�
        if (!n && dialogUI && dialogUI.IsOpen)
        {
            var curr = dialogUI.CurrentNPC;
            if (curr != null) n = curr.SubjectTransform;
            dialogUI.Close();
        }

        // ���ף����������� NPCInteractable ���������
        if (!n) n = FindClosestNpcWithInteractable(p);

        // 4) ע�������Ĳ���
        shopUI.SetContext(p, n);
        shopUI.Open();
    }

    public void CloseShop()
    {
        if (shopUI) shopUI.Close();
    }

    // ---------- ���� ----------
    Transform TryFindPlayerByHolder()
    {
        // ������ PlayerInventoryHolder������Ŀ���У�
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
