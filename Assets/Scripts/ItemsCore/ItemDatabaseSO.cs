using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/ItemDatabase")]
public class ItemDatabaseSO : ScriptableObject
{
    public List<ItemSO> allItems = new();
    Dictionary<string, ItemSO> _map;

    public void RebuildIndex()
    {
        _map = new Dictionary<string, ItemSO>();
        foreach (var d in allItems)
        {
            if (d == null || string.IsNullOrWhiteSpace(d.id)) continue;
            _map[d.id] = d;
        }
    }

    public ItemSO Get(string id)
    {
        if (_map == null) RebuildIndex();
        return (id != null && _map.TryGetValue(id, out var v)) ? v : null;
    }
}
