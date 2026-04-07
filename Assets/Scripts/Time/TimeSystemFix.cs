// 修复时间系统的换算逻辑和日夜一致性
// 此脚本用于分析问题，实际修复需要修改 GameTimeSystem.cs 和 DayNightCycle.cs

using UnityEngine;

public class TimeSystemFix : MonoBehaviour
{
    [Header("调试信息")]
    public bool showDebugInfo = true;
    [Tooltip("可选：手动指定 DayNightCycle，避免场景扫描")]
    public DayNightCycle dayNightCycle;

    void Update()
    {
        if (!showDebugInfo) return;

        GameTimeSystem timeSystem = RuntimeRefs.TimeSystem;
        if (timeSystem == null)
        {
            timeSystem = TimeSystemAccessor.TimeSystem;
        }

        if (timeSystem == null) return;

        Debug.Log($"[时间调试] 现实时间: {Time.time:F2}s | 游戏时间: {timeSystem.Hour:D2}:{timeSystem.Minute:D2}");
        
        // 显示换算分析
        float secondsPerMinute = timeSystem.realSecondsPerGameMinute;
        float actualMinutesPerSecond = 60f / secondsPerMinute;
        float actualHoursPerSecond = actualMinutesPerSecond / 60f;
        
        Debug.Log($"[换算] 配置: {secondsPerMinute}秒/分钟 | 实际: {actualHoursPerSecond:F2}小时/秒");
    }

    /// <summary>
    /// 检查时间系统配置
    /// </summary>
    [ContextMenu("检查时间系统配置")]
    public void CheckTimeSystemConfig()
    {
        Debug.Log("=== 时间系统配置检查 ===");

        GameTimeSystem timeSystem = RuntimeRefs.TimeSystem;
        if (timeSystem == null)
        {
            timeSystem = TimeSystemAccessor.TimeSystem;
        }

        DayNightCycle resolvedDayNightCycle = ResolveDayNightCycle();

        if (timeSystem != null)
        {
            float secondsPerMinute = timeSystem.realSecondsPerGameMinute;
            float actualHoursPerSecond = (60f / secondsPerMinute) / 60f;
            
            Debug.Log($"[GameTimeSystem] realSecondsPerGameMinute = {secondsPerMinute}");
            Debug.Log($"[GameTimeSystem] 实际换算: 1秒现实时间 = {actualHoursPerSecond}小时游戏时间");
            Debug.Log($"[GameTimeSystem] 昼夜判断: 白天(6:00-18:00) {timeSystem.IsDayTime}");
            
            // 检查各时间段
            Debug.Log($"[时间段] 当前: {TimeHelpers.GetTimeOfDayName(timeSystem.CurrentTimeOfDay)}");
            Debug.Log($"[时间段] 时间段定义:");
            Debug.Log($"  - 黎明(Dawn):     5:00 - 7:00");
            Debug.Log($"  - 早晨(Morning):  7:00 - 11:00");
            Debug.Log($"  - 中午(Noon):     11:00 - 14:00");
            Debug.Log($"  - 下午(Afternoon):14:00 - 18:00");
            Debug.Log($"  - 黄昏(Dusk):     18:00 - 20:00");
            Debug.Log($"  - 夜晚(Night):    20:00 - 24:00");
            Debug.Log($"  - 深夜(Midnight): 0:00 - 5:00");
        }

        if (resolvedDayNightCycle != null)
        {
            Debug.Log($"[DayNightCycle] 日出: {resolvedDayNightCycle.sunriseHour}:00");
            Debug.Log($"[DayNightCycle] 正午: {resolvedDayNightCycle.noonHour}:00");
            Debug.Log($"[DayNightCycle] 日落: {resolvedDayNightCycle.sunsetHour}:00");
            Debug.Log($"[DayNightCycle] 光照强度范围: {resolvedDayNightCycle.minLightIntensity} - {resolvedDayNightCycle.maxLightIntensity}");
        }

        // 检查一致性问题
        Debug.Log("\n=== 一致性问题 ===");
        if (timeSystem != null && resolvedDayNightCycle != null)
        {
            bool timeConsistent = (resolvedDayNightCycle.sunriseHour == 5 || resolvedDayNightCycle.sunriseHour == 6);
            Debug.Log($"[问题1] 日出时间与黎明段起始一致? {(resolvedDayNightCycle.sunriseHour == 5 ? "✓" : "✗ (黎明从5:00开始，日出从" + resolvedDayNightCycle.sunriseHour + ":00开始)")}");
            Debug.Log($"[问题2] 昼夜判断与光强变化一致? {(resolvedDayNightCycle.sunriseHour == 6 && resolvedDayNightCycle.sunsetHour == 18 ? "✓" : "✗")}");
            
            if (timeSystem.realSecondsPerGameMinute == 1f)
            {
                Debug.Log($"[问题3] 时间换算: 期望 1秒=1分钟，实际 1秒=1小时 ✗");
            }
        }
    }

    private DayNightCycle ResolveDayNightCycle()
    {
        if (dayNightCycle != null)
        {
            return dayNightCycle;
        }

        Light sun = RenderSettings.sun;
        if (sun != null)
        {
            dayNightCycle = sun.GetComponent<DayNightCycle>();
        }

        return dayNightCycle;
    }
}
