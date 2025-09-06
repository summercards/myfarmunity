using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 简易背包面板：
/// - 自动按容量生成格子
/// - 显示图标/数量（数量=1也显示）
/// - 左键点击切换激活物品
/// - I 键开/关（可禁用）
/// 依赖：PlayerInventoryHolder、ActiveItemController、ItemDatabaseSO
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerInventoryHolder playerInv;      // Player 身上的 PlayerInventoryHolder
    public ActiveItemController activeCtrl;     // Player 身上的 ActiveItemController
    public ItemDatabaseSO itemDB;         // 一般从 playerInv.itemDB 取

    [Header("UI")]
    public GameObject panelRoot;                 // 背包面板根节点（整体开/关）
    public Transform gridRoot;                  // 放格子的父物体（带 GridLayoutGroup）
    public InventorySlotUI slotPrefab;           // 槽位预制
    public Sprite emptySprite;                   // 空格占位图（可空，建议用透明1x1）
    [Range(0f, 1f)] public float emptyIconAlpha = 0.15f;

    [Header("Options")]
    public bool buildOnAwake = true;             // 启动时构建格子
    public bool toggleWithKey = true;            // 用按键切换
    public KeyCode toggleKey = KeyCode.I;

    private readonly List<InventorySlotUI> _slots = new();

    void Reset()
    {
        if (!playerInv) playerInv = FindObjectOfType<PlayerInventoryHolder>();
        if (!activeCtrl) activeCtrl = FindObjectOfType<ActiveItemController>();
        if (!itemDB && playerInv) itemDB = playerInv.itemDB;
    }

    void Awake()
    {
        if (!itemDB && playerInv) itemDB = playerInv.itemDB;
        if (buildOnAwake) BuildSlots();
        RefreshAll();
    }

    void OnEnable()
    {
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
        if (toggleWithKey && Input.GetKeyDown(toggleKey))
            TogglePanel();
    }

    public void TogglePanel()
    {
        if (!panelRoot) return;
        panelRoot.SetActive(!panelRoot.activeSelf);
        if (panelRoot.activeSelf) RefreshAll();
    }

    /// <summary>根据当前背包容量构建槽位UI</summary>
    public void BuildSlots()
    {
        _slots.Clear();
        if (!gridRoot || !slotPrefab) return;

        // 清空旧子物体
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

    /// <summary>全量刷新（容量变化时会重建）</summary>
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

    /// <summary>根据物品ID解析图标；若数据库或字段缺失，返回 null</summary>
    public Sprite ResolveIcon(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var so = (itemDB != null) ? itemDB.Get(id) : null;   // 若方法名不是 Get，请按你的 ItemDB 改名
        if (so == null) return null;

        // ★ 如果 ItemSO 的图标字段不是 "icon"，把下面这行的 "icon" 改成你的字段名
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
