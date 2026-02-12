// Assets/Scripts/Time/TimeUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 时间UI显示组件
/// 显示游戏时间、日期、季节、天气等信息
/// 修复：秒数显示使用游戏时间而非Time.time
/// </summary>
public class TimeUI : MonoBehaviour
{
    [Header("时间系统引用")]
    public GameTimeSystem timeSystem;

    [Header("时间显示")]
    public TMP_Text timeText;
    public TMP_Text dateText;
    public TMP_Text seasonText;
    public TMP_Text timeOfDayText;
    public TMP_Text weatherText;

    [Header("图标显示")]
    public Image weatherIcon;
    public Image timeOfDayIcon;

    [Header("时间图标")]
    public Sprite dawnIcon;
    public Sprite morningIcon;
    public Sprite noonIcon;
    public Sprite afternoonIcon;
    public Sprite duskIcon;
    public Sprite nightIcon;
    public Sprite midnightIcon;

    [Header("天气图标")]
    public Sprite sunnyIcon;
    public Sprite cloudyIcon;
    public Sprite rainyIcon;
    public Sprite stormyIcon;
    public Sprite snowyIcon;
    public Sprite foggyIcon;

    [Header("显示选项")]
    public bool showTime = true;
    public bool showDate = true;
    public bool showSeason = true;
    public bool showTimeOfDay = true;
    public bool showWeather = true;
    public bool showIcons = true;

    [Header("更新间隔")]
    [Tooltip("UI更新间隔（秒）")]
    public float updateInterval = 0.5f;
    private float lastUpdateTime;

    [Header("时间格式")]
    public bool use24HourFormat = true;
    public bool showSeconds = false;

    [Header("日期格式")]
    public string dateFormat = "yyyy年MM月dd日";

    private bool isInitialized = false;

    void Start()
    {
        // 如果没有指定时间系统，尝试查找
        if (timeSystem == null)
        {
            timeSystem = FindObjectOfType<GameTimeSystem>();
        }

        // 订阅加载完成事件
        if (timeSystem != null)
        {
            timeSystem.onLoadComplete.AddListener(OnTimeLoaded);
        }

        // 初始化UI显示
        InitializeUI();

        // 立即更新一次
        UpdateAllUI();
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
    /// 时间加载完成后立即更新UI
    /// </summary>
    private void OnTimeLoaded()
    {
        ForceUpdate();
    }

    void Update()
    {
        if (timeSystem == null) return;

        // 限制更新频率
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;

        // 更新所有UI
        UpdateAllUI();
    }

    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 显示/隐藏UI元素
        if (timeText) timeText.gameObject.SetActive(showTime);
        if (dateText) dateText.gameObject.SetActive(showDate);
        if (seasonText) seasonText.gameObject.SetActive(showSeason);
        if (timeOfDayText) timeOfDayText.gameObject.SetActive(showTimeOfDay);
        if (weatherText) weatherText.gameObject.SetActive(showWeather);
        if (weatherIcon) weatherIcon.gameObject.SetActive(showWeather && showIcons);
        if (timeOfDayIcon) timeOfDayIcon.gameObject.SetActive(showTimeOfDay && showIcons);

        isInitialized = true;
    }

    /// <summary>
    /// 更新所有UI
    /// </summary>
    private void UpdateAllUI()
    {
        if (!isInitialized) return;

        if (showTime) UpdateTime();
        if (showDate) UpdateDate();
        if (showSeason) UpdateSeason();
        if (showTimeOfDay) UpdateTimeOfDay();
        if (showWeather) UpdateWeather();
    }

    /// <summary>
    /// 更新时间显示
    /// 修复：使用游戏时间计算秒数，而不是Time.time
    /// </summary>
    private void UpdateTime()
    {
        if (timeText == null || timeSystem == null) return;

        string timeString;

        if (use24HourFormat)
        {
            if (showSeconds)
            {
                // 修复：计算游戏时间的秒数部分
                int gameSeconds = Mathf.FloorToInt((timeSystem.CurrentTime % 1f) * 60f);
                timeString = $"{timeSystem.Hour:D2}:{timeSystem.Minute:D2}:{gameSeconds:D2}";
            }
            else
            {
                timeString = $"{timeSystem.Hour:D2}:{timeSystem.Minute:D2}";
            }
        }
        else
        {
            int displayHour = timeSystem.Hour % 12;
            if (displayHour == 0) displayHour = 12;
            string ampm = timeSystem.Hour >= 12 ? "PM" : "AM";

            if (showSeconds)
            {
                // 修复：计算游戏时间的秒数部分
                int gameSeconds = Mathf.FloorToInt((timeSystem.CurrentTime % 1f) * 60f);
                timeString = $"{displayHour}:{timeSystem.Minute:D2}:{gameSeconds:D2} {ampm}";
            }
            else
            {
                timeString = $"{displayHour}:{timeSystem.Minute:D2} {ampm}";
            }
        }

        timeText.text = timeString;
    }

    /// <summary>
    /// 更新日期显示
    /// </summary>
    private void UpdateDate()
    {
        if (dateText == null || timeSystem == null) return;

        string dateString = $"{timeSystem.Year}年{timeSystem.Month}月{timeSystem.Day}日";
        dateText.text = dateString;
    }

    /// <summary>
    /// 更新季节显示
    /// </summary>
    private void UpdateSeason()
    {
        if (seasonText == null || timeSystem == null) return;

        seasonText.text = TimeHelpers.GetSeasonName(timeSystem.Season);
    }

    /// <summary>
    /// 更新时间段显示
    /// </summary>
    private void UpdateTimeOfDay()
    {
        if (timeOfDayText == null || timeSystem == null) return;

        timeOfDayText.text = TimeHelpers.GetTimeOfDayName(timeSystem.CurrentTimeOfDay);

        // 更新图标
        if (timeOfDayIcon != null && showIcons)
        {
            timeOfDayIcon.sprite = GetTimeOfDayIcon(timeSystem.CurrentTimeOfDay);
        }
    }

    /// <summary>
    /// 更新天气显示
    /// </summary>
    private void UpdateWeather()
    {
        if (weatherText == null || timeSystem == null) return;

        weatherText.text = timeSystem.GetWeatherName(timeSystem.CurrentWeather);

        // 更新图标
        if (weatherIcon != null && showIcons)
        {
            weatherIcon.sprite = GetWeatherIcon(timeSystem.CurrentWeather);
        }
    }

    /// <summary>
    /// 获取时间段图标
    /// </summary>
    private Sprite GetTimeOfDayIcon(TimeOfDay timeOfDay)
    {
        switch (timeOfDay)
        {
            case TimeOfDay.Dawn: return dawnIcon;
            case TimeOfDay.Morning: return morningIcon;
            case TimeOfDay.Noon: return noonIcon;
            case TimeOfDay.Afternoon: return afternoonIcon;
            case TimeOfDay.Dusk: return duskIcon;
            case TimeOfDay.Night: return nightIcon;
            case TimeOfDay.Midnight: return midnightIcon;
            default: return morningIcon;
        }
    }

    /// <summary>
    /// 获取天气图标
    /// </summary>
    private Sprite GetWeatherIcon(WeatherType weather)
    {
        switch (weather)
        {
            case WeatherType.Sunny: return sunnyIcon;
            case WeatherType.Cloudy: return cloudyIcon;
            case WeatherType.Rainy: return rainyIcon;
            case WeatherType.Stormy: return stormyIcon;
            case WeatherType.Snowy: return snowyIcon;
            case WeatherType.Foggy: return foggyIcon;
            default: return sunnyIcon;
        }
    }

    /// <summary>
    /// 设置时间系统
    /// </summary>
    public void SetTimeSystem(GameTimeSystem system)
    {
        timeSystem = system;
        if (system != null)
        {
            system.onLoadComplete.AddListener(OnTimeLoaded);
        }
        UpdateAllUI();
    }

    /// <summary>
    /// 切换显示选项
    /// </summary>
    public void ToggleTime(bool show)
    {
        showTime = show;
        if (timeText) timeText.gameObject.SetActive(show);
    }

    public void ToggleDate(bool show)
    {
        showDate = show;
        if (dateText) dateText.gameObject.SetActive(show);
    }

    public void ToggleSeason(bool show)
    {
        showSeason = show;
        if (seasonText) seasonText.gameObject.SetActive(show);
    }

    public void ToggleTimeOfDay(bool show)
    {
        showTimeOfDay = show;
        if (timeOfDayText) timeOfDayText.gameObject.SetActive(show);
        if (timeOfDayIcon) timeOfDayIcon.gameObject.SetActive(show && showIcons);
    }

    public void ToggleWeather(bool show)
    {
        showWeather = show;
        if (weatherText) weatherText.gameObject.SetActive(show);
        if (weatherIcon) weatherIcon.gameObject.SetActive(show && showIcons);
    }

    /// <summary>
    /// 强制立即更新
    /// </summary>
    public void ForceUpdate()
    {
        lastUpdateTime = Time.time - updateInterval;
        UpdateAllUI();
    }

    /// <summary>
    /// 设置时间格式
    /// </summary>
    public void SetTimeFormat(bool use24Hour, bool showSeconds)
    {
        use24HourFormat = use24Hour;
        this.showSeconds = showSeconds;
        ForceUpdate();
    }
}
