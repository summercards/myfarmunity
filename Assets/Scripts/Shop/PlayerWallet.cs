using UnityEngine;
using UnityEngine.Events;

public class PlayerWallet : MonoBehaviour
{
    [Tooltip("³õÊ¼½ð±Ò")]
    public int coins = 0;

    [System.Serializable] public class IntEvent : UnityEvent<int> { }
    public IntEvent onCoinsChanged;

    public bool CanAfford(int cost) => coins >= cost;

    public bool TrySpend(int cost)
    {
        if (coins < cost) return false;
        coins -= cost;
        onCoinsChanged?.Invoke(coins);
        return true;
    }

    public void Add(int amount)
    {
        coins += amount;
        onCoinsChanged?.Invoke(coins);
    }
}
