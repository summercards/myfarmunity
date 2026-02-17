using UnityEngine;
using System.Collections;

/// <summary>
/// �������������̵� UI �����壨�� SimpleShopUI ��ͬһ�����壩�ϡ�
/// ���ã�����뵱ǰ���� NPC/ê��ľ��볬����ֵʱ���Զ��ر��̵꣬������ͷ�����ݡ�
/// </summary>
[DisallowMultipleComponent]
public class ShopAutoCloseByDistance : MonoBehaviour
{
    private NPCDialogWorldBridge _cachedWorldBridge;

    void Awake()
    {
        _cachedWorldBridge = FindObjectOfType<NPCDialogWorldBridge>();
    }

    [Header("References")]
    [SerializeField] private SimpleShopUI shopUI;                // ָ������̵� UI �ű�
    [SerializeField] private Transform target;                   // ��ǰ���� NPC ����ͷ������ê��
    [SerializeField] private Transform player;                   // ��ң��ɿգ��Զ��� Tag ���ң�
    [SerializeField] private string playerTag = "Player";        // ��� Tag��Ĭ�� Player
    [SerializeField] private NPCDialogWorldBridge worldBridge;   // �ɿգ��Զ����ң����ڹص�ͷ������

    [Header("Auto Close by Distance")]
    [SerializeField] private bool autoCloseWhenFar = true;       // ���Զ��رչ���
    [SerializeField] private float closeDistance = 4f;           // �����������͹ر�
    [Tooltip("������ֵ�������ٽ�ֵ���ض�����ͨ�� 0.2~0.4 ����")]
    [SerializeField] private float hysteresis = 0.2f;
    [SerializeField] private float checkInterval = 0.1f;         // ��ѯ���������ÿ֡����

    private Coroutine loop;

    /// <summary>
    /// �ڴ��̵�ʱ�� NPC/ê�������
    /// </summary>
    public void Bind(Transform npcOrAnchor, Transform playerTransform = null)
    {
        target = npcOrAnchor;
        if (playerTransform != null) player = playerTransform;
        _cachedWorldBridge = FindObjectOfType<NPCDialogWorldBridge>();
    }

    private void OnEnable()
    {
        if (loop == null) loop = StartCoroutine(CheckLoop());
    }

    private void OnDisable()
    {
        if (loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }
    }

    private IEnumerator CheckLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            if (shopUI == null) continue;
            if (!shopUI.IsOpen) continue;        // ��� SimpleShopUI ��Ҫ�� IsOpen ���ԣ����·�˵����
            if (!autoCloseWhenFar) continue;

            // �Զ��� Tag ��ȡ���
            if (player == null && !string.IsNullOrEmpty(playerTag))
            {
                var go = GameObject.FindGameObjectWithTag(playerTag);
                if (go != null) player = go.transform;
            }

            if (player == null || target == null) continue;

            float d = Vector3.Distance(player.position, target.position);
            if (d > closeDistance + hysteresis)
            {
                // �Ƚ����κ�ͷ�����ݣ��̵�̨��/�������ݣ�
                
                if (_cachedWorldBridge != null) _cachedWorldBridge.EndStandalone();

                // �ٹر��̵�
                shopUI.Close();
            }
        }
    }
}
