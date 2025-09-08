using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 尝试“自动适配”你项目里的背包：
/// - 在场景中查找名为 PlayerInventoryHolder 的组件（或手动拖到 holderOverride）
/// - 通过反射寻找 inventory 字段/属性与 Add/Remove/GetCount 等方法（常见命名都尝试）
/// - 若无法直接调用背包API，则在购买时用 pickupPrefab 兜底生成到玩家脚边（玩家自行拾取）
/// </summary>
public class InventoryBridge : MonoBehaviour
{
    [Header("定位背包对象")]
    [Tooltip("优先使用这里指定的背包持有者；为空则自动 FindObjectOfType(\"PlayerInventoryHolder\").")]
    public MonoBehaviour holderOverride;

    [Tooltip("优先尝试从持有者上名为 inventory / Inventory 的字段或属性取背包对象。为空会自动尝试常见名称。")]
    public string inventoryMemberName = ""; // 为空走自动

    [Header("物品ID → 背包API所需对象（可选）")]
    [Tooltip("有些背包API需要传 ScriptableObject 或 Item 对象，这里可手动配置映射。")]
    public List<ItemMapping> manualMapping = new();

    [Serializable]
    public class ItemMapping
    {
        public string itemId;
        public UnityEngine.Object itemObject; // 可是你的 ItemSO / Item / 任意背包所需对象
    }

    object _holder;      // PlayerInventoryHolder 实例
    object _inventory;   // 真实背包对象（可能在 holder 内）
    MethodInfo _miAdd, _miRemove, _miCount;

    void Awake()
    {
        // 1) 找 holder
        if (holderOverride) _holder = holderOverride;
        if (_holder == null)
        {
            // 尝试按类型名查找
            var t = FindTypeByName("PlayerInventoryHolder");
            if (t != null)
            {
                var comp = FindObjectOfType(t) as Component;
                if (comp) _holder = comp;
            }
        }
        if (_holder == null)
        {
            Debug.LogWarning("[InventoryBridge] 未找到 PlayerInventoryHolder（可在 holderOverride 手动指定）。将使用拾取物体生成作为兜底。");
            return;
        }

        // 2) 找 inventory 对象（字段或属性）
        _inventory = ResolveInventoryObject(_holder);
        if (_inventory == null)
        {
            Debug.LogWarning("[InventoryBridge] 未能解析 holder 内的背包对象（inventory）。");
            return;
        }

        // 3) 绑定方法
        BindMethods();
    }

    public int GetCount(string itemId)
    {
        if (_inventory == null || _miCount == null) return 0;

        var (arg, expectsString) = ResolveItemArg(itemId, _miCount);
        try
        {
            var ret = _miCount.Invoke(_inventory, expectsString ? new object[] { itemId } : new object[] { arg });
            if (ret is int i) return i;
        }
        catch { }
        return 0;
    }

    /// <summary> 尝试添加；失败时若给了 pickupPrefab，则在玩家脚边生成对应数量的拾取预制体（并返回 true表示已购成功）。 </summary>
    public bool TryAdd(string itemId, int amount, Transform player, GameObject pickupPrefab)
    {
        if (amount <= 0) return true;

        if (_inventory != null && _miAdd != null)
        {
            var (arg, expectsString) = ResolveItemArg(itemId, _miAdd);
            try
            {
                // 常见签名：(object,int) / (string,int) / (object) / (string)
                var pars = _miAdd.GetParameters();
                if (pars.Length == 2)
                {
                    _miAdd.Invoke(_inventory, expectsString ? new object[] { itemId, amount } : new object[] { arg, amount });
                    return true;
                }
                else if (pars.Length == 1)
                {
                    for (int i = 0; i < amount; i++)
                        _miAdd.Invoke(_inventory, expectsString ? new object[] { itemId } : new object[] { arg });
                    return true;
                }
            }
            catch { }
        }

        // 兜底：生成拾取预制体
        if (pickupPrefab && player)
        {
            for (int i = 0; i < amount; i++)
            {
                var pos = player.position + player.forward * 0.6f + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.2f;
                GameObject.Instantiate(pickupPrefab, pos, Quaternion.identity);
            }
            Debug.Log("[InventoryBridge] 无法直加到背包，已生成拾取物体作为兜底。");
            return true;
        }

        Debug.LogWarning("[InventoryBridge] TryAdd 失败，且没有可用兜底。");
        return false;
    }

    /// <summary> 尝试移除；移除失败则返回 false（不做兜底）。 </summary>
    public bool TryRemove(string itemId, int amount)
    {
        if (amount <= 0) return true;

        if (_inventory != null && _miRemove != null)
        {
            var (arg, expectsString) = ResolveItemArg(itemId, _miRemove);
            try
            {
                var pars = _miRemove.GetParameters();
                if (pars.Length == 2)
                {
                    _miRemove.Invoke(_inventory, expectsString ? new object[] { itemId, amount } : new object[] { arg, amount });
                    return true;
                }
                else if (pars.Length == 1)
                {
                    for (int i = 0; i < amount; i++)
                        _miRemove.Invoke(_inventory, expectsString ? new object[] { itemId } : new object[] { arg });
                    return true;
                }
            }
            catch { }
        }

        Debug.LogWarning("[InventoryBridge] TryRemove 失败，未找到兼容的移除API。");
        return false;
    }

    // --------- 反射辅助 ---------
    (object arg, bool expectsString) ResolveItemArg(string itemId, MethodInfo mi)
    {
        var ps = mi.GetParameters();
        var argType = ps[0].ParameterType;
        // string
        if (argType == typeof(string)) return (itemId, true);
        // 其它类型：尝试用手动映射
        var map = manualMapping.FirstOrDefault(m => m.itemId == itemId);
        if (map != null && map.itemObject != null) return (map.itemObject, false);
        return (null, false);
    }

    void BindMethods()
    {
        var invType = _inventory.GetType();

        // 搜常见方法名
        _miAdd = FindMethod(invType, new[] { "AddItem", "TryAddItem", "Add", "AddById" });
        _miRemove = FindMethod(invType, new[] { "RemoveItem", "TryRemoveItem", "Remove", "RemoveById" });
        _miCount = FindMethod(invType, new[] { "GetItemCount", "GetCount", "CountOf" });

        // 要求第一个参数是 物品/ID，第二个可能是数量
        // 若找不到不用强求，走兜底
        if (_miAdd == null) Debug.Log("[InventoryBridge] 未找到 Add 方法（将可能走兜底生成）。");
        if (_miRemove == null) Debug.Log("[InventoryBridge] 未找到 Remove 方法。");
        if (_miCount == null) Debug.Log("[InventoryBridge] 未找到 GetCount 方法。");
    }

    MethodInfo FindMethod(Type t, string[] names)
    {
        foreach (var n in names)
        {
            var mis = t.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var mi in mis)
            {
                if (mi.Name != n) continue;
                var ps = mi.GetParameters();
                if (ps.Length == 1 || ps.Length == 2) return mi;
            }
        }
        return null;
    }

    object ResolveInventoryObject(object holderObj)
    {
        if (holderObj == null) return null;
        var ht = holderObj.GetType();

        // 1) 如果本身就有常见方法，也可直接当 inventory 用
        if (FindMethod(ht, new[] { "AddItem", "Add", "TryAddItem" }) != null)
            return holderObj;

        // 2) 字段/属性
        string[] names = string.IsNullOrEmpty(inventoryMemberName)
            ? new[] { "inventory", "Inventory", "playerInventory", "PlayerInventory" }
            : new[] { inventoryMemberName };

        foreach (var n in names)
        {
            var f = ht.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) return f.GetValue(holderObj);

            var p = ht.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null) return p.GetValue(holderObj);
        }

        return null;
    }

    Component FindObjectOfType(Type type)
    {
        var arr = UnityEngine.Object.FindObjectsOfType<Component>();
        foreach (var c in arr) if (c && c.GetType() == type) return c;
        return null;
    }

    Type FindTypeByName(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName);
            if (t != null) return t;
        }
        return null;
    }
}
