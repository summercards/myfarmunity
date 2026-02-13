// Assets/Scripts/Time/GameTimeSystem.cs
using UnityEngine;
using System;
using UnityEngine.Events;

/// <summary>
/// 游戏时间系统
/// 管理游戏内的时间、日期、季节和天气
/// </summary>
[CreateAssetMenu(fileName = "GameTimeSystem", menuName = "Game/Time System")]
public class GameTimeSystem : ScriptableObject
{
    [Header("时间配置")]
    [Tooltip("1个现实秒 = 多少游戏分钟")]
    [Range(0.1f, 60f)]
    public float realSecondsPerGameMinute = 1f;

    [Tooltip("游戏开始时间（小时，0-24）")]
    [Range(0f, 24f)]
    public float startHour = 6f;

    [Tooltip("游戏开始天数")]
    public int startDay = 1;

    [Tooltip("游戏开始月份")]
    [Range(1, 12)]
    public int startMonth = 1;

    [Tooltip("游戏开始年份")]
    public int startYear = 2024;

    [Header("季节配置")]
    public Season startSeason = Season.Spring;
    [Tooltip("每季持续天数")]
    public int daysPerSeason = 28;

    [Header("天气配置")]
    public WeatherType currentWeather = WeatherType.Sunny;
    [Tooltip("天气变化间隔（游戏小时）")]
    public float weatherChangeInterval = 6f;

    [Header("事件")]
    public UnityEvent onHourChanged;
    public UnityEvent onDayChanged;
    public UnityEvent onMonthChanged;
    public UnityEvent onSeasonChanged;
    public UnityEvent onYearChanged;
    public UnityEvent onWeatherChanged;
    public UnityEvent onTimeOfDayChanged;
    public UnityEvent onLoadComplete; // 新增：存档加载完成事件

    // === 运行时数据 ===

    // 时间
    private float currentGameTime = 0f; // 游戏时间（分钟为单位）
    private int currentHour = 6;
    private int currentMinute = 0;

    // 日期
    private int currentDay = 1;
    private int currentMonth = 1;
    private int currentYear = 2024;
    private Season currentSeason = Season.Spring;

    // 时间段
    private TimeOfDay currentTimeOfDay = TimeOfDay.Morning;
    private TimeOfDay lastTimeOfDay = TimeOfDay.Morning;

    // 天气
    private float weatherTimer = 0f;

    // 暂停状态
    private bool isPaused = false;

    // 上次处理过的总天数（用于计算增量）
    private int lastProcessedTotalDays = 0;

    // === 公共属性访问器 ===

    public int Hour => currentHour;
    public int Minute => currentMinute;
    public int Day => currentDay;
    public int Month => currentMonth;
    public int Year => currentYear;
    public Season Season => currentSeason;
    public TimeOfDay CurrentTimeOfDay => currentTimeOfDay;
    public WeatherType CurrentWeather => currentWeather;
    public bool IsPaused => isPaused;

    /// <summary>
    /// 获取当前游戏时间（0-24小时制）
    /// </summary>
    public float CurrentTime => currentHour + currentMinute / 60f;

    /// <summary>
    /// 获取格式化的时间字符串 (HH:MM)
    /// </summary>
    public string TimeString => TimeHelpers.FormatTime(currentHour, currentMinute);

    /// <summary>
    /// 获取格式化的日期字符串
    /// </summary>
    public string DateString => TimeHelpers.FormatDate(currentYear, currentMonth, currentDay, currentSeason);

    /// <summary>
    /// 是否为白天 (5:00 - 20:00)
    /// 修复：与时间段定义保持一致
    /// </summary>
    public bool IsDayTime => currentHour >= 5 && currentHour < 20;

    /// <summary>
    /// 是否为夜晚 (20:00 - 5:00)
    /// </summary>
    public bool IsNightTime => !IsDayTime;

    /// <summary>
    /// 初始化时间系统
    /// 修复：使用SetWeather方法触发事件，并重置所有运行时状态
    /// </summary>
    public void Initialize()
    {
        currentGameTime = startHour * 60f;
        currentDay = startDay;
        currentMonth = startMonth;
        currentYear = startYear;
        currentSeason = startSeason;
        weatherTimer = 0f; // 重置天气计时器

        // 计算初始总天数（从开始日期算起）
        int totalDaysFromStartDate = (startYear - 1) * 365 + (startMonth - 1) * 30 + startDay;
        // 这里我们简化处理，直接用当前的总小时数计算
        lastProcessedTotalDays = Mathf.FloorToInt(currentGameTime / 60f / 24f);

        UpdateTimeFromGameTime();
        currentTimeOfDay = TimeHelpers.GetTimeOfDay(currentHour);
        lastTimeOfDay = currentTimeOfDay;

        // 修复：使用SetWeather方法，确保触发onWeatherChanged事件
        SetWeather(WeatherType.Sunny);
    }

    /// <summary>
    /// 更新时间系统
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (isPaused) return;

        // 计算游戏时间增量（分钟）
        // realSecondsPerGameMinute = 1 表示 1秒现实时间 = 1分钟游戏时间
        float gameMinutesPassed = deltaTime / realSecondsPerGameMinute;
        currentGameTime += gameMinutesPassed;

        // 更新天气计时器
        weatherTimer += gameMinutesPassed / 60f; // 转换为小时
        if (weatherTimer >= weatherChangeInterval)
        {
            ChangeWeather();
            weatherTimer = 0f;
        }

        // 更新时间
        UpdateTimeFromGameTime();

        // 检查时间段变化
        CheckTimeOfDayChange();
    }

    /// <summary>
    /// 从游戏时间更新小时和分钟
    /// </summary>
    private void UpdateTimeFromGameTime()
    {
        // 记录旧的小时数
        int previousHour = currentHour;

        // 计算总小时数（带小数）
        float totalHours = currentGameTime / 60f;

        // 计算当前总天数（从游戏开始）
        int currentTotalDays = Mathf.FloorToInt(totalHours / 24f);

        // 计算这一帧新增的天数（关键修复：只计算增量）
        int newDaysPassed = currentTotalDays - lastProcessedTotalDays;

        // 更新小时和分钟
        float hourInDay = totalHours % 24f;
        currentHour = Mathf.FloorToInt(hourInDay);
        currentMinute = Mathf.FloorToInt((hourInDay - currentHour) * 60f);

        // 处理天数变化（只增加这一帧的天数差值）
        if (newDaysPassed > 0)
        {
            AdvanceDays(newDaysPassed);
            lastProcessedTotalDays = currentTotalDays; // 更新已处理的天数
        }

        // 触发小时变化事件（修复：只在小时改变时触发一次）
        if (currentHour != previousHour)
        {
            onHourChanged?.Invoke();
        }
    }

    /// <summary>
    /// 前进指定天数
    /// </summary>
    private void AdvanceDays(int days)
    {
        int oldDay = currentDay;
        currentDay += days;

        // 循环处理月份变化（修复：支持跨越多个月份）
        int daysInMonth = TimeHelpers.GetDaysInMonth(currentYear, currentMonth);
        while (currentDay > daysInMonth)
        {
            currentDay -= daysInMonth;
            AdvanceMonths(1); // 这会更新 currentMonth 和 currentYear
            daysInMonth = TimeHelpers.GetDaysInMonth(currentYear, currentMonth); // 更新月份天数
        }

        // 触发日期变化事件
        onDayChanged?.Invoke();
    }

    /// <summary>
    /// 前进指定月数
    /// </summary>
    private void AdvanceMonths(int months)
    {
        currentMonth += months;

        // 循环处理年份变化（修复：支持跨越多年）
        while (currentMonth > 12)
        {
            currentMonth -= 12;
            AdvanceYears(1); // 这会更新 currentYear
        }

        // 检查季节变化
        Season newSeason = TimeHelpers.GetSeason(currentMonth);
        if (newSeason != currentSeason)
        {
            currentSeason = newSeason;
            onSeasonChanged?.Invoke();
        }

        onMonthChanged?.Invoke();
    }

    /// <summary>
    /// 前进指定年数
    /// </summary>
    private void AdvanceYears(int years)
    {
        currentYear += years;
        onYearChanged?.Invoke();
    }

    /// <summary>
    /// 检查时间段变化
    /// </summary>
    private void CheckTimeOfDayChange()
    {
        TimeOfDay newTimeOfDay = TimeHelpers.GetTimeOfDay(currentHour);

        if (newTimeOfDay != currentTimeOfDay)
        {
            lastTimeOfDay = currentTimeOfDay;
            currentTimeOfDay = newTimeOfDay;
            onTimeOfDayChanged?.Invoke();
        }
    }

    /// <summary>
    /// 改变天气
    /// </summary>
    private void ChangeWeather()
    {
        WeatherType newWeather = (WeatherType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(WeatherType)).Length);

        // 季节影响天气
        if (currentSeason == Season.Winter)
        {
            // 冬天更容易下雪
            if (UnityEngine.Random.value < 0.4f)
                newWeather = WeatherType.Snowy;
        }
        else if (currentSeason == Season.Summer)
        {
            // 夏天更容易晴天或多云
            newWeather = (WeatherType)(UnityEngine.Random.value < 0.7f ? UnityEngine.Random.Range(0, 2) : UnityEngine.Random.Range(2, 6));
        }

        // 只有天气真正改变时才触发事件
        if (newWeather != currentWeather)
        {
            SetWeather(newWeather);
        }
    }

    /// <summary>
    /// 暂停时间
    /// </summary>
    public void Pause()
    {
        isPaused = true;
    }

    /// <summary>
    /// 恢复时间
    /// </summary>
    public void Resume()
    {
        isPaused = false;
    }

    /// <summary>
    /// 设置指定时间
    /// </summary>
    public void SetTime(int hour, int minute)
    {
        currentHour = Mathf.Clamp(hour, 0, 23);
        currentMinute = Mathf.Clamp(minute, 0, 59);
        currentGameTime = (currentHour * 60f) + currentMinute;

        CheckTimeOfDayChange();
    }

    /// <summary>
    /// 设置指定日期
    /// </summary>
    public void SetDate(int year, int month, int day)
    {
        currentYear = Mathf.Max(1, year);
        currentMonth = Mathf.Clamp(month, 1, 12);
        currentDay = Mathf.Clamp(day, 1, TimeHelpers.GetDaysInMonth(currentYear, currentMonth));
        currentSeason = TimeHelpers.GetSeason(currentMonth);
    }

    /// <summary>
    /// 设置天气
    /// </summary>
    public void SetWeather(WeatherType weather)
    {
        if (weather == currentWeather) return;

        currentWeather = weather;
        onWeatherChanged?.Invoke();
    }

    /// <summary>
    /// 设置时间流逝速度
    /// </summary>
    public void SetTimeScale(float scale)
    {
        realSecondsPerGameMinute = Mathf.Clamp(scale, 0.1f, 60f);
    }

    /// <summary>
    /// 快速前进指定小时
    /// </summary>
    public void FastForward(int hours)
    {
        currentGameTime += hours * 60f;
        UpdateTimeFromGameTime();
        CheckTimeOfDayChange();
    }

    /// <summary>
    /// 跳转到第二天早上
    /// </summary>
    public void SleepToNextDay()
    {
        int hoursUntilMorning = (24 - currentHour) + 6;
        FastForward(hoursUntilMorning);
    }

    /// <summary>
    /// 获取时间信息摘要
    /// </summary>
    public string GetTimeSummary()
    {
        return string.Format("游戏时间: {0}\n日期: {1}\n季节: {2}\n时段: {3}\n天气: {4}\n速度: {5:F1}秒/分钟",
            TimeString,
            DateString,
            TimeHelpers.GetSeasonName(currentSeason),
            TimeHelpers.GetTimeOfDayName(currentTimeOfDay),
            GetWeatherName(currentWeather),
            realSecondsPerGameMinute);
    }

    /// <summary>
    /// 获取天气名称
    /// </summary>
    public string GetWeatherName(WeatherType weather)
    {
        switch (weather)
        {
            case WeatherType.Sunny: return "晴天";
            case WeatherType.Cloudy: return "多云";
            case WeatherType.Rainy: return "雨天";
            case WeatherType.Stormy: return "暴雨";
            case WeatherType.Snowy: return "下雪";
            case WeatherType.Foggy: return "雾天";
            default: return "未知";
        }
    }

    /// <summary>
    /// 保存时间数据
    /// </summary>
    public TimeSaveData GetSaveData()
    {
        return new TimeSaveData
        {
            currentGameTime = currentGameTime,
            currentDay = currentDay,
            currentMonth = currentMonth,
            currentYear = currentYear,
            currentSeason = (int)currentSeason,
            currentWeather = (int)currentWeather,
            isPaused = isPaused,
            realSecondsPerGameMinute = realSecondsPerGameMinute,
            weatherTimer = weatherTimer,     // 保存天气计时器
            currentHour = currentHour,       // 保存当前小时
            currentMinute = currentMinute    // 保存当前分钟
        };
    }

    /// <summary>
    /// 加载时间数据
    /// 修复：加载完成后触发onLoadComplete事件，通知所有子系统
    /// </summary>
    public void LoadSaveData(TimeSaveData data)
    {
        currentGameTime = data.currentGameTime;
        currentDay = data.currentDay;
        currentMonth = data.currentMonth;
        currentYear = data.currentYear;
        currentSeason = (Season)data.currentSeason;
        isPaused = data.isPaused;
        realSecondsPerGameMinute = data.realSecondsPerGameMinute;
        weatherTimer = data.weatherTimer; // 恢复天气计时器

        // 修复：正确设置已处理的总天数，避免加载后日期跳跃
        lastProcessedTotalDays = Mathf.FloorToInt(currentGameTime / 60f / 24f);

        UpdateTimeFromGameTime();
        currentTimeOfDay = TimeHelpers.GetTimeOfDay(currentHour);

        // 触发加载完成事件，通知所有子系统更新状态
        onLoadComplete?.Invoke();
    }
}

/// <summary>
/// 时间存档数据
/// 修复：添加weatherTimer、currentHour、currentMinute字段
/// </summary>
[Serializable]
public class TimeSaveData
{
    public float currentGameTime;
    public int currentDay;
    public int currentMonth;
    public int currentYear;
    public int currentSeason;
    public int currentWeather;
    public bool isPaused;
    public float realSecondsPerGameMinute;
    public float weatherTimer; // 天气计时器
    public int currentHour;     // 当前小时
    public int currentMinute;   // 当前分钟
}
