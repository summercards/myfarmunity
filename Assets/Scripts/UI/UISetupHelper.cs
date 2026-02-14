// Assets/Scripts/UI/UISetupHelper.cs
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI设置助手
/// 在场景加载时自动生成UI系统，确保每个场景都有完整的UI
/// </summary>
public class UISetupHelper : MonoBehaviour
{
    [Header("自动生成选项")]
    [Tooltip("在Start时自动生成UI（如果场景中没有GUI）")]
    public bool autoGenerateOnStart = true;

    [Header("UI组件开关")]
    [Tooltip("生成背包UI")]
    public bool createInventoryUI = true;

    [Tooltip("生成快捷栏UI")]
    public bool createHotbarUI = true;

    [Tooltip("生成角色属性UI")]
    public bool createCharacterStatsUI = true;

    [Tooltip("生成NPC对话框UI")]
    public bool createNPCDialogUI = true;

    [Tooltip("生成商店UI")]
    public bool createShopUI = true;

    [Tooltip("生成拾取提示HUD")]
    public bool createPickupHUD = true;

    [Tooltip("生成钱包HUD")]
    public bool createWalletHUD = true;

    private void Start()
    {
        if (autoGenerateOnStart)
        {
            CheckAndGenerateUI();
        }
    }

    /// <summary>
    /// 检查场景中是否有GUI，如果没有则自动生成
    /// </summary>
    public void CheckAndGenerateUI()
    {
        GameObject existingGUI = GameObject.Find("GUI");

        if (existingGUI != null)
        {
            Debug.Log("[UISetupHelper] 场景中已有GUI，跳过生成");
            // 确保所有UI组件都存在
            EnsureAllUIComponentsExist(existingGUI);
            return;
        }

        Debug.Log("[UISetupHelper] 场景中没有GUI，开始自动生成...");
        GenerateFullUISystem();
    }

    /// <summary>
    /// 确保所有UI组件都存在
    /// </summary>
    private void EnsureAllUIComponentsExist(GameObject guiRoot)
    {
        // 检查Canvas
        Canvas canvas = guiRoot.GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[UISetupHelper] GUI中没有Canvas，建议重新生成");
            return;
        }

        // 检查各个UI组件
        EnsureUIComponentExists<InventoryUI>(canvas.transform, "InventoryUI");
        EnsureUIComponentExists<NPCDialogUI>(canvas.transform, "NPCDialogUI");
        EnsureUIComponentExists<SimpleShopUI>(canvas.transform, "SimpleShopUI");
        EnsureUIComponentExists<CharacterStatsUI>(canvas.transform, "CharacterStatsUI");
        EnsureUIComponentExists<PickupHUD>(canvas.transform, "PickupHUD");
        EnsureUIComponentExists<WalletHUD>(canvas.transform, "WalletHUD");

        Debug.Log("[UISetupHelper] UI组件检查完成");
    }

    /// <summary>
    /// 确保特定UI组件存在
    /// </summary>
    private void EnsureUIComponentExists<T>(Transform parent, string componentName) where T : Component
    {
        T existingComponent = parent.GetComponentInChildren<T>(true);
        if (existingComponent == null)
        {
            Debug.LogWarning($"[UISetupHelper] 缺少 {componentName}，建议重新生成UI");
        }
    }

    /// <summary>
    /// 生成完整UI系统
    /// </summary>
    public void GenerateFullUISystem()
    {
        GameObject guiRoot = new GameObject("GUI");
        GameObject canvasGO = CreateCanvas(guiRoot);

        Debug.Log("[UISetupHelper] Canvas已创建");

        // 生成各个UI组件
        if (createInventoryUI)
        {
            GenerateInventoryUI(canvasGO);
        }

        if (createHotbarUI)
        {
            GenerateHotbarUI(canvasGO);
        }

        if (createCharacterStatsUI)
        {
            GenerateCharacterStatsUI(canvasGO);
        }

        if (createNPCDialogUI)
        {
            GenerateNPCDialogUI(canvasGO);
        }

        if (createShopUI)
        {
            GenerateShopUI(canvasGO);
        }

        if (createPickupHUD)
        {
            GeneratePickupHUD(canvasGO);
        }

        if (createWalletHUD)
        {
            GenerateWalletHUD(canvasGO);
        }

        Debug.Log("[UISetupHelper] 完整UI系统已生成！");

        // 5秒后销毁自己（不再需要）
        Destroy(gameObject, 5f);
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

        Debug.Log("[UISetupHelper] InventoryUI已生成");
    }

    /// <summary>
    /// 生成快捷栏UI
    /// </summary>
    private void GenerateHotbarUI(GameObject canvas)
    {
        GameObject hotbarGO = new GameObject("HotbarUI");
        hotbarGO.transform.SetParent(canvas.transform, false);
        AddRectTransform(hotbarGO, new Vector2(400, 80), new Vector2(0.5f, 0.05f));

        Debug.Log("[UISetupHelper] HotbarUI已生成");
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

        Debug.Log("[UISetupHelper] CharacterStatsUI已生成");
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

        Debug.Log("[UISetupHelper] NPCDialogUI已生成");
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

        Debug.Log("[UISetupHelper] SimpleShopUI已生成");
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

        Debug.Log("[UISetupHelper] PickupHUD已生成");
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

        Debug.Log("[UISetupHelper] WalletHUD已生成");
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
    private RectTransform AddRectTransform(GameObject go, Vector2 size, Vector2 anchor)
    {
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;

        return rect;
    }
}
