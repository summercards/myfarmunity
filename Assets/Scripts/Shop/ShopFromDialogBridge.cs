using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

/// <summary>
/// �ѡ�NPC �Ի������ġ����̵꡿��ť���� SimpleShopUI ����������
/// - �� NPCDialogUI ��ȡ�� CurrentNPC�����䣬������ԭ�ļ���
/// - ���� SimpleShopUI.SetContext(player, npc)
/// - ���� SimpleShopUI.Open()�����ڲ��Ქ���̵�̨�ʡ�������ֻ��֤ npc ���ԣ�
/// </summary>
[DisallowMultipleComponent]
public class ShopFromDialogBridge : MonoBehaviour
{
    [Header("Refs")]
    public NPCDialogUI npcDialogUI;       // �Ի���壨Panel_NPCDialog �ϵ��Ǹ���
    public SimpleShopUI shopUI;           // ����̵� UI �����
    public Transform player;              // ��ң��ɿգ�Ϊ��ʱ�Զ��� Player tag ���������

    [Header("Button (��ѡ)")]
    public Button openShopButton;         // �������ֱ��������󶨰�ť��Ҳ���԰Ѱ�ť�Ͻ���

    // ���仺��
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

        // ��ѡ������Ѱ�ť�Ͻ����ˣ�����˳�������
        if (openShopButton)
        {
            openShopButton.onClick.RemoveAllListeners();
            openShopButton.onClick.AddListener(OpenShopForCurrentNPC);
        }
    }

    /// <summary>�ڰ�ť�� OnClick() ���������������ɡ�</summary>
    public void OpenShopForCurrentNPC()
    {
        if (!shopUI || !npcDialogUI) return;

        // 1) ȡ����ǰ NPC �� Transform
        Transform npcTransform = ResolveCurrentNPCTransform();
        if (!npcTransform)
        {
            Debug.LogWarning("[ShopFromDialogBridge] �Ҳ�����ǰ NPC �� Transform���޷����̵ꡣ");
            return;
        }

        // 2) ���̵괫�����ģ���� & NPC��
        shopUI.SetContext(player, npcTransform);

        // 3) ���̵꣨SimpleShopUI.Open �ڲ����Զ������̵�̨�ʡ���
        shopUI.Open();

        // 4) ��ѡ�������ϣ�����̵�ʱ�ѶԻ����Ҳ�ص���
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
