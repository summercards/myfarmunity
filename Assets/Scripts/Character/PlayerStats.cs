using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    public float currentHealth = 100f;
    public float maxHealth = 100f;

    [Header("Stamina")]
    public float currentStamina = 100f;
    public float maxStamina = 100f;
    public float staminaRegenRate = 5f;

    [Header("Level")]
    public int level = 1;
    public int currentExp = 0;
    public int expToNextLevel = 1000;

    // 事件：当属性改变时触发（供UI更新）
    public event Action OnStatsChanged;

    void Awake()
    {
        RuntimeRefs.RegisterPlayerStats(this);
    }

    void Start()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }

    void Update()
    {
        // 简单的耐力回复逻辑
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            if (currentStamina > maxStamina) currentStamina = maxStamina;
            
            // 可选：频繁触发 UI 更新可能会影响性能，实际中可以使用定时器或只在整数变化时触发
            // OnStatsChanged?.Invoke(); 
        }
    }
    
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;
        OnStatsChanged?.Invoke();
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        OnStatsChanged?.Invoke();
    }

    public void ConsumeStamina(float amount)
    {
        currentStamina -= amount;
        if (currentStamina < 0) currentStamina = 0;
        OnStatsChanged?.Invoke();
    }

    void OnDestroy()
    {
        RuntimeRefs.UnregisterPlayerStats(this);
    }
}
