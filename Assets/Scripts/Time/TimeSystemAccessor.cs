// Assets/Scripts/Time/TimeSystemAccessor.cs
using UnityEngine;
using FarmGame.Core;

/// <summary>
/// 时间系统接入器
/// 提供简化的全局访问接口，方便其他系统快速接入时间系统
/// 支持两种模式：单例模式（推荐用于全局场景）和资源引用模式
/// </summary>
public class TimeSystemAccessor : MonoBehaviour
{
    private static TimeSystemAccessor instance;
    private static GameTimeSystem cachedTimeSystem;
    private static bool useGlobalInstance = false;

    [Header("模式选择")]
    [Tooltip("是否使用全局单例模式（DontDestroyOnLoad）。如果为false，则使用已注册或资源引用的 TimeSystem。")]
    public bool useSingleton = true;

    [Header("时间系统引用（仅在非单例模式下使用）")]
    public GameTimeSystem timeSystem;

    /// <summary>
    /// 获取时间系统引用
    /// </summary>
    public static GameTimeSystem TimeSystem
    {
        get
        {
            // 如果已缓存且有效，直接返回
            if (cachedTimeSystem != null)
            {
                return cachedTimeSystem;
            }

            // 1. 优先使用运行时注册的实例
            cachedTimeSystem = RuntimeRefs.TimeSystem;
            if (cachedTimeSystem != null)
            {
                return cachedTimeSystem;
            }

            // 2. 单例模式：从全局实例获取
            if (useGlobalInstance || (instance != null && instance.useSingleton))
            {
                if (instance != null && instance.timeSystem != null)
                {
                    cachedTimeSystem = instance.timeSystem;
                    RuntimeRefs.RegisterTimeSystem(cachedTimeSystem);
                    return cachedTimeSystem;
                }
            }

            // 3. 优先从 Resources 加载
            cachedTimeSystem = Resources.Load<GameTimeSystem>("DefaultTimeSystem");
            if (cachedTimeSystem != null)
            {
                RuntimeRefs.RegisterTimeSystem(cachedTimeSystem);
                return cachedTimeSystem;
            }

            return cachedTimeSystem;
        }
    }

    /// <summary>
    /// 检查时间系统是否可用
    /// </summary>
    public static bool IsAvailable => TimeSystem != null;

    void Awake()
    {
        if (useSingleton)
        {
            if (!RuntimeService.TryClaimSingleton(this, instance, nameof(TimeSystemAccessor)))
            {
                return;
            }

            instance = this;
            useGlobalInstance = true;
            Debug.Log("[TimeSystemAccessor] 全局单例模式已启用");
        }

        if (timeSystem != null)
        {
            cachedTimeSystem = timeSystem;
            RuntimeRefs.RegisterTimeSystem(timeSystem);
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            cachedTimeSystem = null;
            useGlobalInstance = false;
        }
    }

    // === 简化的访问接口 ===

    #region 时间访问

    /// <summary>
    /// 当前小时（0-23）
    /// </summary>
    public static int Hour => IsAvailable ? TimeSystem.Hour : 0;

    /// <summary>
    /// 当前分钟（0-59）
    /// </summary>
    public static int Minute => IsAvailable ? TimeSystem.Minute : 0;

    /// <summary>
    /// 当前时间（0-24，带小数）
    /// </summary>
    public static float CurrentTime => IsAvailable ? TimeSystem.CurrentTime : 0;

    /// <summary>
    /// 时间字符串（HH:MM）
    /// </summary>
    public static string TimeString => IsAvailable ? TimeSystem.TimeString : "00:00";

    #endregion

    #region 日期访问

    /// <summary>
    /// 当前日期
    /// </summary>
    public static int Day => IsAvailable ? TimeSystem.Day : 1;
    public static int Month => IsAvailable ? TimeSystem.Month : 1;
    public static int Year => IsAvailable ? TimeSystem.Year : 2024;

    /// <summary>
    /// 一年中的第几天（1-365或366）
    /// </summary>
    public static int DayOfYear => IsAvailable ? TimeHelpers.GetDayOfYear(TimeSystem.Day, TimeSystem.Month, TimeSystem.Year) : 1;

    /// <summary>
    /// 日期字符串
    /// </summary>
    public static string DateString => IsAvailable ? TimeSystem.DateString : "2024年1月1日";

    /// <summary>
    /// 当前季节
    /// </summary>
    public static Season Season => IsAvailable ? TimeSystem.Season : Season.Spring;

    /// <summary>
    /// 季节名称
    /// </summary>
    public static string SeasonName => IsAvailable ? TimeHelpers.GetSeasonName(Season) : "未知";

    #endregion

    #region 时段访问

    /// <summary>
    /// 当前时间段
    /// </summary>
    public static TimeOfDay CurrentTimeOfDay => IsAvailable ? TimeSystem.CurrentTimeOfDay : TimeOfDay.Morning;

    /// <summary>
    /// 时段名称
    /// </summary>
    public static string TimeOfDayName => IsAvailable ? TimeHelpers.GetTimeOfDayName(CurrentTimeOfDay) : "未知";

    #endregion

    #region 天气访问

    /// <summary>
    /// 当前天气
    /// </summary>
    public static WeatherType Weather => IsAvailable ? TimeSystem.CurrentWeather : WeatherType.Sunny;

    /// <summary>
    /// 天气名称
    /// </summary>
    public static string WeatherName => IsAvailable ? TimeSystem.GetWeatherName(Weather) : "未知";

    #endregion

    #region 状态查询

    /// <summary>
    /// 是否为白天（5:00-20:00）
    /// </summary>
    public static bool IsDayTime => IsAvailable ? TimeSystem.IsDayTime : true;

    /// <summary>
    /// 是否为夜晚（20:00-5:00）
    /// </summary>
    public static bool IsNightTime => IsAvailable ? TimeSystem.IsNightTime : false;

    /// <summary>
    /// 是否暂停
    /// </summary>
    public static bool IsPaused => IsAvailable ? TimeSystem.IsPaused : false;

    #endregion

    #region 时间控制

    /// <summary>
    /// 设置时间
    /// </summary>
    public static void SetTime(int hour, int minute)
    {
        if (IsAvailable) TimeSystem.SetTime(hour, minute);
    }

    /// <summary>
    /// 设置日期
    /// </summary>
    public static void SetDate(int year, int month, int day)
    {
        if (IsAvailable) TimeSystem.SetDate(year, month, day);
    }

    /// <summary>
    /// 设置天气
    /// </summary>
    public static void SetWeather(WeatherType weather)
    {
        if (IsAvailable) TimeSystem.SetWeather(weather);
    }

    /// <summary>
    /// 暂停时间
    /// </summary>
    public static void Pause()
    {
        if (IsAvailable) TimeSystem.Pause();
    }

    /// <summary>
    /// 恢复时间
    /// </summary>
    public static void Resume()
    {
        if (IsAvailable) TimeSystem.Resume();
    }

    /// <summary>
    /// 切换暂停状态
    /// </summary>
    public static void TogglePause()
    {
        if (IsAvailable)
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }

    /// <summary>
    /// 快速前进
    /// </summary>
    public static void FastForward(int hours = 1)
    {
        if (IsAvailable) TimeSystem.FastForward(hours);
    }

    /// <summary>
    /// 跳到第二天早晨
    /// </summary>
    public static void SleepToNextDay()
    {
        if (IsAvailable) TimeSystem.SleepToNextDay();
    }

    #endregion

    #region 事件订阅

    /// <summary>
    /// 订阅小时变化事件
    /// </summary>
    public static void SubscribeToHourChange(UnityEngine.Events.UnityAction action)
    {
        if (IsAvailable) TimeSystem.onHourChanged.AddListener(action);
    }

    /// <summary>
    /// 取消订阅小时变化事件
    /// </summary>
    public static void UnsubscribeFromHourChange(UnityEngine.Events.UnityAction action)
    {
        if (IsAvailable) TimeSystem.onHourChanged.RemoveListener(action);
    }

    /// <summary>
    /// 订阅日期变化事件
    /// </summary>
    public static void SubscribeToDayChange(UnityEngine.Events.UnityAction action)
    {
        if (IsAvailable) TimeSystem.onDayChanged.AddListener(action);
    }

    /// <summary>
    /// 取消订阅日期变化事件
    /// </summary>
    public static void UnsubscribeFromDayChange(UnityEngine.Events.UnityAction action)
    {
        if (IsAvailable) TimeSystem.onDayChanged.RemoveListener(action);
    }

    /// <summary>
    /// 订阅季节变化事件
    /// </summary>
    public static void SubscribeToSeasonChange(UnityEngine.Events.UnityAction action)
    {
        if (IsAvailable) TimeSystem.onSeasonChanged.AddListener(action);
    }

    /// <summary>
    /// 取消订阅季节变化事件
    /// </summary>
    public static void UnsubscribeFromSeasonChange(UnityEngine.Events.UnityAction action)
    {
        if (IsAvailable) TimeSystem.onSeasonChanged.RemoveListener(action);
    }

    /// <summary>
    /// 订阅天气变化事件
    /// </summary>
    public static void SubscribeToWeatherChange(UnityEngine.Events.UnityAction action)
    {
        if (IsAvailable) TimeSystem.onWeatherChanged.AddListener(action);
    }

    /// <summary>
    /// 取消订阅天气变化事件
    /// </summary>
    public static void UnsubscribeFromWeatherChange(UnityEngine.Events.UnityAction action)
    {
        if (IsAvailable) TimeSystem.onWeatherChanged.RemoveListener(action);
    }

    /// <summary>
    /// 订阅时间段变化事件
    /// </summary>
    public static void SubscribeToTimeOfDayChange(UnityEngine.Events.UnityAction action)
    {
        if (IsAvailable) TimeSystem.onTimeOfDayChanged.AddListener(action);
    }

    /// <summary>
    /// 取消订阅时间段变化事件
    /// </summary>
    public static void UnsubscribeFromTimeOfDayChange(UnityEngine.Events.UnityAction action)
    {
        if (IsAvailable) TimeSystem.onTimeOfDayChanged.RemoveListener(action);
    }

    /// <summary>
    /// 订阅加载完成事件
    /// </summary>
    public static void SubscribeToLoadComplete(UnityEngine.Events.UnityAction action)
    {
        if (IsAvailable) TimeSystem.onLoadComplete.AddListener(action);
    }

    /// <summary>
    /// 取消订阅加载完成事件
    /// </summary>
    public static void UnsubscribeFromLoadComplete(UnityEngine.Events.UnityAction action)
    {
        if (IsAvailable) TimeSystem.onLoadComplete.RemoveListener(action);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 检查是否为指定时间段
    /// </summary>
    public static bool IsTimeOfDay(TimeOfDay timeOfDay)
    {
        return CurrentTimeOfDay == timeOfDay;
    }

    /// <summary>
    /// 检查是否为指定季节
    /// </summary>
    public static bool IsSeason(Season season)
    {
        return Season == season;
    }

    /// <summary>
    /// 检查是否为指定天气
    /// </summary>
    public static bool IsWeather(WeatherType weather)
    {
        return Weather == weather;
    }

    /// <summary>
    /// 获取时间段的中文名称
    /// </summary>
    public static string GetTimeOfDayName(TimeOfDay timeOfDay)
    {
        return TimeHelpers.GetTimeOfDayName(timeOfDay);
    }

    /// <summary>
    /// 获取季节的中文名称
    /// </summary>
    public static string GetSeasonName(Season season)
    {
        return TimeHelpers.GetSeasonName(season);
    }

    /// <summary>
    /// 格式化时间
    /// </summary>
    public static string FormatTime(int hour, int minute)
    {
        return TimeHelpers.FormatTime(hour, minute);
    }

    /// <summary>
    /// 格式化日期
    /// </summary>
    public static string FormatDate(int year, int month, int day, Season season)
    {
        return TimeHelpers.FormatDate(year, month, day, season);
    }

    #endregion
}
