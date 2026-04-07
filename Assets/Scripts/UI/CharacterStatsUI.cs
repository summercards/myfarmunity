using UnityEngine;
using UnityEngine.UI;

public class CharacterStatsUI : MonoBehaviour
{
    [Header("UI Refs")]
    public GameObject panelRoot;
    public Text healthText;
    public Text staminaText;
    public Text levelText;

    [Header("Data Refs")]
    public PlayerStats playerStats;

    [Header("Options")]
    public KeyCode toggleKey = KeyCode.C;

    void Awake()
    {
        // 自动查找面板
        if (panelRoot == null)
        {
            var found = transform.Find("CharacterStatsPanel");
            if (found) panelRoot = found.gameObject;
        }

        // 自动查找子文本组件
        if (panelRoot != null)
        {
            if (healthText == null) healthText = panelRoot.transform.Find("HealthText")?.GetComponent<Text>();
            if (staminaText == null) staminaText = panelRoot.transform.Find("StaminaText")?.GetComponent<Text>();
            if (levelText == null) levelText = panelRoot.transform.Find("LevelText")?.GetComponent<Text>();
        }

        // 自动查找玩家数据
        if (playerStats == null)
        {
            playerStats = RuntimeRefs.PlayerStats;
            if (playerStats == null && RuntimeRefs.PlayerTransform != null)
                playerStats = RuntimeRefs.PlayerTransform.GetComponent<PlayerStats>();
        }

        // 初始设为隐藏
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void OnEnable()
    {
        RuntimeRefs.PlayerStatsChanged += HandlePlayerStatsChanged;
    }

    void OnDisable()
    {
        RuntimeRefs.PlayerStatsChanged -= HandlePlayerStatsChanged;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }

        // 只要面板开着就刷新数据
        if (panelRoot != null && panelRoot.activeSelf)
        {
            RefreshStats();
        }
    }

    public void TogglePanel()
    {
        if (panelRoot == null) return;
        
        bool isActive = !panelRoot.activeSelf;
        panelRoot.SetActive(isActive);
        
        if (isActive) RefreshStats();
    }

    void RefreshStats()
    {
        if (playerStats == null) return;

        if (healthText) healthText.text = $"Health: {playerStats.currentHealth:0}/{playerStats.maxHealth:0}";
        if (staminaText) staminaText.text = $"Stamina: {playerStats.currentStamina:0}/{playerStats.maxStamina:0}";
        if (levelText) levelText.text = $"Level: {playerStats.level}";
    }

    void HandlePlayerStatsChanged(PlayerStats stats)
    {
        playerStats = stats;
        if (panelRoot != null && panelRoot.activeSelf)
        {
            RefreshStats();
        }
    }
}
