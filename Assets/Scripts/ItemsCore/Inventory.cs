// Assets/Scripts/ItemsCore/Inventory.cs
using System;

/// <summary>һ���ǳ��򵥵�˳�򱳰����̶��������ɶѵ�������λ�洢��</summary>
[Serializable]
public class ItemStack
{
    public string id;
    public int count;
    public int durability;
    public ItemStack(string id, int count = 1, int durability = 0)
    {
        this.id = id; this.count = count; this.durability = durability;
    }
}

public class Inventory
{
    public ItemStack[] slots;

    /// <summary>���ڲ�ѯָ����Ʒ�����ѵ�����Ĭ��99��</summary>
    public System.Func<string, int> GetMaxStackForId = _ => 99;

    public Inventory(int capacity = 24)
    {
        slots = new ItemStack[Math.Max(1, capacity)];
    }

    public void Clear()
    {
        for (int i = 0; i < slots.Length; i++) slots[i] = null;
    }

    public int GetItemCount(string id)
    {
        if (string.IsNullOrEmpty(id) || slots == null) return 0;
        int total = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s != null && s.id == id) total += s.count;
        }
        return total;
    }

    /// <summary>��������� count ��ָ��id������ʵ�����������</summary>
    public int AddItem(string id, int count = 1)
    {
        if (string.IsNullOrEmpty(id) || count <= 0) return 0;
        int remain = count;

        int maxStack = Math.Max(1, GetMaxStackForId?.Invoke(id) ?? 99);

        // ��������жѵ�
        for (int i = 0; i < slots.Length && remain > 0; i++)
        {
            var s = slots[i];
            if (s != null && s.id == id && s.count < maxStack)
            {
                int can = maxStack - s.count;
                int take = Math.Min(can, remain);
                s.count += take;
                remain -= take;
            }
        }

        // ��ռ�ÿղ�
        for (int i = 0; i < slots.Length && remain > 0; i++)
        {
            if (slots[i] == null)
            {
                int take = Math.Min(maxStack, remain);
                slots[i] = new ItemStack(id, take);
                remain -= take;
            }
        }
        return count - remain;
    }

    /// <summary>�Ƴ� count ��ָ��id������ʵ���Ƴ�������</summary>
    public int RemoveItem(string id, int count = 1)
    {
        if (string.IsNullOrEmpty(id) || count <= 0) return 0;

        int remain = count;
        for (int i = 0; i < slots.Length && remain > 0; i++)
        {
            var s = slots[i];
            if (s != null && s.id == id)
            {
                int take = Math.Min(s.count, remain);
                s.count -= take; remain -= take;
                if (s.count <= 0) slots[i] = null;
            }
        }
        return count - remain;
    }

    public int FirstEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++) if (slots[i] == null) return i;
        return -1;
    }

    public void SwapSlots(int a, int b)
    {
        if (a < 0 || b < 0 || a >= slots.Length || b >= slots.Length || a == b) return;
        var tmp = slots[a]; slots[a] = slots[b]; slots[b] = tmp;
    }

    /// <summary>�Ѳ�λ a �����ɸ���Ʒ�ƶ�����λ b�������ڷֶѣ��������ƶ�������</summary>
    public int MoveSome(int fromIndex, int toIndex, int amount)
    {
        if (fromIndex < 0 || toIndex < 0 || fromIndex >= slots.Length || toIndex >= slots.Length) return 0;
        var from = slots[fromIndex];
        if (from == null || amount <= 0) return 0;

        if (slots[toIndex] == null)
        {
            int take = Math.Min(amount, from.count);
            slots[toIndex] = new ItemStack(from.id, take, from.durability);
            from.count -= take;
            if (from.count <= 0) slots[fromIndex] = null;
            return take;
        }
        else
        {
            var to = slots[toIndex];
            if (to.id != from.id) return 0;
            int maxStack = Math.Max(1, GetMaxStackForId?.Invoke(to.id) ?? 99);
            int can = maxStack - to.count;
            int take = Math.Min(can, Math.Min(amount, from.count));
            to.count += take;
            from.count -= take;
            if (from.count <= 0) slots[fromIndex] = null;
            return take;
        }
    }
}
