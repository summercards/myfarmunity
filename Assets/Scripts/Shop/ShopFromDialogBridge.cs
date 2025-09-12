using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

/// <summary>
/// �ӡ��Ի���尴ť�����̵꣬����**���˲��**�л�Ϊ���̵�̨�ʡ���
/// - ��ȡ NPCDialogUI �� CurrentNPC��������ԭ�ű����÷����ȡ��
/// - �� player & npc ���� SimpleShopUI.SetContext(...)��
/// - ���� SimpleShopUI.Open()��
/// - �� Open ֮ǰ�ȶ� NPCDialogWorldBridge �� ShowStandalone(..., shopOpenLine)��
///   ȷ����ʹ���Ի���屻���أ�ͷ������Ҳ���е��̵��İ���
/// </summary>
[DisallowMultipleComponent]
public class ShopFromDialogBridge : MonoBehaviour
{
    [Header("Refs")]
    public NPCDialogUI npcDialogUI;                 // Panel_NPCDialog �ϵ����
    public NPCDialogWorldBridge dialogWorldBridge;  // ͬ�����ϵ��Ž���������ֱ�������ã�
    public SimpleShopUI shopUI;                     // �̵� UI ���
    public Transform player;                        // ��ң��ɿգ��Զ��� Tag=Player ���������

    [Header("Button����ѡ��")]
    public Button openShopButton;                   // �����룬������Զ��󶨵���¼�

    [Header("Shop Line")]
    [TextArea] public string shopOpenLine = "��ӭ���٣���Ҫ��ʲô��";

    // ���仺�棺��ȡ CurrentNPC
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

    /// <summary>����������󵽡����̵ꡱ��ť�� OnClick()</summary>
    public void OpenShopForCurrentNPC()
    {
        if (!shopUI || !npcDialogUI)
        {
            Debug.LogWarning("[ShopFromDialogBridge] ȱ�����ã�shopUI �� npcDialogUI��");
            return;
        }

        // 1) ������ǰ NPC
        Transform npcTr = ResolveCurrentNPCTransform();
        if (!npcTr)
        {
            Debug.LogWarning("[ShopFromDialogBridge] �Ҳ�����ǰ NPC Transform��");
            return;
        }

        // 2) ���е��̵�̨�ʣ������ر�ǰִ�У�ȷ���Ž������ڣ�
        var bridge = dialogWorldBridge ? dialogWorldBridge : Object.FindObjectOfType<NPCDialogWorldBridge>();
        if (bridge)
        {
            var anchor = npcTr.Find("BubbleAnchor");
            bridge.ShowStandalone(anchor ? anchor : npcTr, shopOpenLine);
        }

        // 3) ����� & NPC �����Ĵ����̵꣬�ٴ��̵�
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
