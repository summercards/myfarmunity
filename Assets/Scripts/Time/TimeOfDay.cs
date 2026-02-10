// Assets/Scripts/Time/TimeOfDay.cs
using UnityEngine;

/// <summary>
/// 一天中的时间段
/// </summary>
public enum TimeOfDay
{
    Dawn,       // 黎明 (5:00 - 7:00)
    Morning,    // 早晨 (7:00 - 11:00)
    Noon,       // 中午 (11:00 - 14:00)
    Afternoon,  // 下午 (14:00 - 18:00)
    Dusk,       // 黄昏 (18:00 - 20:00)
    Night,      // 夜晚 (20:00 - 5:00)
    Midnight    // 深夜 (0:00 - 3:00)
}

/// <summary>
/// 季节
/// </summary>
public enum Season
{
    Spring,     // 春季
    Summer,     // 夏季
    Autumn,     // 秋季
    Winter      // 冬季
}

/// <summary>
/// 天气类型
/// </summary>
public enum WeatherType
{
    Sunny,      // 晴天
    Cloudy,     // 多云
    Rainy,      // 雨天
    Stormy,     // 暴雨
    Snowy,      // 下雪
    Foggy       // 雾天
}

/// <summary>
/// 时间系统辅助类
/// </summary>
public static class TimeHelpers
{
    /// <summary>
    /// 将小时(0-24)转换为TimeOfDay
    /// </summary>
    public static TimeOfDay GetTimeOfDay(int hour)
    {
        if (hour >= 5 && hour < 7) return TimeOfDay.Dawn;
        if (hour >= 7 && hour < 11) return TimeOfDay.Morning;
        if (hour >= 11 && hour < 14) return TimeOfDay.Noon;
        if (hour >= 14 && hour < 18) return TimeOfDay.Afternoon;
        if (hour >= 18 && hour < 20) return TimeOfDay.Dusk;
        if (hour >= 20 || hour < 0) return TimeOfDay.Night;
        return TimeOfDay.Midnight;
    }

    /// <summary>
    /// 将月份数字转换为季节
    /// </summary>
    public static Season GetSeason(int month)
    {
        switch (month)
        {
            case 3:
            case 4:
            case 5:
                return Season.Spring;
            case 6:
            case 7:
            case 8:
                return Season.Summer;
            case 9:
            case 10:
            case 11:
                return Season.Autumn;
            case 12:
            case 1:
            case 2:
                return Season.Winter;
            default:
                return Season.Spring;
        }
    }

    /// <summary>
    /// 获取季节的中文名称
    /// </summary>
    public static string GetSeasonName(Season season)
    {
        switch (season)
        {
            case Season.Spring: return "春季";
            case Season.Summer: return "夏季";
            case Season.Autumn: return "秋季";
            case Season.Winter: return "冬季";
            default: return "未知";
        }
    }

    /// <summary>
    /// 获取时间段的中文名称
    /// </summary>
    public static string GetTimeOfDayName(TimeOfDay timeOfDay)
    {
        switch (timeOfDay)
        {
            case TimeOfDay.Dawn: return "黎明";
            case TimeOfDay.Morning: return "早晨";
            case TimeOfDay.Noon: return "中午";
            case TimeOfDay.Afternoon: return "下午";
            case TimeOfDay.Dusk: return "黄昏";
            case TimeOfDay.Night: return "夜晚";
            case TimeOfDay.Midnight: return "深夜";
            default: return "未知";
        }
    }

    /// <summary>
    /// 格式化时间显示
    /// </summary>
    public static string FormatTime(int hour, int minute)
    {
        return $"{hour:D2}:{minute:D2}";
    }

    /// <summary>
    /// 格式化日期显示
    /// </summary>
    public static string FormatDate(int year, int month, int day, Season season)
    {
        return $"{year}年{month}月{day}日 {GetSeasonName(season)}";
    }

    /// <summary>
    /// 检查是否为闰年
    /// </summary>
    public static bool IsLeapYear(int year)
    {
        return (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
    }

    /// <summary>
    /// 获取某个月的天数
    /// </summary>
    public static int GetDaysInMonth(int year, int month)
    {
        switch (month)
        {
            case 1:
            case 3:
            case 5:
            case 7:
            case 8:
            case 10:
            case 12:
                return 31;
            case 4:
            case 6:
            case 9:
            case 11:
                return 30;
            case 2:
                return IsLeapYear(year) ? 29 : 28;
            default:
                return 30;
        }
    }
}
