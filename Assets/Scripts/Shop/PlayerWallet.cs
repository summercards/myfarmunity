using UnityEngine;
using UnityEngine.Events;

public class PlayerWallet : MonoBehaviour
{
    [Tooltip("初始金币")]
    public int coins = 0;

    [System.Serializable] public class IntEvent : UnityEvent<int> { }
    public IntEvent onCoinsChanged;

    void Awake()
    {
        RuntimeRefs.RegisterPlayerWallet(this);
    }

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

    void OnDestroy()
    {
        RuntimeRefs.UnregisterPlayerWallet(this);
    }
}
