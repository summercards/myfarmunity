using UnityEngine;

/// <summary>
/// 自动设置时间系统组件之间的引用关系
/// 将此脚本添加到场景中，运行一次后会自动配置所有时间系统组件的引用
/// </summary>
public class SetupTimeSystemReferences : MonoBehaviour
{
    void Start()
    {
        SetupTimeSystem();
    }

    [ContextMenu("设置时间系统引用")]
    public void SetupTimeSystem()
    {
        // 1. 加载 GameTimeSystem 资源
        GameTimeSystem timeSystem = Resources.Load<GameTimeSystem>("DefaultTimeSystem");
        
        if (timeSystem == null)
        {
            Debug.LogError("[Setup] 未找到 DefaultTimeSystem 资源！");
            return;
        }

        // 2. 配置 TimeController
        TimeController controller = FindObjectOfType<TimeController>();
        if (controller != null)
        {
            controller.timeSystem = timeSystem;
            Debug.Log("[Setup] TimeController 已配置");
        }
        else
        {
            Debug.LogWarning("[Setup] 未找到 TimeController");
        }

        // 3. 配置 DayNightCycle
        DayNightCycle dayNightCycle = FindObjectOfType<DayNightCycle>();
        if (dayNightCycle != null)
        {
            dayNightCycle.timeSystem = timeSystem;
            if (dayNightCycle.directionalLight == null)
            {
                dayNightCycle.directionalLight = FindObjectOfType<Light>();
            }
            Debug.Log("[Setup] DayNightCycle 已配置");
        }
        else
        {
            Debug.LogWarning("[Setup] 未找到 DayNightCycle");
        }

        // 4. 配置 TimeUI
        TimeUI timeUI = FindObjectOfType<TimeUI>();
        if (timeUI != null)
        {
            timeUI.timeSystem = timeSystem;
            
            // 查找并设置文本引用
            Transform panel = transform.Find("TimePanel");
            if (panel == null)
            {
                panel = GameObject.Find("TimePanel")?.transform;
            }
            
            if (panel != null)
            {
                timeUI.timeText = panel.Find("TimeText")?.GetComponent<TMPro.TextMeshProUGUI>();
                timeUI.dateText = panel.Find("DateText")?.GetComponent<TMPro.TextMeshProUGUI>();
                timeUI.seasonText = panel.Find("SeasonText")?.GetComponent<TMPro.TextMeshProUGUI>();
                timeUI.weatherText = panel.Find("WeatherText")?.GetComponent<TMPro.TextMeshProUGUI>();
                
                Debug.Log("[Setup] TimeUI 已配置");
            }
            else
            {
                Debug.LogWarning("[Setup] 未找到 TimePanel");
            }
        }
        else
        {
            Debug.LogWarning("[Setup] 未找到 TimeUI");
        }

        Debug.Log("[Setup] 时间系统引用配置完成！");

        // 删除此脚本，避免重复执行
        Destroy(this);
    }
}
