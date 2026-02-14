// Assets/Scripts/Editor/UITemplateGenerator.cs
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// UI模板生成工具
/// 一键在场景中生成完整的UI系统，保留所有功能和存档
/// </summary>
public class UITemplateGenerator : EditorWindow
{
    private static UITemplateGenerator window;

    [MenuItem("Tools/UI工具/生成UI模板 %#u")]
    public static void ShowWindow()
    {
        window = GetWindow<UITemplateGenerator>("UI模板生成器");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("UI模板生成器", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "此工具会在当前场景中生成完整的UI系统，\n" +
            "包括背包、快捷栏、角色属性、商店、NPC对话框等。\n\n" +
            "✅ 保留所有功能\n" +
            "✅ 保持存档数据\n" +
            "✅ 自动关联玩家\n" +
            "❌ 不使用预制体（避免冲突）",
            MessageType.Info
        );

        EditorGUILayout.Space();

        GUILayout.Label("生成选项", EditorStyles.boldLabel);

        bool createInventory = GUILayout.Toggle(true, "✓ 背包系统 (InventoryUI)");
        bool createHotbar = GUILayout.Toggle(true, "✓ 快捷栏 (HotbarUI)");
        bool createCharacterStats = GUILayout.Toggle(true, "✓ 角色属性 (CharacterStatsUI)");
        bool createNPCDialog = GUILayout.Toggle(true, "✓ NPC对话框 (NPCDialogUI)");
        bool createShop = GUILayout.Toggle(true, "✓ 商店系统 (SimpleShopUI)");
        bool createPickupHUD = GUILayout.Toggle(true, "✓ 拾取提示 (PickupHUD)");
        bool createWalletHUD = GUILayout.Toggle(true, "✓ 钱包显示 (WalletHUD)");

        EditorGUILayout.Space();

        GUILayout.Label("操作", EditorStyles.boldLabel);

        if (GUILayout.Button("生成完整UI系统", GUILayout.Height(40)))
        {
            GenerateFullUISystem();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("仅生成背包系统"))
        {
            GenerateInventoryOnly();
        }

        if (GUILayout.Button("仅生成快捷栏"))
        {
            GenerateHotbarOnly();
        }

        if (GUILayout.Button("生成NPC对话系统"))
        {
            GenerateNPCDialogSystem();
        }

        EditorGUILayout.Space();

        GUILayout.Label("检查当前场景", EditorStyles.boldLabel);

        if (GUILayout.Button("分析现有UI结构"))
        {
            AnalyzeCurrentUIStructure();
        }

        EditorGUILayout.HelpBox(
            "提示：生成前建议先保存场景。\n" +
            "生成后可以手动调整UI布局和位置。",
            MessageType.None
        );
    }

    /// <summary>
    /// 生成完整UI系统
    /// </summary>
    private void GenerateFullUISystem()
    {
        GameObject guiRoot = new GameObject("GUI");
        GameObject canvasGO = CreateCanvas(guiRoot);

        // 生成各个UI组件
        GenerateInventoryUI(canvasGO);
        GenerateHotbarUI(canvasGO);
        GenerateCharacterStatsUI(canvasGO);
        GenerateNPCDialogUI(canvasGO);
        GenerateShopUI(canvasGO);
        GeneratePickupHUD(canvasGO);
        GenerateWalletHUD(canvasGO);

        Selection.activeGameObject = guiRoot;
        Debug.Log("[UITemplateGenerator] 完整UI系统已生成！");

        EditorUtility.DisplayDialog("生成完成",
            "完整UI系统已成功生成到当前场景！\n\n" +
            "包含：\n" +
            "- 背包系统\n" +
            "- 快捷栏\n" +
            "- 角色属性\n" +
            "- NPC对话框\n" +
            "- 商店系统\n" +
            "- 拾取提示\n" +
            "- 钱包显示",
            "确定");
    }

    /// <summary>
    /// 生成背包UI
    /// </summary>
    private void GenerateInventoryUI(GameObject canvas)
    {
        GameObject inventoryGO = new GameObject("InventoryUI");
        inventoryGO.transform.SetParent(canvas.transform, false);
        AddRectTransform(inventoryGO, new Vector2(400, 500), new Vector2(0.5f, 0.5f));

        InventoryUI inventoryUI = inventoryGO.AddComponent<InventoryUI>();
        // InventoryUI 默认就是关闭的，不需要设置

        Debug.Log("[UITemplateGenerator] InventoryUI 已生成");
    }

    /// <summary>
    /// 生成快捷栏UI
    /// </summary>
    private void GenerateHotbarUI(GameObject canvas)
    {
        GameObject hotbarGO = new GameObject("HotbarUI");
        hotbarGO.transform.SetParent(canvas.transform, false);
        AddRectTransform(hotbarGO, new Vector2(400, 80), new Vector2(0.5f, 0.05f));

        // 这里可以根据需要添加快捷栏UI组件
        // 目前 Hotbar 功能主要通过 HotbarSelector 组件实现

        Debug.Log("[UITemplateGenerator] HotbarUI 已生成");
    }

    /// <summary>
    /// 生成角色属性UI
    /// </summary>
    private void GenerateCharacterStatsUI(GameObject canvas)
    {
        GameObject statsGO = new GameObject("CharacterStatsUI");
        statsGO.transform.SetParent(canvas.transform, false);
        AddRectTransform(statsGO, new Vector2(300, 200), new Vector2(0.5f, 0.5f));

        CharacterStatsUI statsUI = statsGO.AddComponent<CharacterStatsUI>();

        Debug.Log("[UITemplateGenerator] CharacterStatsUI 已生成");
    }

    /// <summary>
    /// 生成NPC对话框UI
    /// </summary>
    private void GenerateNPCDialogUI(GameObject canvas)
    {
        GameObject dialogGO = new GameObject("NPCDialogUI");
        dialogGO.transform.SetParent(canvas.transform, false);
        AddRectTransform(dialogGO, new Vector2(600, 150), new Vector2(0.5f, 0.15f));

        NPCDialogUI dialogUI = dialogGO.AddComponent<NPCDialogUI>();

        Debug.Log("[UITemplateGenerator] NPCDialogUI 已生成");
    }

    /// <summary>
    /// 生成商店UI
    /// </summary>
    private void GenerateShopUI(GameObject canvas)
    {
        GameObject shopGO = new GameObject("SimpleShopUI");
        shopGO.transform.SetParent(canvas.transform, false);
        AddRectTransform(shopGO, new Vector2(800, 600), new Vector2(0.5f, 0.5f));

        SimpleShopUI shopUI = shopGO.AddComponent<SimpleShopUI>();

        Debug.Log("[UITemplateGenerator] SimpleShopUI 已生成");
    }

    /// <summary>
    /// 生成拾取提示HUD
    /// </summary>
    private void GeneratePickupHUD(GameObject canvas)
    {
        GameObject pickupGO = new GameObject("PickupHUD");
        pickupGO.transform.SetParent(canvas.transform, false);
        AddRectTransform(pickupGO, new Vector2(200, 50), new Vector2(0.5f, 0.9f));

        PickupHUD pickupHUD = pickupGO.AddComponent<PickupHUD>();

        Debug.Log("[UITemplateGenerator] PickupHUD 已生成");
    }

    /// <summary>
    /// 生成钱包显示HUD
    /// </summary>
    private void GenerateWalletHUD(GameObject canvas)
    {
        GameObject walletGO = new GameObject("WalletHUD");
        walletGO.transform.SetParent(canvas.transform, false);
        AddRectTransform(walletGO, new Vector2(150, 50), new Vector2(0.9f, 0.95f));

        WalletHUD walletHUD = walletGO.AddComponent<WalletHUD>();

        Debug.Log("[UITemplateGenerator] WalletHUD 已生成");
    }

    /// <summary>
    /// 仅生成背包
    /// </summary>
    private void GenerateInventoryOnly()
    {
        GameObject guiRoot = new GameObject("GUI");
        GameObject canvasGO = CreateCanvas(guiRoot);
        GenerateInventoryUI(canvasGO);

        Selection.activeGameObject = guiRoot;
        Debug.Log("[UITemplateGenerator] 仅InventoryUI已生成");
    }

    /// <summary>
    /// 仅生成快捷栏
    /// </summary>
    private void GenerateHotbarOnly()
    {
        GameObject guiRoot = new GameObject("GUI");
        GameObject canvasGO = CreateCanvas(guiRoot);
        GenerateHotbarUI(canvasGO);

        Selection.activeGameObject = guiRoot;
        Debug.Log("[UITemplateGenerator] 仅HotbarUI已生成");
    }

    /// <summary>
    /// 生成NPC对话系统
    /// </summary>
    private void GenerateNPCDialogSystem()
    {
        GameObject guiRoot = new GameObject("GUI");
        GameObject canvasGO = CreateCanvas(guiRoot);
        GenerateNPCDialogUI(canvasGO);

        Selection.activeGameObject = guiRoot;
        Debug.Log("[UITemplateGenerator] NPCDialogUI已生成");
    }

    /// <summary>
    /// 创建Canvas
    /// </summary>
    private GameObject CreateCanvas(GameObject parent)
    {
        GameObject canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(parent.transform);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        return canvasGO;
    }

    /// <summary>
    /// 添加RectTransform
    /// </summary>
    private void AddRectTransform(GameObject go, Vector2 size, Vector2 anchor)
    {
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// 分析当前场景的UI结构
    /// </summary>
    private void AnalyzeCurrentUIStructure()
    {
        GameObject[] uiObjects = GameObject.FindObjectsOfType<GameObject>(true);
        System.Text.StringBuilder info = new System.Text.StringBuilder();
        info.AppendLine("当前场景UI结构分析：\n");

        int canvasCount = 0;
        int inventoryCount = 0;
        int dialogCount = 0;
        int shopCount = 0;

        foreach (var obj in uiObjects)
        {
            if (obj.name == "Canvas") canvasCount++;
            if (obj.name == "InventoryUI" || obj.GetComponent<InventoryUI>()) inventoryCount++;
            if (obj.name == "NPCDialogUI" || obj.GetComponent<NPCDialogUI>()) dialogCount++;
            if (obj.name == "SimpleShopUI" || obj.GetComponent<SimpleShopUI>()) shopCount++;
        }

        info.AppendLine($"📊 统计信息：");
        info.AppendLine($"   Canvas 数量：{canvasCount}");
        info.AppendLine($"   InventoryUI 数量：{inventoryCount}");
        info.AppendLine($"   NPCDialogUI 数量：{dialogCount}");
        info.AppendLine($"   SimpleShopUI 数量：{shopCount}");

        GameObject gui = GameObject.Find("GUI");
        if (gui != null)
        {
            info.AppendLine($"\n✅ 找到 GUI 对象");
            info.AppendLine($"   子对象数量：{gui.transform.childCount}");

            info.AppendLine($"\n📋 GUI 子对象列表：");
            foreach (Transform child in gui.transform)
            {
                info.AppendLine($"   - {child.name}");
            }
        }
        else
        {
            info.AppendLine($"\n❌ 未找到 GUI 对象（建议生成）");
        }

        Debug.Log(info.ToString());
        EditorUtility.DisplayDialog("UI结构分析", info.ToString(), "关闭");
    }
}
