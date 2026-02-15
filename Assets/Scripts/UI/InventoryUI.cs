using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 背包UI控制器：
/// - 自动生成格子（基于容量）
/// - 显示图标/数量（数量=1也显示）
/// - 点击格子切换当前手持物品
/// - I 键开关/关闭（可配置）
/// 依赖 PlayerInventoryHolder、ActiveItemController、ItemDatabaseSO
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerInventoryHolder playerInv;      // Player 对象上的 PlayerInventoryHolder
    public ActiveItemController activeCtrl;     // Player 对象上的 ActiveItemController
    public ItemDatabaseSO itemDB;         // 一般从 playerInv.itemDB 获取

    [Header("UI")]
    public GameObject panelRoot;                 // 面板根节点（用于开/关闭）
    public Transform gridRoot;                  // 放格子的父容器（带 GridLayoutGroup）
    public InventorySlotUI slotPrefab;           // 格子预制体
    public Sprite emptySprite;                   // 空格子图标（可为空，或透明1x1）
    [Range(0f, 1f)] public float emptyIconAlpha = 0.15f;

    [Header("Options")]
    public bool buildOnAwake = true;             // 启动时自动构建
    public bool toggleWithKey = true;            // 允许按键切换
    public KeyCode toggleKey = KeyCode.I;

    private readonly List<InventorySlotUI> _slots = new();

    void Reset()
    {
        // Reset 时尝试动态查找
        if (!playerInv) playerInv = PlayerInventoryHolder.Instance;
        if (!playerInv) playerInv = FindObjectOfType<PlayerInventoryHolder>();
        if (!activeCtrl) activeCtrl = FindObjectOfType<ActiveItemController>();
        if (!itemDB && playerInv) itemDB = playerInv.itemDB;
    }

    void Awake()
    {
        // 尝试从单例获取
        if (!playerInv) playerInv = PlayerInventoryHolder.Instance;
        if (!playerInv) playerInv = FindObjectOfType<PlayerInventoryHolder>();

        if (!itemDB && playerInv) itemDB = playerInv.itemDB;

        if (buildOnAwake) BuildSlots();
        RefreshAll();
    }

    void OnEnable()
    {
        // 确保引用正确
        EnsureReferences();

        if (playerInv != null) playerInv.OnInventoryChanged += RefreshAll;
        if (activeCtrl != null) activeCtrl.OnActiveChanged += _ => RefreshAll();
        RefreshAll();
    }

    void OnDisable()
    {
        if (playerInv != null) playerInv.OnInventoryChanged -= RefreshAll;
        if (activeCtrl != null) activeCtrl.OnActiveChanged -= _ => RefreshAll();
    }

    void Update()
    {
        // 动态查找 PlayerInventoryHolder（确保引用正确）
        EnsureReferences();

        if (toggleWithKey && Input.GetKeyDown(toggleKey))
            TogglePanel();
    }

    /// <summary>
    /// 确保所有引用都正确连接
    /// 场景切换或首次加载时调用
    /// </summary>
    private void EnsureReferences()
    {
        bool changed = false;

        // 检查 playerInv
        if (playerInv == null || playerInv.gameObject == null)
        {
            var prevInv = playerInv;
            playerInv = PlayerInventoryHolder.Instance;
            if (!playerInv) playerInv = FindObjectOfType<PlayerInventoryHolder>();

            if (prevInv != playerInv && playerInv != null)
            {
                Debug.Log($"[InventoryUI] 重新绑定 PlayerInventoryHolder");
                changed = true;

                // 重新订阅事件
                if (isActiveAndEnabled)
                {
                    if (prevInv != null) prevInv.OnInventoryChanged -= RefreshAll;
                    playerInv.OnInventoryChanged += RefreshAll;
                }
            }
        }

        // 检查 activeCtrl
        if (activeCtrl == null || (activeCtrl as MonoBehaviour) == null ||
            (activeCtrl as MonoBehaviour).gameObject == null)
        {
            activeCtrl = FindObjectOfType<ActiveItemController>();
            if (activeCtrl != null && isActiveAndEnabled)
            {
                Debug.Log($"[InventoryUI] 重新绑定 ActiveItemController");
                changed = true;
            }
        }

        // 检查 itemDB
        if (!itemDB && playerInv) itemDB = playerInv.itemDB;

        // 如果引用发生变化，刷新UI
        if (changed && panelRoot != null && panelRoot.activeInHierarchy)
        {
            RefreshAll();
        }
    }

    public void TogglePanel()
    {
        if (!panelRoot) return;
        panelRoot.SetActive(!panelRoot.activeSelf);
        if (panelRoot.activeSelf) RefreshAll();
    }

    /// <summary>根据当前容量重新构建格子UI</summary>
    public void BuildSlots()
    {
        _slots.Clear();
        if (!gridRoot || !slotPrefab) return;

        // 清空旧格子
        for (int i = gridRoot.childCount - 1; i >= 0; i--)
            Destroy(gridRoot.GetChild(i).gameObject);

        int capacity = GetCapacity();
        for (int i = 0; i < capacity; i++)
        {
            var slot = Instantiate(slotPrefab, gridRoot);
            slot.Setup(this, i);
            _slots.Add(slot);
        }
    }

    /// <summary>全部刷新：数据变化时调用，或重新构建</summary>
    public void RefreshAll()
    {
        if (!_isPanelVisible()) return;

        if (_slots.Count != GetCapacity())
            BuildSlots();

        for (int i = 0; i < _slots.Count; i++)
            _slots[i].Refresh();
    }

    public ItemStack GetStack(int index)
    {
        if (playerInv == null || playerInv.Inventory == null || playerInv.Inventory.slots == null) return null;
        if (index < 0 || index >= playerInv.Inventory.slots.Length) return null;
        return playerInv.Inventory.slots[index];
    }

    public bool IsActiveId(string id)
    {
        return activeCtrl != null && !string.IsNullOrEmpty(id) && activeCtrl.ActiveId == id;
    }

    public void OnSlotLeftClick(int index)
    {
        var s = GetStack(index);
        if (s == null || string.IsNullOrEmpty(s.id) || s.count <= 0) return;
        if (activeCtrl != null) activeCtrl.SetActive(s.id, prefer: true);
        RefreshAll();
    }

    /// <summary>根据物品ID解析图标；若数据库缺失字段则返回 null</summary>
    public Sprite ResolveIcon(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var so = (itemDB != null) ? itemDB.Get(id) : null;   // 你的 ItemDB 如果有 Get 方法就调用
        if (so == null) return null;

        // 这里假设 ItemSO 上的图标字段名为 "icon"，如果你的字段名不同，请替换
        return so.icon;
    }

    private int GetCapacity()
    {
        if (playerInv == null || playerInv.Inventory == null || playerInv.Inventory.slots == null) return 0;
        return playerInv.Inventory.slots.Length;
    }

    private bool _isPanelVisible()
    {
        return panelRoot == null || panelRoot.activeInHierarchy;
    }
}
