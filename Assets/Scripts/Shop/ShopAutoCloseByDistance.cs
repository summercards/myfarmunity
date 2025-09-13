using UnityEngine;
using System.Collections;

/// <summary>
/// 把这个组件挂在商店 UI 根物体（含 SimpleShopUI 的同一个物体）上。
/// 作用：玩家与当前交易 NPC/锚点的距离超过阈值时，自动关闭商店，并结束头顶气泡。
/// </summary>
[DisallowMultipleComponent]
public class ShopAutoCloseByDistance : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SimpleShopUI shopUI;                // 指向你的商店 UI 脚本
    [SerializeField] private Transform target;                   // 当前交互 NPC 或其头顶气泡锚点
    [SerializeField] private Transform player;                   // 玩家（可空，自动按 Tag 查找）
    [SerializeField] private string playerTag = "Player";        // 玩家 Tag，默认 Player
    [SerializeField] private NPCDialogWorldBridge worldBridge;   // 可空，自动查找，用于关掉头顶气泡

    [Header("Auto Close by Distance")]
    [SerializeField] private bool autoCloseWhenFar = true;       // 打开自动关闭功能
    [SerializeField] private float closeDistance = 4f;           // 超过这个距离就关闭
    [Tooltip("防抖阈值，避免临界值来回抖动；通常 0.2~0.4 即可")]
    [SerializeField] private float hysteresis = 0.2f;
    [SerializeField] private float checkInterval = 0.1f;         // 轮询间隔，避免每帧计算

    private Coroutine loop;

    /// <summary>
    /// 在打开商店时绑定 NPC/锚点与玩家
    /// </summary>
    public void Bind(Transform npcOrAnchor, Transform playerTransform = null)
    {
        target = npcOrAnchor;
        if (playerTransform != null) player = playerTransform;
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
            if (!shopUI.IsOpen) continue;        // 你的 SimpleShopUI 需要有 IsOpen 属性（见下方说明）
            if (!autoCloseWhenFar) continue;

            // 自动按 Tag 获取玩家
            if (player == null && !string.IsNullOrEmpty(playerTag))
            {
                var go = GameObject.FindGameObjectWithTag(playerTag);
                if (go != null) player = go.transform;
            }

            if (player == null || target == null) continue;

            float d = Vector3.Distance(player.position, target.position);
            if (d > closeDistance + hysteresis)
            {
                // 先结束任何头顶气泡（商店台词/独立气泡）
                if (worldBridge == null) worldBridge = FindObjectOfType<NPCDialogWorldBridge>();
                if (worldBridge != null) worldBridge.EndStandalone();

                // 再关闭商店
                shopUI.Close();
            }
        }
    }
}
