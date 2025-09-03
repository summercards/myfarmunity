using System;

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
    readonly ItemDatabaseSO db;

    public Inventory(int capacity, ItemDatabaseSO db)
    {
        slots = new ItemStack[capacity];
        this.db = db;
    }

    public int GetItemCount(string id)
    {
        int sum = 0;
        foreach (var s in slots) if (s != null && s.id == id) sum += s.count;
        return sum;
    }

    public int AddItem(string id, int count = 1)
    {
        var data = db.Get(id);
        if (data == null) return 0;
        int remain = count;

        // 先叠加
        for (int i = 0; i < slots.Length && remain > 0; i++)
        {
            var s = slots[i];
            if (s != null && s.id == id && s.count < data.maxStack)
            {
                int can = Math.Min(data.maxStack - s.count, remain);
                s.count += can; remain -= can;
            }
        }
        // 再找空格
        for (int i = 0; i < slots.Length && remain > 0; i++)
        {
            if (slots[i] == null)
            {
                int put = Math.Min(data.maxStack, remain);
                slots[i] = new ItemStack(id, put); remain -= put;
            }
        }
        return count - remain;
    }

    public int RemoveItem(string id, int count = 1)
    {
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
}
