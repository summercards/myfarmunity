using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// 商店预制体生成工具（修正版 - 与 SimpleShopUI/MiniShop 完全兼容）
/// </summary>
public class ShopUIPrefabGenerator
{
    [MenuItem("Tools/Shop UI/Generate Shop Prefab")]
    public static void GenerateShopPrefab()
    {
        string folderPath = "Assets/Prefabs/UI/";
        string prefabName = "ShopUI.prefab";
        string fullPath = folderPath + prefabName;

    if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        GameObject root = new GameObject("ShopUI", typeof(RectTransform));
        var shopUI = root.AddComponent<SimpleShopUI>();

        CreateUIHierarchy(root, shopUI);

        PrefabUtility.SaveAsPrefabAsset(root, fullPath);
        GameObject.DestroyImmediate(root);

        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);

        Debug.Log("[ShopUIPrefabGenerator] 商店预制体已生成：" + fullPath);
    }

    private static GameObject UI(string name, Transform parent, params System.Type[] extra)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        foreach (var t in extra) go.AddComponent(t);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void CreateUIHierarchy(GameObject root, SimpleShopUI shopUI)
    {
        // Canvas
        var canvasGO = UI("Canvas", root.transform, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Main Panel
        var panel = UI("MainPanel", canvasGO.transform, typeof(Image));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(700, 500);
        panel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.92f);
        shopUI.root = panel;

        // ===== HEADER =====
        var header = UI("Header", panel.transform);
        var headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.sizeDelta = new Vector2(0, 60);
        headerRect.anchoredPosition = new Vector2(0, -10);

        // Title
        var titleGO = UI("Title", header.transform, typeof(TextMeshProUGUI));
        var title = titleGO.GetComponent<TextMeshProUGUI>();
        title.text = "商店";
        title.fontSize = 26;
        title.alignment = TextAlignmentOptions.Left;
        title.color = new Color(1f, .85f, .3f);
        title.rectTransform.anchoredPosition = new Vector2(80, -25);
        shopUI.titleText = title;

        // Close
        var closeGO = UI("CloseButton", header.transform, typeof(Image), typeof(Button));
        var closeRect = closeGO.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 0.5f);
        closeRect.anchorMax = new Vector2(1, 0.5f);
        closeRect.sizeDelta = new Vector2(80, 35);
        closeRect.anchoredPosition = new Vector2(-60, -25);
        closeGO.GetComponent<Image>().color = new Color(.8f, .3f, .3f);
        shopUI.closeBtn = closeGO.GetComponent<Button>();

        var closeText = UI("Text", closeGO.transform, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        closeText.text = "关闭";
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.rectTransform.sizeDelta = Vector2.zero;

        // ===== SCROLL =====
        var scrollGO = UI("ScrollRect", panel.transform, typeof(Image), typeof(ScrollRect));
        var scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.03f);
        shopUI.scrollRect = scrollRect;

        var scrollRectT = scrollGO.GetComponent<RectTransform>();
        scrollRectT.anchorMin = new Vector2(0, 0);
        scrollRectT.anchorMax = new Vector2(1, 1);
        scrollRectT.offsetMin = new Vector2(20, 80);
        scrollRectT.offsetMax = new Vector2(-20, -80);

        // Viewport
        var viewport = UI("Viewport", scrollGO.transform, typeof(Image), typeof(Mask));
        viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;

        // Content
        var content = UI("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        shopUI.contentRect = contentRect;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;

        // Grid
        var gridGO = UI("Grid", content.transform, typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        var grid = gridGO.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(110, 110);
        grid.spacing = new Vector2(10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        var fitter = gridGO.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ===== ITEM TEMPLATE =====
        var item = UI("ItemTemplate", gridGO.transform, typeof(Image), typeof(Button));
        item.GetComponent<Image>().color = new Color(.95f, .95f, .95f, 1f);
        item.SetActive(false);
        shopUI.itemTemplate = item;

        // icon
        var icon = UI("Icon", item.transform, typeof(Image)).GetComponent<Image>();
        icon.rectTransform.sizeDelta = new Vector2(64, 64);
        icon.rectTransform.anchoredPosition = new Vector2(0, 15);

        // name
        var name = UI("Name", item.transform, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        name.alignment = TextAlignmentOptions.Center;
        name.rectTransform.anchoredPosition = new Vector2(0, -20);

        // price
        var price = UI("Price", item.transform, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        price.alignment = TextAlignmentOptions.Center;
        price.rectTransform.anchoredPosition = new Vector2(0, -40);

        // ===== BOTTOM BAR =====
        var bottom = UI("BottomBar", panel.transform);
        var bRect = bottom.GetComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0, 0);
        bRect.anchorMax = new Vector2(1, 0);
        bRect.sizeDelta = new Vector2(0, 70);

        var buy = UI("Buy", bottom.transform, typeof(Image), typeof(Button));
        buy.GetComponent<Image>().color = new Color(.3f, .8f, .3f);
        shopUI.buyModeBtn = buy.GetComponent<Button>();
        shopUI.buyModeBg = buy.GetComponent<Image>();

        var sell = UI("Sell", bottom.transform, typeof(Image), typeof(Button));
        sell.GetComponent<Image>().color = Color.white;
        shopUI.sellModeBtn = sell.GetComponent<Button>();
        shopUI.sellModeBg = sell.GetComponent<Image>();

        var coins = UI("Coins", bottom.transform, typeof(Image), typeof(TextMeshProUGUI));
        coins.GetComponent<Image>().color = new Color(.2f, .2f, .2f, 1f);
        shopUI.coinsText = coins.GetComponent<TextMeshProUGUI>();

        EditorUtility.SetDirty(root);
    }

}
