// Assets/Scripts/Portal/TeleportFixer.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// 传送修复工具
/// 自动检测和修复传送后的Player问题
/// </summary>
public class TeleportFixer : MonoBehaviour
{
    [Header("修复选项")]
    [Tooltip("启用自动修复")]
    public bool enableAutoFix = true;

    [Tooltip("调试模式")]
    public bool debugMode = true;

    [Header("修复设置")]
    [Tooltip("默认出生位置")]
    public Vector3 defaultSpawnPosition = new Vector3(0, 1, 0);

    [Tooltip("默认出生旋转")]
    public Quaternion defaultSpawnRotation = Quaternion.identity;

    private bool isFixing = false;

    private void Update()
    {
        if (!enableAutoFix) return;
        if (isFixing) return;

        // 检查是否有Player
        GameObject player = RuntimeRefs.PlayerTransform ? RuntimeRefs.PlayerTransform.gameObject : null;
        if (player == null)
        {
            if (debugMode)
                Debug.LogWarning("[TeleportFixer] 未找到Player，可能需要修复");
            return;
        }

        // 检查Player是否被禁用
        if (!player.activeInHierarchy)
        {
            StartCoroutine(FixDisabledPlayer(player));
            return;
        }

        // 检查Player位置是否合理
        if (player.transform.position.y < -10f || player.transform.position.y > 100f)
        {
            StartCoroutine(FixPlayerPosition(player));
            return;
        }
    }

    /// <summary>
    /// 修复被禁用的Player
    /// </summary>
    private IEnumerator FixDisabledPlayer(GameObject player)
    {
        isFixing = true;

        if (debugMode)
            Debug.LogWarning($"[TeleportFixer] 检测到Player被禁用，正在修复...");

        yield return null;

        player.SetActive(true);

        if (debugMode)
            Debug.Log($"[TeleportFixer] Player已启用，当前位置: {player.transform.position}");

        isFixing = false;
    }

    /// <summary>
    /// 修复Player位置
    /// </summary>
    private IEnumerator FixPlayerPosition(GameObject player)
    {
        isFixing = true;

        if (debugMode)
            Debug.LogWarning($"[TeleportFixer] 检测到Player位置异常: {player.transform.position}，正在修复...");

        yield return null;

        // 禁用CharacterController以避免冲突
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            if (debugMode) Debug.Log("[TeleportFixer] 已禁用CharacterController");
        }

        // 设置Player位置
        player.transform.position = defaultSpawnPosition;
        player.transform.rotation = defaultSpawnRotation;

        if (debugMode)
            Debug.Log($"[TeleportFixer] Player位置已修复: {player.transform.position}");

        yield return null;

        // 重新启用CharacterController
        if (cc != null)
        {
            cc.enabled = true;
            if (debugMode) Debug.Log("[TeleportFixer] 已重新启用CharacterController");
        }

        isFixing = false;
    }

    /// <summary>
    /// 手动修复Player
    /// </summary>
    [ContextMenu("手动修复Player")]
    public void ManualFixPlayer()
    {
        GameObject player = RuntimeRefs.PlayerTransform ? RuntimeRefs.PlayerTransform.gameObject : null;

        if (player == null)
        {
            Debug.LogError("[TeleportFixer] 无法修复：场景中没有Player");
            return;
        }

        StartCoroutine(PerformManualFix(player));
    }

    /// <summary>
    /// 执行手动修复
    /// </summary>
    private IEnumerator PerformManualFix(GameObject player)
    {
        if (debugMode)
            Debug.Log($"[TeleportFixer] 开始手动修复Player...");

        isFixing = true;

        // 等待一帧
        yield return null;

        // 确保Player启用
        if (!player.activeInHierarchy)
        {
            player.SetActive(true);
            if (debugMode) Debug.Log("[TeleportFixer] 已启用Player");
        }

        // 禁用CharacterController
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }

        // 设置位置
        player.transform.position = defaultSpawnPosition;
        player.transform.rotation = defaultSpawnRotation;

        if (debugMode)
            Debug.Log($"[TeleportFixer] Player位置已设置: {player.transform.position}");

        yield return null;

        // 重新启用CharacterController
        if (cc != null)
        {
            cc.enabled = true;
        }

        // 确保Player的Transform存在
        if (player.transform == null)
        {
            Debug.LogError("[TeleportFixer] Player缺少Transform组件！");
        }

        // 检查Player是否有必要的组件
        CheckPlayerComponents(player);

        isFixing = false;

        if (debugMode)
            Debug.Log("[TeleportFixer] 手动修复完成");
    }

    /// <summary>
    /// 检查Player的组件
    /// </summary>
    private void CheckPlayerComponents(GameObject player)
    {
        if (debugMode) Debug.Log("[TeleportFixer] 检查Player组件...");

        // 检查必要的组件
        string[] requiredComponents = { "CharacterController", "TPSInput", "TPSCharacter" };

        foreach (string componentName in requiredComponents)
        {
            Component comp = player.GetComponent(componentName);
            if (comp == null)
            {
                Debug.LogWarning($"[TeleportFixer] Player缺少组件: {componentName}");
            }
            else
            {
                if (debugMode) Debug.Log($"[TeleportFixer] ✓ {componentName}");
            }
        }
    }

    /// <summary>
    /// 显示当前Player状态
    /// </summary>
    [ContextMenu("显示Player状态")]
    public void ShowPlayerStatus()
    {
        GameObject player = RuntimeRefs.PlayerTransform ? RuntimeRefs.PlayerTransform.gameObject : null;

        if (player == null)
        {
            Debug.Log("[TeleportFixer] 当前场景中没有Player");
            return;
        }

        System.Text.StringBuilder status = new System.Text.StringBuilder();
        status.AppendLine($"=== Player 状态 ===");
        status.AppendLine($"名称: {player.name}");
        status.AppendLine($"启用: {player.activeInHierarchy}");
        status.AppendLine($"位置: {player.transform.position}");
        status.AppendLine($"旋转: {player.transform.rotation.eulerAngles}");

        status.AppendLine($"\n=== 组件列表 ===");
        Component[] components = player.GetComponents<Component>();
        foreach (var comp in components)
        {
            status.AppendLine($"- {comp.GetType().Name}");
        }

        Debug.Log(status.ToString());
    }
}
