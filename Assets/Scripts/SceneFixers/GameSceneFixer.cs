using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// game场景修复器
/// 自动检测并修复game场景中缺失的Player引用
///
/// 使用方法：
/// 1. 打开 game.unity 场景
/// 2. 在菜单中选择 Tools > Scene Fixers > Fix Game Scene
/// 或者点击组件上的 "Fix Scene" 按钮
/// </summary>
public class GameSceneFixer : MonoBehaviour
{
    [Header("自动修复配置")]
    [Tooltip("Player对象的名称前缀")]
    public string playerObjectNamePrefix = "Player";

    [Tooltip("是否在修复后保存场景")]
    public bool saveSceneAfterFix = true;

    [Header("查找结果（只读）")]
    [SerializeField] private int playerObjectsFound = 0;
    [SerializeField] private int inventoryHoldersFound = 0;
    [SerializeField] private int activeItemControllersFound = 0;
    [SerializeField] private int referencesFixed = 0;

    /// <summary>
    /// 执行场景修复
    /// </summary>
    [ContextMenu("Fix Game Scene")]
    public void FixScene()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("[GameSceneFixer] 请在编辑器模式下运行，不要在Play模式下执行！");
            return;
        }

        Debug.Log("[GameSceneFixer] ===== 开始修复 game 场景 =====");

        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "game")
        {
            Debug.LogWarning($"[GameSceneFixer] 当前场景是 '{scene.name}'，建议打开 game.unity 场景");
        }

        // 重置计数器
        playerObjectsFound = 0;
        inventoryHoldersFound = 0;
        activeItemControllersFound = 0;
        referencesFixed = 0;

        // 查找所有可能的Player对象
        GameObject[] allObjects = scene.GetRootGameObjects();
        GameObject playerObj = null;
        GameObject altPlayerObj = null;

        foreach (var root in allObjects)
        {
            // 递归查找所有对象
            var found = FindPlayerObjects(root.transform);
            if (found.player != null) playerObj = found.player;
            if (found.altPlayer != null) altPlayerObj = found.altPlayer;
        }

        // 查找组件持有者（包含脚本引用的对象）
        GameObject managerHolder = FindObjectWithComponent<SceneReferenceManager>(allObjects);

        // 报告发现的对象
        Debug.Log($"[GameSceneFixer] 找到的对象:");
        Debug.Log($"  - Player对象: {(playerObj != null ? playerObj.name : "未找到")}");
        Debug.Log($"[GameSceneFixer]  - 备用Player: {(altPlayerObj != null ? altPlayerObj.name : "未找到")}");
        Debug.Log($"[GameSceneFixer]  - 引用管理器: {(managerHolder != null ? managerHolder.name : "未找到")}");

        // 检查和修复Player对象上的组件
        if (playerObj != null)
        {
            FixPlayerComponents(playerObj);
        }
        else if (altPlayerObj != null)
        {
            Debug.LogWarning("[GameSceneFixer] 未找到主Player对象，使用备用Player");
            FixPlayerComponents(altPlayerObj);
        }
        else
        {
            Debug.LogError("[GameSceneFixer] 未找到任何Player对象！无法自动修复。");
        }

        // 检查和修复SceneReferenceManager上的引用
        if (managerHolder != null)
        {
            FixSceneReferences(managerHolder, playerObj ?? altPlayerObj);
        }

        // 输出修复摘要
        Debug.Log($"[GameSceneFixer] ===== 修复完成 =====");
        Debug.Log($"[GameSceneFixer] Player对象: {playerObjectsFound}");
        Debug.Log($"[GameSceneFixer] InventoryHolders: {inventoryHoldersFound}");
        Debug.Log($"[GameSceneFixer] ActiveItemControllers: {activeItemControllersFound}");
        Debug.Log($"[GameSceneFixer] 修复的引用: {referencesFixed}");

        // 标记场景为已修改
        if (referencesFixed > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[GameSceneFixer] 场景已标记为脏（需要保存）");

            if (saveSceneAfterFix)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[GameSceneFixer] 场景已保存");
            }
        }
        else
        {
            Debug.Log("[GameSceneFixer] 没有发现需要修复的问题");
        }
    }

    /// <summary>
    /// 修复Player对象上的组件
    /// </summary>
    private void FixPlayerComponents(GameObject playerObj)
    {
        playerObjectsFound++;

        // 检查PlayerInventoryHolder
        var inv = playerObj.GetComponent<PlayerInventoryHolder>();
        if (inv == null)
        {
            // 尝试在子对象中查找
            inv = playerObj.GetComponentInChildren<PlayerInventoryHolder>();
            if (inv != null)
            {
                Debug.Log($"[GameSceneFixer] PlayerInventoryHolder 在子对象中: {inv.gameObject.name}");
            }
        }
        if (inv != null)
        {
            inventoryHoldersFound++;
            Debug.Log($"[GameSceneFixer] ✓ PlayerInventoryHolder 已找到");
        }
        else
        {
            Debug.LogWarning($"[GameSceneFixer] ✗ PlayerInventoryHolder 缺失！需要手动添加组件");
        }

        // 检查ActiveItemController
        var active = playerObj.GetComponent<ActiveItemController>();
        if (active == null)
        {
            active = playerObj.GetComponentInChildren<ActiveItemController>();
        }
        if (active != null)
        {
            activeItemControllersFound++;
            Debug.Log($"[GameSceneFixer] ✓ ActiveItemController 已找到");
        }
        else
        {
            Debug.LogWarning($"[GameSceneFixer] ✗ ActiveItemController 缺失！需要手动添加组件");
        }

        // 检查PlayerBuilder
        var builder = playerObj.GetComponent<PlayerBuilder>();
        if (builder == null)
        {
            builder = playerObj.GetComponentInChildren<PlayerBuilder>();
        }
        if (builder != null)
        {
            Debug.Log($"[GameSceneFixer] ✓ PlayerBuilder 已找到");
            // 检查Builder的引用
            if (builder.catalog == null)
            {
                Debug.LogWarning("[GameSceneFixer] PlayerBuilder.catalog 未配置！需要手动分配 BuildCatalogSO");
            }
        }
        else
        {
            Debug.LogWarning($"[GameSceneFixer] ✗ PlayerBuilder 缺失！建造系统无法工作");
        }

        // 检查PlayerStats
        var stats = playerObj.GetComponent<PlayerStats>();
        if (stats != null)
        {
            Debug.Log($"[GameSceneFixer] ✓ PlayerStats 已找到");
        }
    }

    /// <summary>
    /// 修复场景引用管理器上的引用
    /// </summary>
    private void FixSceneReferences(GameObject managerHolder, GameObject playerObj)
    {
        if (playerObj == null)
        {
            Debug.LogError("[GameSceneFixer] 无法修复引用：Player对象为空");
            return;
        }

        // 使用SerializedObject修改SceneReferenceManager的引用
        var so = new SerializedObject(managerHolder);
        var playerInvProp = so.FindProperty("playerInv");
        var playerSystemProp = so.FindProperty("playerSystem");
        var manualPlayerProp = so.FindProperty("manualPlayer");

        var inv = playerObj.GetComponent<PlayerInventoryHolder>();
        var active = playerObj.GetComponent<ActiveItemController>();

        bool modified = false;

        if (playerInvProp != null && inv != null)
        {
            playerInvProp.objectReferenceValue = inv;
            Debug.Log($"[GameSceneFixer] 修复 playerInv -> {inv.gameObject.name}");
            modified = true;
            referencesFixed++;
        }

        if (playerSystemProp != null && active != null)
        {
            playerSystemProp.objectReferenceValue = active;
            Debug.Log($"[GameSceneFixer] 修复 playerSystem -> {active.gameObject.name}");
            modified = true;
            referencesFixed++;
        }

        if (manualPlayerProp != null)
        {
            manualPlayerProp.objectReferenceValue = playerObj.transform;
            Debug.Log($"[GameSceneFixer] 修复 manualPlayer -> {playerObj.name}");
            modified = true;
            referencesFixed++;
        }

        if (modified)
        {
            so.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// 递归查找Player对象
    /// </summary>
    private (GameObject player, GameObject altPlayer) FindPlayerObjects(Transform root)
    {
        GameObject player = null;
        GameObject altPlayer = null;

        // 检查当前对象
        if (root.name.StartsWith(playerObjectNamePrefix, System.StringComparison.OrdinalIgnoreCase))
        {
            // 检查是否有关键组件
            if (root.GetComponent<PlayerInventoryHolder>() != null ||
                root.GetComponent<ActiveItemController>() != null ||
                root.GetComponent<PlayerStats>() != null)
            {
                player = root.gameObject;
            }
            else
            {
                altPlayer = root.gameObject;
            }
        }

        // 递归子对象
        foreach (Transform child in root)
        {
            var (p, a) = FindPlayerObjects(child);
            if (p != null && player == null) player = p;
            if (a != null && altPlayer == null) altPlayer = a;
        }

        return (player, altPlayer);
    }

    /// <summary>
    /// 查找包含指定组件的对象
    /// </summary>
    private GameObject FindObjectWithComponent<T>(GameObject[] roots) where T : Component
    {
        foreach (var root in roots)
        {
            var result = FindComponentInChildren<T>(root.transform);
            if (result != null) return result.gameObject;
        }
        return null;
    }

    private T FindComponentInChildren<T>(Transform root) where T : Component
    {
        var component = root.GetComponent<T>();
        if (component != null) return component;

        foreach (Transform child in root)
        {
            var found = FindComponentInChildren<T>(child);
            if (found != null) return found;
        }

        return null;
    }
}


/// <summary>
/// 编辑器菜单扩展
/// </summary>
public static class GameSceneFixerMenu
{
    [MenuItem("Tools/Scene Fixers/Fix Game Scene", false, 1)]
    private static void FixGameSceneFromMenu()
    {
        // 查找当前场景中的Fixer
        var fixer = Object.FindFirstObjectByType<GameSceneFixer>();
        if (fixer == null)
        {
            // 创建临时的Fixer对象
            var go = new GameObject("TempSceneFixer");
            fixer = go.AddComponent<GameSceneFixer>();
        }

        fixer.FixScene();
    }

    [MenuItem("Tools/Scene Fixers/Fix Game Scene", true, 1)]
    private static bool ValidateFixGameScene()
    {
        return !Application.isPlaying && EditorSceneManager.GetActiveScene().name == "game";
    }

    [MenuItem("Tools/Scene Fixers/Create Fixer in Current Scene", false, 2)]
    private static void CreateFixerInScene()
    {
        var go = new GameObject("GameSceneFixer");
        go.AddComponent<GameSceneFixer>();
        Debug.Log("[GameSceneFixer] 已创建 GameSceneFixer 对象");
    }
}


#endif

/// <summary>
/// 场景引用管理器（game场景中可能存在的组件）
/// </summary>
public class SceneReferenceManager : MonoBehaviour
{
    public PlayerInventoryHolder playerInv;
    public ActiveItemController playerSystem;
    public Transform manualPlayer;
    public string playerTag = "Player";
}
