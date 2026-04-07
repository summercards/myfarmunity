// Assets/Scripts/UI/SimpleShopUI.cs
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SimpleShopUI - 稳健版
/// - 在合适时机(打开/Enable)自动构建商品列表
/// - 精确查找模板子物体 Icon/Name/Price 并填充
/// - 在关键点打印调试日志，便于排查
/// </summary>
public class SimpleShopUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("List")]
    public RectTransform contentRect;    // **应指向 Grid (RectTransform)**
    public GameObject itemTemplate;      // 模板（场景中应为 inactive）
    public ScrollRect scrollRect;

    [Header("Buttons")]
    public Button closeBtn;
    public Button buyModeBtn;
    public Button sellModeBtn;
    public Image buyModeBg;
    public Image sellModeBg;

    [Header("Text")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI coinsText;

    [Header("Data")]
    public ShopCatalogSO catalog;        // 拖入你的 SC_Simple

    public bool IsOpen { get; private set; }

    private bool built = false;
    private MiniShop miniShop; // 保留兼容（如果场景没有就为 null）

    void Awake()
    {
        miniShop = GetComponent<MiniShop>();

        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(Close);
        }

        // 如果设计上 root 在开始应为隐藏，则隐藏
        if (root != null && Application.isPlaying)
            root.SetActive(false);
    }

    void OnEnable()
    {
        RuntimeRefs.RegisterSimpleShopUI(this);

        // 如果在编辑器场景里手动激活 root，我们也尝试构建（仅在播放时）
        if (Application.isPlaying)
        {
            // 如果 UI 已经可见且还未构建，触发构建
            if (!built && root != null && root.activeInHierarchy)
            {
                Debug.Log("[Shop] OnEnable: root already active, BuildItems()");
                BuildItems();
            }
        }
    }

    void OnDisable()
    {
        RuntimeRefs.UnregisterSimpleShopUI(this);
    }

    /// <summary> 打开商店（外部调用） </summary>
    public void Open()
    {
        // 如果存在旧的 MiniShop 实现，仍保留委派（可删除此段以强制使用本实现）
        if (miniShop != null)
        {
            Debug.Log("[Shop] Delegating Open to MiniShop");
            miniShop.Open();
            IsOpen = miniShop.IsOpen;
            return;
        }

        if (root != null)
            root.SetActive(true);
        IsOpen = true;

        if (!built)
        {
            BuildItems();
        }
    }

    /// <summary> 关闭商店 </summary>
    public void Close()
    {
        if (miniShop != null)
        {
            miniShop.Close();
            IsOpen = miniShop.IsOpen;
            return;
        }

        if (root != null)
            root.SetActive(false);

        IsOpen = false;
    }

    /// <summary>
    /// 由 Open / OnEnable 触发：把 catalog 的条目实例化到 contentRect（Grid）下
    /// </summary>
    [ContextMenu("RebuildShopItems")]
    public void BuildItems()
    {
        if (!IsRuntimeSceneGameObject(gameObject))
        {
            Debug.LogWarning("[Shop] BuildItems aborted: SimpleShopUI is not a runtime scene instance.");
            return;
        }

        // 基础校验
        if (catalog == null)
        {
            Debug.LogWarning("[Shop] BuildItems aborted: catalog is null");
            return;
        }

        if (contentRect == null)
        {
            Debug.LogError("[Shop] BuildItems aborted: contentRect (Grid) is not assigned!");
            return;
        }

        if (!IsRuntimeSceneGameObject(contentRect.gameObject))
        {
            Debug.LogError("[Shop] BuildItems aborted: contentRect is not a runtime scene object.");
            return;
        }

        if (itemTemplate == null)
        {
            Debug.LogError("[Shop] BuildItems aborted: itemTemplate is not assigned!");
            return;
        }

        bool templateInScene = IsRuntimeSceneGameObject(itemTemplate);

        // 确保模板处于 inactive（避免被当作实际项）
        if (templateInScene && itemTemplate.activeSelf)
        {
            Debug.LogWarning("[Shop] itemTemplate is active in hierarchy — it should be inactive. Automatically deactivating.");
            itemTemplate.SetActive(false);
        }

        // 清理旧的克隆（保留 itemTemplate 本身）
        for (int i = contentRect.childCount - 1; i >= 0; i--)
        {
            var child = contentRect.GetChild(i).gameObject;
            if (!templateInScene || child != itemTemplate)
            {
                // 在编辑器下 DestroyImmediate 比 Destroy 更立即
#if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(child);
#else
                Destroy(child);
#endif
            }
        }

        // 读取 catalog 的条目并实例化
        int created = 0;
        try
        {
            // 假设 catalog.entries 是一个可枚举集合（与你的 SO 定义一致）
            foreach (var entry in catalog.entries)
            {
                // 实例化模板到 contentRect（Grid）
                var go = Instantiate(itemTemplate, contentRect, false);
                go.SetActive(true);

                // 填充 Name (查找子对象名为 "Name")
                var nameTf = go.transform.Find("Name");
                if (nameTf != null)
                {
                    var nameLabel = nameTf.GetComponent<TextMeshProUGUI>();
                    if (nameLabel != null)
                    {
                        // entry 结构假定含 displayName 字段/属性
                        nameLabel.text = entry.displayName;
                    }
                }
                else
                {
                    // 回退：查找任意 TextMeshProUGUI 并尝试设置
                    var txt = go.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null)
                        txt.text = entry.displayName;
                }

                // 填充 Price
                var priceTf = go.transform.Find("Price");
                if (priceTf != null)
                {
                    var priceLabel = priceTf.GetComponent<TextMeshProUGUI>();
                    if (priceLabel != null)
                        priceLabel.text = entry.buyPrice.ToString();
                }

                // 填充 Icon（查找名为 "Icon" 的子物体）
                var iconTf = go.transform.Find("Icon");
                if (iconTf != null)
                {
                    var iconImg = iconTf.GetComponent<Image>();
                    if (iconImg != null && entry.icon != null)
                        iconImg.sprite = entry.icon;
                }
                else
                {
                    // 如果没有专门的 Icon 子物体，尝试寻找 Image 并设置 sprite（谨慎）
                    var anyImg = go.GetComponentInChildren<Image>(true);
                    if (anyImg != null && entry.icon != null)
                        anyImg.sprite = entry.icon;
                }

                // 点击行为（演示用：输出日志）
                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    var captured = entry; // 捕获变量防闭包问题
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        // 找钱包
                        var wallet = RuntimeRefs.PlayerWallet;
                        if (wallet == null)
                        {
                            Debug.LogError("[Shop] 场景中没有 PlayerWallet");
                            return;
                        }

                        // 钱不够
                        if (!wallet.CanAfford(captured.buyPrice))
                        {
                            Debug.Log("[Shop] 金币不足");
                            return;
                        }

                        // 扣钱
                        if (!wallet.TrySpend(captured.buyPrice))
                        {
                            Debug.Log("[Shop] 扣费失败");
                            return;
                        }

                        // 找背包桥
                        var inv = RuntimeRefs.InventoryBridge;

                        // 找玩家位置（用于掉落兜底）
                        Transform player = RuntimeRefs.PlayerTransform;

                        bool added = false;
                        if (inv != null)
                        {
                            added = inv.TryAdd(
                                captured.itemId,
                                1,
                                player,
                                captured.pickupPrefab
                            );
                        }

                        if (added)
                            Debug.Log("[Shop] 购买成功: " + captured.displayName);
                        else
                            Debug.LogWarning("[Shop] 已扣钱但未加入背包");
                    });
                }

                created++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[Shop] BuildItems exception: " + ex);
        }

        // 强制刷新布局以便 ContentSizeFitter / Grid 生效
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        built = true;
        Debug.LogFormat("[Shop] BuildItems finished. Created {0} items (catalog: {1})", created, catalog != null ? catalog.name : "null");
    }

    // 提供给调试用：强制重新生成（运行时右键组件菜单或在编辑器 Inspector 的三点下调用）
    [ContextMenu("ForceRebuild")]
    public void ForceRebuild()
    {
        built = false;
        BuildItems();
    }

    private static bool IsRuntimeSceneGameObject(GameObject obj)
    {
        return obj != null && obj.scene.IsValid();
    }
}
