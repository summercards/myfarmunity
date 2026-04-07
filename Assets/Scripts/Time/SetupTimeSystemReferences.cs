using UnityEngine;

/// <summary>
/// 自动设置时间系统组件之间的引用关系
/// 将此脚本添加到场景中，运行一次后会自动配置所有时间系统组件的引用
/// </summary>
public class SetupTimeSystemReferences : MonoBehaviour
{
    [Header("Behavior")]
    [Tooltip("自动从当前对象/父子层级补齐 TimeController/DayNightCycle/TimeUI（不做全场景扫描）。")]
    public bool autoBindFromLocalHierarchy = true;
    [Tooltip("缺少可选引用时是否打印 Warning。")]
    public bool warnWhenOptionalReferenceMissing = false;
    [Tooltip("配置完成后是否自动移除该脚本。")]
    public bool destroyAfterSetup = true;

    [Header("Optional Refs")]
    public TimeController controller;
    public DayNightCycle dayNightCycle;
    public Light directionalLight;
    public TimeUI timeUI;
    public Transform timePanelRoot;

    void Start()
    {
        SetupTimeSystem();
    }

    [ContextMenu("设置时间系统引用")]
    public void SetupTimeSystem()
    {
        if (autoBindFromLocalHierarchy)
        {
            controller = ResolveFromLocalHierarchy(controller);
            dayNightCycle = ResolveFromLocalHierarchy(dayNightCycle);
            timeUI = ResolveFromLocalHierarchy(timeUI);
        }

        // 1. 加载 GameTimeSystem 资源
        GameTimeSystem timeSystem = Resources.Load<GameTimeSystem>("DefaultTimeSystem");
        
        if (timeSystem == null)
        {
            Debug.LogError("[Setup] 未找到 DefaultTimeSystem 资源！");
            return;
        }

        // 2. 配置 TimeController
        if (controller != null)
        {
            controller.timeSystem = timeSystem;
            Debug.Log("[Setup] TimeController 已配置");
        }
        else
        {
            LogOptionalMissing("TimeController");
        }

        // 3. 配置 DayNightCycle
        if (dayNightCycle != null)
        {
            dayNightCycle.timeSystem = timeSystem;
            if (dayNightCycle.directionalLight == null)
            {
                dayNightCycle.directionalLight = directionalLight != null ? directionalLight : RenderSettings.sun;
            }
            Debug.Log("[Setup] DayNightCycle 已配置");
        }
        else
        {
            LogOptionalMissing("DayNightCycle");
        }

        // 4. 配置 TimeUI
        if (timeUI != null)
        {
            timeUI.timeSystem = timeSystem;
            
            // 查找并设置文本引用
            Transform panel = timePanelRoot != null ? timePanelRoot : transform.Find("TimePanel");
            
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
                LogOptionalMissing("TimePanel");
            }
        }
        else
        {
            LogOptionalMissing("TimeUI");
        }

        Debug.Log("[Setup] 时间系统引用配置完成！");

        // 删除此脚本，避免重复执行
        if (destroyAfterSetup)
        {
            Destroy(this);
        }
    }

    private void LogOptionalMissing(string name)
    {
        if (warnWhenOptionalReferenceMissing)
        {
            Debug.LogWarning($"[Setup] 未找到 {name}");
        }
    }

    private T ResolveFromLocalHierarchy<T>(T current) where T : Component
    {
        if (current != null)
        {
            return current;
        }

        if (TryGetComponent(out T onSelf))
        {
            return onSelf;
        }

        T inChildren = GetComponentInChildren<T>(true);
        if (inChildren != null)
        {
            return inChildren;
        }

        T inParents = GetComponentInParent<T>(true);
        return inParents;
    }
}
