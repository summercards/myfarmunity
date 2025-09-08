using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// ���ԡ��Զ����䡱����Ŀ��ı�����
/// - �ڳ����в�����Ϊ PlayerInventoryHolder ����������ֶ��ϵ� holderOverride��
/// - ͨ������Ѱ�� inventory �ֶ�/������ Add/Remove/GetCount �ȷ������������������ԣ�
/// - ���޷�ֱ�ӵ��ñ���API�����ڹ���ʱ�� pickupPrefab �������ɵ���ҽűߣ��������ʰȡ��
/// </summary>
public class InventoryBridge : MonoBehaviour
{
    [Header("��λ��������")]
    [Tooltip("����ʹ������ָ���ı��������ߣ�Ϊ�����Զ� FindObjectOfType(\"PlayerInventoryHolder\").")]
    public MonoBehaviour holderOverride;

    [Tooltip("���ȳ��Դӳ���������Ϊ inventory / Inventory ���ֶλ�����ȡ��������Ϊ�ջ��Զ����Գ������ơ�")]
    public string inventoryMemberName = ""; // Ϊ�����Զ�

    [Header("��ƷID �� ����API������󣨿�ѡ��")]
    [Tooltip("��Щ����API��Ҫ�� ScriptableObject �� Item ����������ֶ�����ӳ�䡣")]
    public List<ItemMapping> manualMapping = new();

    [Serializable]
    public class ItemMapping
    {
        public string itemId;
        public UnityEngine.Object itemObject; // ������� ItemSO / Item / ���ⱳ���������
    }

    object _holder;      // PlayerInventoryHolder ʵ��
    object _inventory;   // ��ʵ�������󣨿����� holder �ڣ�
    MethodInfo _miAdd, _miRemove, _miCount;

    void Awake()
    {
        // 1) �� holder
        if (holderOverride) _holder = holderOverride;
        if (_holder == null)
        {
            // ���԰�����������
            var t = FindTypeByName("PlayerInventoryHolder");
            if (t != null)
            {
                var comp = FindObjectOfType(t) as Component;
                if (comp) _holder = comp;
            }
        }
        if (_holder == null)
        {
            Debug.LogWarning("[InventoryBridge] δ�ҵ� PlayerInventoryHolder������ holderOverride �ֶ�ָ��������ʹ��ʰȡ����������Ϊ���ס�");
            return;
        }

        // 2) �� inventory �����ֶλ����ԣ�
        _inventory = ResolveInventoryObject(_holder);
        if (_inventory == null)
        {
            Debug.LogWarning("[InventoryBridge] δ�ܽ��� holder �ڵı�������inventory����");
            return;
        }

        // 3) �󶨷���
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

    /// <summary> ������ӣ�ʧ��ʱ������ pickupPrefab��������ҽű����ɶ�Ӧ������ʰȡԤ���壨������ true��ʾ�ѹ��ɹ����� </summary>
    public bool TryAdd(string itemId, int amount, Transform player, GameObject pickupPrefab)
    {
        if (amount <= 0) return true;

        if (_inventory != null && _miAdd != null)
        {
            var (arg, expectsString) = ResolveItemArg(itemId, _miAdd);
            try
            {
                // ����ǩ����(object,int) / (string,int) / (object) / (string)
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

        // ���ף�����ʰȡԤ����
        if (pickupPrefab && player)
        {
            for (int i = 0; i < amount; i++)
            {
                var pos = player.position + player.forward * 0.6f + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.2f;
                GameObject.Instantiate(pickupPrefab, pos, Quaternion.identity);
            }
            Debug.Log("[InventoryBridge] �޷�ֱ�ӵ�������������ʰȡ������Ϊ���ס�");
            return true;
        }

        Debug.LogWarning("[InventoryBridge] TryAdd ʧ�ܣ���û�п��ö��ס�");
        return false;
    }

    /// <summary> �����Ƴ����Ƴ�ʧ���򷵻� false���������ף��� </summary>
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

        Debug.LogWarning("[InventoryBridge] TryRemove ʧ�ܣ�δ�ҵ����ݵ��Ƴ�API��");
        return false;
    }

    // --------- ���丨�� ---------
    (object arg, bool expectsString) ResolveItemArg(string itemId, MethodInfo mi)
    {
        var ps = mi.GetParameters();
        var argType = ps[0].ParameterType;
        // string
        if (argType == typeof(string)) return (itemId, true);
        // �������ͣ��������ֶ�ӳ��
        var map = manualMapping.FirstOrDefault(m => m.itemId == itemId);
        if (map != null && map.itemObject != null) return (map.itemObject, false);
        return (null, false);
    }

    void BindMethods()
    {
        var invType = _inventory.GetType();

        // �ѳ���������
        _miAdd = FindMethod(invType, new[] { "AddItem", "TryAddItem", "Add", "AddById" });
        _miRemove = FindMethod(invType, new[] { "RemoveItem", "TryRemoveItem", "Remove", "RemoveById" });
        _miCount = FindMethod(invType, new[] { "GetItemCount", "GetCount", "CountOf" });

        // Ҫ���һ�������� ��Ʒ/ID���ڶ�������������
        // ���Ҳ�������ǿ���߶���
        if (_miAdd == null) Debug.Log("[InventoryBridge] δ�ҵ� Add �������������߶������ɣ���");
        if (_miRemove == null) Debug.Log("[InventoryBridge] δ�ҵ� Remove ������");
        if (_miCount == null) Debug.Log("[InventoryBridge] δ�ҵ� GetCount ������");
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

        // 1) ���������г���������Ҳ��ֱ�ӵ� inventory ��
        if (FindMethod(ht, new[] { "AddItem", "Add", "TryAddItem" }) != null)
            return holderObj;

        // 2) �ֶ�/����
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
