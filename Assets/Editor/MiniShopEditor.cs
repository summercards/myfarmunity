using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MiniShop))]
public class MiniShopEditor : Editor
{
    // 根与布局
    SerializedProperty root, gridParent, cellTemplate;

    // 顶部
    SerializedProperty closeButton, walletTextTMP, walletTextUGUI;

    // 数据
    SerializedProperty catalog, wallet, inventoryBridge;

    // 对话（可选）
    SerializedProperty dialogUI;

    // 走开自动关闭
    SerializedProperty autoCloseWhenFar, closeDistance, autoCloseGrace, npcMask;

    // 列表选项 & 台词
    SerializedProperty showOnlyBuyable, shopOpenLine;

    // 手动距离关闭
    SerializedProperty manualDistanceClose, manualPlayer, manualNpc, manualCloseDistance;

    // 网格布局
    SerializedProperty useGridLayout, columns, rows, cellSize, spacing;
    SerializedProperty paddingLeft, paddingTop, paddingRight, paddingBottom;

    // 列表底板
    SerializedProperty listBackground, bgExtraPadding;

    void OnEnable()
    {
        // 根与布局
        root = serializedObject.FindProperty("root");
        gridParent = serializedObject.FindProperty("gridParent");
        cellTemplate = serializedObject.FindProperty("cellTemplate");

        // 顶部
        closeButton = serializedObject.FindProperty("closeButton");
        walletTextTMP = serializedObject.FindProperty("walletTextTMP");
        walletTextUGUI = serializedObject.FindProperty("walletTextUGUI");

        // 数据
        catalog = serializedObject.FindProperty("catalog");
        wallet = serializedObject.FindProperty("wallet");
        inventoryBridge = serializedObject.FindProperty("inventoryBridge");

        // 对话（可选）
        dialogUI = serializedObject.FindProperty("dialogUI");

        // 走开自动关闭
        autoCloseWhenFar = serializedObject.FindProperty("autoCloseWhenFar");
        closeDistance = serializedObject.FindProperty("closeDistance");
        autoCloseGrace = serializedObject.FindProperty("autoCloseGrace");
        npcMask = serializedObject.FindProperty("npcMask");

        // 列表选项 & 台词
        showOnlyBuyable = serializedObject.FindProperty("showOnlyBuyable");
        shopOpenLine = serializedObject.FindProperty("shopOpenLine");

        // 手动距离关闭
        manualDistanceClose = serializedObject.FindProperty("manualDistanceClose");
        manualPlayer = serializedObject.FindProperty("manualPlayer");
        manualNpc = serializedObject.FindProperty("manualNpc");
        manualCloseDistance = serializedObject.FindProperty("manualCloseDistance");

        // 网格布局
        useGridLayout = serializedObject.FindProperty("useGridLayout");
        columns = serializedObject.FindProperty("columns");
        rows = serializedObject.FindProperty("rows");
        cellSize = serializedObject.FindProperty("cellSize");
        spacing = serializedObject.FindProperty("spacing");
        paddingLeft = serializedObject.FindProperty("paddingLeft");
        paddingTop = serializedObject.FindProperty("paddingTop");
        paddingRight = serializedObject.FindProperty("paddingRight");
        paddingBottom = serializedObject.FindProperty("paddingBottom");

        // 底板
        listBackground = serializedObject.FindProperty("listBackground");
        bgExtraPadding = serializedObject.FindProperty("bgExtraPadding");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader("根节点与布局");
        Prop(root, "根节点 Root（整个商店面板）");
        Prop(gridParent, "商品容器 Grid Parent（列表父物体）");
        Prop(cellTemplate, "商品卡片模板 Cell Template（需隐藏）");

        Space();

        DrawHeader("顶部栏");
        Prop(closeButton, "关闭按钮");
        Prop(walletTextTMP, "金币文本（TMP 可选）");
        Prop(walletTextUGUI, "金币文本（UGUI 可选）");

        Space();

        DrawHeader("数据");
        Prop(catalog, "商店目录（ShopCatalogSO）");
        Prop(wallet, "玩家钱包（PlayerWallet）");
        Prop(inventoryBridge, "背包桥（InventoryBridge）");

        Space();

        DrawHeader("对话（可选）");
        Prop(dialogUI, "NPC 对话 UI（不需要可留空）");

        Space();

        DrawHeader("走开自动关闭");
        Prop(autoCloseWhenFar, "启用（玩家远离自动关闭）");
        Indent(() => {
            Prop(closeDistance, "关闭距离（米）");
            Prop(autoCloseGrace, "开启后缓冲时长（秒）");
            Prop(npcMask, "NPC 图层掩码（用于就近查找）");
        });

        Space();

        DrawHeader("商品列表选项");
        Prop(showOnlyBuyable, "只显示可购买的条目（buyPrice>0）");

        Space();

        DrawHeader("商店开场台词");
        EditorGUILayout.PropertyField(shopOpenLine, new GUIContent("台词内容（可在从对话打开时覆盖）"), true);

        Space();

        DrawHeader("手动距离关闭（简单粗暴）");
        Prop(manualDistanceClose, "启用（只看这两个对象的距离）");
        Indent(() => {
            Prop(manualPlayer, "玩家 Transform");
            Prop(manualNpc, "NPC / BubbleAnchor Transform");
            Prop(manualCloseDistance, "关闭距离（米）");
        });

        Space();

        DrawHeader("网格布局");
        Prop(useGridLayout, "启用网格布局（自动添加/配置 GridLayoutGroup）");
        Indent(() => {
            Prop(columns, "列数（优先使用）");
            Prop(rows, "行数（列数为 0 时生效）");
            Prop(cellSize, "卡片尺寸（宽, 高）");
            Prop(spacing, "间距（水平, 垂直）");
            Prop(paddingLeft, "内边距 左");
            Prop(paddingTop, "内边距 上");
            Prop(paddingRight, "内边距 右");
            Prop(paddingBottom, "内边距 下");
        });

        Space();

        DrawHeader("列表底板");
        Prop(listBackground, "底板 RectTransform（可为空）");
        Prop(bgExtraPadding, "额外边距（宽, 高）");

        serializedObject.ApplyModifiedProperties();
    }

    // ---------- helpers ----------
    void DrawHeader(string title)
    {
        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }
    void Prop(SerializedProperty p, string label)
    {
        if (p == null) return;
        EditorGUILayout.PropertyField(p, new GUIContent(label), true);
    }
    void Space(float h = 6f) => GUILayout.Space(h);
    void Indent(System.Action draw)
    {
        EditorGUI.indentLevel++;
        draw?.Invoke();
        EditorGUI.indentLevel--;
    }
}
