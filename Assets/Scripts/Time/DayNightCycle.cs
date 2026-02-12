// Assets/Scripts/Time/DayNightCycle.cs
using UnityEngine;

/// <summary>
/// 昼夜循环系统
/// 根据游戏时间控制光照、天空、环境光等
/// 修复：昼夜定义与时间段保持一致（白天5:00-20:00，夜晚20:00-5:00）
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    [Header("时间系统引用")]
    public GameTimeSystem timeSystem;

    [Header("光照配置")]
    [Tooltip("主方向光（太阳/月亮）")]
    public Light directionalLight;

    [Tooltip("天空盒")]
    public Material skybox;

    [Header("太阳配置")]
    [Tooltip("日出的旋转角度")]
    public Vector3 sunriseRotation = new Vector3(10f, -90f, 0f);

    [Tooltip("正午的旋转角度")]
    public Vector3 noonRotation = new Vector3(90f, 0f, 0f);

    [Tooltip("日落的旋转角度")]
    public Vector3 sunsetRotation = new Vector3(170f, 90f, 0f);

    [Tooltip("夜晚的旋转角度（月亮）")]
    public Vector3 nightRotation = new Vector3(270f, 0f, 0f);

    [Tooltip("日出时间（小时）- 与黎明时段(5:00-7:00)起始一致")]
    [Range(0f, 24f)]
    public float sunriseHour = 5f;

    [Tooltip("正午时间（小时）")]
    [Range(0f, 24f)]
    public float noonHour = 12f;

    [Tooltip("日落时间（小时）- 与黄昏时段(18:00-20:00)结束一致")]
    [Range(0f, 24f)]
    public float sunsetHour = 20f;

    [Header("光照强度配置")]
    [Tooltip("正午光照强度")]
    [Range(0f, 2f)]
    public float maxLightIntensity = 1.2f;

    [Tooltip("夜晚光照强度")]
    [Range(0f, 1f)]
    public float minLightIntensity = 0.1f;

    [Tooltip("光照强度过渡速度")]
    [Range(0.1f, 5f)]
    public float lightTransitionSpeed = 1f;

    [Header("环境光配置")]
    [Tooltip("白天环境光颜色")]
    public Color dayAmbientColor = new Color(0.6f, 0.6f, 0.7f, 1f);

    [Tooltip("夜晚环境光颜色")]
    public Color nightAmbientColor = new Color(0.1f, 0.1f, 0.2f, 1f);

    [Tooltip("环境光过渡速度")]
    [Range(0.1f, 5f)]
    public float ambientTransitionSpeed = 1f;

    [Header("雾效配置")]
    [Tooltip("白天雾的颜色")]
    public Color dayFogColor = new Color(0.8f, 0.9f, 1f, 1f);

    [Tooltip("夜晚雾的颜色")]
    public Color nightFogColor = new Color(0.1f, 0.1f, 0.2f, 1f);

    [Tooltip("是否使用雾效")]
    public bool useFog = true;

    [Header("月亮配置")]
    [Tooltip("月光强度")]
    [Range(0f, 1f)]
    public float moonIntensity = 0.3f;

    [Header("太阳颜色")]
    [Tooltip("日出时的太阳颜色")]
    public Color sunriseColor = new Color(1f, 0.6f, 0.3f, 1f);

    [Tooltip("正午时的太阳颜色")]
    public Color noonColor = new Color(1f, 0.95f, 0.8f, 1f);

    [Tooltip("日落时的太阳颜色")]
    public Color sunsetColor = new Color(1f, 0.5f, 0.2f, 1f);

    // 运行时数据
    private float currentLightIntensity;
    private Color currentAmbientColor;
    private Color currentFogColor;
    private Vector3 currentRotation;

    private bool isInitialized = false;

    void Start()
    {
        // 如果没有指定时间系统，尝试查找
        if (timeSystem == null)
        {
            timeSystem = FindObjectOfType<GameTimeSystem>();
        }

        // 初始化灯光
        if (directionalLight == null)
        {
            directionalLight = FindObjectOfType<Light>();
        }

        // 订阅加载完成事件
        if (timeSystem != null)
        {
            timeSystem.onLoadComplete.AddListener(OnTimeLoaded);
        }

        // 设置初始值（等待时间系统初始化后再更新）
    }

    void OnDestroy()
    {
        // 只有时间系统存在时才取消订阅
        if (timeSystem != null)
        {
            timeSystem.onLoadComplete.RemoveListener(OnTimeLoaded);
        }
    }

    /// <summary>
    /// 时间加载完成后立即更新
    /// </summary>
    private void OnTimeLoaded()
    {
        UpdateDayNightCycle();
        isInitialized = true;
    }

    void Update()
    {
        if (timeSystem == null) return;

        // 确保时间系统已初始化
        if (!isInitialized && timeSystem.Hour > 0)
        {
            OnTimeLoaded();
        }

        UpdateDayNightCycle();
    }

    /// <summary>
    /// 更新昼夜循环
    /// </summary>
    private void UpdateDayNightCycle()
    {
        float currentTime = timeSystem.CurrentTime;
        float dayProgress = GetDayProgress(currentTime);

        // 更新光源
        UpdateLight(dayProgress);

        // 更新环境光
        UpdateAmbientLight(dayProgress);

        // 更新雾效
        UpdateFog(dayProgress);
    }

    /// <summary>
    /// 获取一天的时间进度（0-1）
    /// </summary>
    private float GetDayProgress(float hour)
    {
        // 将24小时映射到0-1
        return Mathf.Repeat(hour / 24f, 1f);
    }

    /// <summary>
    /// 更新光源
    /// </summary>
    private void UpdateLight(float dayProgress)
    {
        if (directionalLight == null) return;

        // 计算当前小时（0-24）
        float currentHour = dayProgress * 24f;

        // 计算光照强度
        float lightIntensity = CalculateLightIntensity(currentHour);
        currentLightIntensity = Mathf.Lerp(currentLightIntensity, lightIntensity, Time.deltaTime * lightTransitionSpeed);
        directionalLight.intensity = currentLightIntensity;

        // 计算光源颜色
        Color lightColor = CalculateLightColor(currentHour);
        directionalLight.color = lightColor;

        // 计算光源旋转
        Vector3 targetRotation = CalculateLightRotation(currentHour);
        currentRotation = Vector3.Lerp(currentRotation, targetRotation, Time.deltaTime * lightTransitionSpeed);
        directionalLight.transform.rotation = Quaternion.Euler(currentRotation);
    }

    /// <summary>
    /// 计算光照强度
    /// 修复：与昼夜判断保持一致（白天5:00-20:00）
    /// </summary>
    private float CalculateLightIntensity(float hour)
    {
        // 白天时段 (5:00 - 20:00)
        if (hour >= sunriseHour && hour < sunsetHour)
        {
            // 日出到正午
            if (hour < noonHour)
            {
                float progress = (hour - sunriseHour) / (noonHour - sunriseHour);
                return Mathf.Lerp(minLightIntensity, maxLightIntensity, progress);
            }
            // 正午到日落
            else
            {
                float progress = (hour - noonHour) / (sunsetHour - noonHour);
                return Mathf.Lerp(maxLightIntensity, minLightIntensity, progress);
            }
        }
        // 夜晚时段 (20:00 - 5:00)
        else
        {
            return minLightIntensity * moonIntensity; // 夜晚使用月光强度
        }
    }

    /// <summary>
    /// 计算光源颜色
    /// </summary>
    private Color CalculateLightColor(float hour)
    {
        // 日出
        if (hour >= sunriseHour && hour < sunriseHour + 2f)
        {
            return sunriseColor;
        }
        // 正午时段
        else if (hour >= sunriseHour + 2f && hour < sunsetHour - 2f)
        {
            return noonColor;
        }
        // 日落
        else if (hour >= sunsetHour - 2f && hour < sunsetHour)
        {
            return sunsetColor;
        }
        // 夜晚
        else
        {
            return Color.white;
        }
    }

    /// <summary>
    /// 计算光源旋转
    /// </summary>
    private Vector3 CalculateLightRotation(float hour)
    {
        // 日出到正午
        if (hour >= sunriseHour && hour < noonHour)
        {
            float progress = (hour - sunriseHour) / (noonHour - sunriseHour);
            return Vector3.Lerp(sunriseRotation, noonRotation, progress);
        }
        // 正午到日落
        else if (hour >= noonHour && hour < sunsetHour)
        {
            float progress = (hour - noonHour) / (sunsetHour - noonHour);
            return Vector3.Lerp(noonRotation, sunsetRotation, progress);
        }
        // 日落到午夜
        else if (hour >= sunsetHour && hour < 24f)
        {
            float progress = (hour - sunsetHour) / (24f - sunsetHour);
            return Vector3.Lerp(sunsetRotation, nightRotation, progress);
        }
        // 午夜到日出
        else
        {
            float progress = hour / sunriseHour;
            return Vector3.Lerp(nightRotation, sunriseRotation, progress);
        }
    }

    /// <summary>
    /// 更新环境光
    /// 修复：与昼夜判断保持一致
    /// </summary>
    private void UpdateAmbientLight(float dayProgress)
    {
        float currentHour = dayProgress * 24f;

        Color targetAmbientColor;
        // 白天 (5:00 - 20:00)
        if (currentHour >= sunriseHour && currentHour < sunsetHour)
        {
            targetAmbientColor = dayAmbientColor;
        }
        // 夜晚 (20:00 - 5:00)
        else
        {
            targetAmbientColor = nightAmbientColor;
        }

        currentAmbientColor = Color.Lerp(currentAmbientColor, targetAmbientColor, Time.deltaTime * ambientTransitionSpeed);
        RenderSettings.ambientLight = currentAmbientColor;
    }

    /// <summary>
    /// 更新雾效
    /// 修复：与昼夜判断保持一致
    /// </summary>
    private void UpdateFog(float dayProgress)
    {
        if (!useFog) return;

        float currentHour = dayProgress * 24f;

        Color targetFogColor;
        // 白天 (5:00 - 20:00)
        if (currentHour >= sunriseHour && currentHour < sunsetHour)
        {
            targetFogColor = dayFogColor;
        }
        // 夜晚 (20:00 - 5:00)
        else
        {
            targetFogColor = nightFogColor;
        }

        currentFogColor = Color.Lerp(currentFogColor, targetFogColor, Time.deltaTime * ambientTransitionSpeed);
        RenderSettings.fogColor = currentFogColor;
        RenderSettings.fog = true;
    }

    /// <summary>
    /// 设置时间系统引用
    /// </summary>
    public void SetTimeSystem(GameTimeSystem system)
    {
        timeSystem = system;
        if (system != null)
        {
            system.onLoadComplete.AddListener(OnTimeLoaded);
        }
    }

    /// <summary>
    /// 设置方向光
    /// </summary>
    public void SetDirectionalLight(Light light)
    {
        directionalLight = light;
    }

    /// <summary>
    /// 强制立即更新
    /// </summary>
    public void ForceUpdate()
    {
        if (timeSystem != null)
        {
            UpdateDayNightCycle();
        }
    }
}
