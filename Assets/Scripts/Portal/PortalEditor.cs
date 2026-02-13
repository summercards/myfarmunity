// Assets/Scripts/Portal/PortalEditor.cs
using UnityEngine;
using UnityEditor;

/// <summary>
/// 传送门编辑器 - 提供便捷的创建和管理功能
/// </summary>
public class PortalEditor : EditorWindow
{
    private string newPortalName = "传送门";
    private string targetScene = "";
    private string targetSpawnID = "";
    private float portalRadius = 1f;
    private Color portalColor = new Color(0f, 0.8f, 1f, 0.5f);

    [MenuItem("Tools/传送门/创建传送门 %#p")]
    public static void ShowWindow()
    {
        GetWindow<PortalEditor>("传送门编辑器");
    }

    private void OnGUI()
    {
        GUILayout.Label("传送门编辑器", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // === 创建传送门 ===
        GUILayout.Label("创建新传送门", EditorStyles.label);
        newPortalName = EditorGUILayout.TextField("传送门名称", newPortalName);
        targetScene = EditorGUILayout.TextField("目标场景", targetScene);
        targetSpawnID = EditorGUILayout.TextField("目标SpawnID", targetSpawnID);
        portalRadius = EditorGUILayout.FloatField("传送门半径", portalRadius);
        portalColor = EditorGUILayout.ColorField("传送门颜色", portalColor);

        EditorGUILayout.Space();
        if (GUILayout.Button("在当前位置创建传送门", GUILayout.Height(30)))
        {
            CreatePortalAtSelection();
        }

        if (GUILayout.Button("创建配对传送门", GUILayout.Height(30)))
        {
            CreatePairedPortals();
        }

        EditorGUILayout.Space();

        // === 快速操作 ===
        GUILayout.Label("快速操作", EditorStyles.label);

        if (GUILayout.Button("为选中对象添加传送门"))
        {
            AddPortalToSelected();
        }

        if (GUILayout.Button("列出所有场景中的传送门"))
        {
            ListAllPortals();
        }

        EditorGUILayout.Space();

        // === 说明 ===
        GUILayout.Label("使用说明", EditorStyles.label);
        EditorGUILayout.HelpBox(
            "1. 选中一个游戏对象或点击创建按钮\n" +
            "2. 设置目标场景名称\n" +
            "3. 设置目标SpawnID（对应场景中SpawnPoint的spawnID）\n" +
            "4. 玩家进入传送门后自动传送\n\n" +
            "快捷键: Ctrl+Shift+P 打开此窗口",
            MessageType.Info
        );
    }

    /// <summary>
    /// 在当前选中位置创建传送门
    /// </summary>
    private void CreatePortalAtSelection()
    {
        Vector3 position = Selection.activeGameObject != null
            ? Selection.activeGameObject.transform.position
            : Vector3.zero;

        CreatePortalWithVisuals(newPortalName, targetScene, targetSpawnID, position, portalColor);
    }

    /// <summary>
    /// 创建配对传送门（来回传送）
    /// </summary>
    private void CreatePairedPortals()
    {
        if (string.IsNullOrEmpty(targetScene))
        {
            EditorUtility.DisplayDialog("错误", "请先设置目标场景名称", "确定");
            return;
        }

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // 创建传送门A（当前场景 -> 目标场景）
        string spawnIDA = $"{currentScene}_Entry";
        CreatePortalWithVisuals($"传送门_A_to_{targetScene}", targetScene, spawnIDA,
            new Vector3(-2, 0, 0), portalColor);

        // 创建SpawnPoint A（目标场景的出生点）
        CreateSpawnPoint(spawnIDA, targetScene, new Vector3(2, 0, 0));

        // 创建传送门B（目标场景 -> 当前场景）
        string spawnIDB = $"{targetScene}_Entry";
        CreatePortalWithVisuals($"传送门_B_to_{currentScene}", currentScene, spawnIDB,
            new Vector3(2, 0, 0), new Color(1f, 0.5f, 0f, 0.5f));

        // 创建SpawnPoint B（当前场景的出生点）
        CreateSpawnPoint(spawnIDB, currentScene, new Vector3(-2, 0, 0));

        Debug.Log($"[PortalEditor] 已创建配对传送门:");
        Debug.Log($"  - 传送门A: {currentScene} -> {targetScene} (SpawnID: {spawnIDA})");
        Debug.Log($"  - 传送门B: {targetScene} -> {currentScene} (SpawnID: {spawnIDB})");
        Debug.Log($"  - SpawnPoint A: {spawnIDA} 在场景 {targetScene}");
        Debug.Log($"  - SpawnPoint B: {spawnIDB} 在场景 {currentScene}");
    }

    /// <summary>
    /// 创建SpawnPoint
    /// </summary>
    private void CreateSpawnPoint(string spawnID, string targetScene, Vector3 position)
    {
        GameObject spawnObj = new GameObject($"SpawnPoint_{spawnID}");
        spawnObj.transform.position = position;

        SpawnPoint spawnPoint = spawnObj.AddComponent<SpawnPoint>();
        spawnPoint.spawnID = spawnID;

        Debug.Log($"[PortalEditor] 已创建SpawnPoint: {spawnID}");
    }

    /// <summary>
    /// 为选中的对象添加传送门
    /// </summary>
    private void AddPortalToSelected()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选中一个游戏对象", "确定");
            return;
        }

        GameObject selected = Selection.activeGameObject;

        // 检查是否已有传送门组件
        if (selected.GetComponent<Portal>() != null)
        {
            EditorUtility.DisplayDialog("提示", "该对象已有传送门组件", "确定");
            return;
        }

        // 添加传送门
        Portal portal = selected.AddComponent<Portal>();
        portal.targetScene = targetScene;
        portal.targetSpawnID = targetSpawnID;

        // 添加碰撞体（如果没有）
        if (selected.GetComponent<Collider>() == null)
        {
            SphereCollider collider = selected.AddComponent<SphereCollider>();
            collider.radius = portalRadius;
            collider.isTrigger = true;
        }
        else
        {
            selected.GetComponent<Collider>().isTrigger = true;
        }

        Debug.Log($"[PortalEditor] 已为 {selected.name} 添加传送门组件");
    }

    /// <summary>
    /// 列出所有场景中的传送门
    /// </summary>
    private void ListAllPortals()
    {
        Portal[] portals = FindObjectsOfType<Portal>();

        if (portals.Length == 0)
        {
            EditorUtility.DisplayDialog("传送门列表", "当前场景中没有传送门", "确定");
            return;
        }

        string info = "当前场景中的传送门:\n\n";
        foreach (Portal portal in portals)
        {
            info += $"• {portal.gameObject.name}\n";
            info += $"  目标: {portal.targetScene}\n";
            info += $"  SpawnID: {portal.targetSpawnID}\n";
            info += $"  位置: {portal.transform.position}\n\n";
        }

        EditorUtility.DisplayDialog("传送门列表", info, "确定");
    }

    /// <summary>
    /// 创建传送门的视觉效果
    /// </summary>
    private void CreatePortalVisual(GameObject parent, Color color)
    {
        // 创建一个半透明球体作为传送门视觉效果
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "PortalVisual";
        visual.transform.parent = parent.transform;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * 2f;

        // 设置材质
        Renderer renderer = visual.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Glossiness", 0.8f);
        material.SetFloat("_Mode", 3f); // Transparent
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetOverrideTag("RenderType", "Transparent");

        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        // 删除碰撞体
        DestroyImmediate(visual.GetComponent<Collider>());
    }

    /// <summary>
    /// 创建带视觉效果的传送门
    /// </summary>
    private void CreatePortalWithVisuals(string name, string targetScene, string spawnID, Vector3 position, Color color)
    {
        GameObject portalObj = new GameObject(name);
        portalObj.transform.position = position;

        // 添加碰撞体
        SphereCollider collider = portalObj.AddComponent<SphereCollider>();
        collider.radius = portalRadius;
        collider.isTrigger = true;

        // 添加传送门脚本
        Portal portal = portalObj.AddComponent<Portal>();
        portal.targetScene = targetScene;
        portal.targetSpawnID = spawnID;

        // 添加视觉效果生成器
        PortalVisualGenerator visualGen = portalObj.AddComponent<PortalVisualGenerator>();
        visualGen.portalColor = color;
        visualGen.portalSize = portalRadius * 2f;

        // 立即生成视觉效果
        visualGen.GenerateVisuals();

        // 选中新建的传送门
        Selection.activeGameObject = portalObj;

        Debug.Log($"[PortalEditor] 已创建传送门: {name} -> {targetScene} (SpawnID: {spawnID})");
    }
}
