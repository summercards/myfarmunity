// Assets/Scripts/TimeController.cs
using UnityEngine;

/// <summary>
/// 时间控制器
/// 在场景中管理时间系统的运行
/// </summary>
public class TimeController : MonoBehaviour
{
    [Header("时间系统引用")]
    public GameTimeSystem timeSystem;

    [Header("运行时配置")]
    [Tooltip("是否在游戏开始时自动初始化")]
    public bool autoInitialize = true;

    [Tooltip("是否在游戏开始时自动运行")]
    public bool autoStart = true;

    [Header("调试选项")]
    [Tooltip("显示调试信息")]
    public bool showDebugInfo = false;

    [Tooltip("允许通过键盘调整时间")]
    public bool allowTimeControl = true;

    // 快捷键配置
    [Tooltip("暂停/恢复时间")]
    public KeyCode pauseKey = KeyCode.P;
    [Tooltip("快速前进1小时")]
    public KeyCode fastForwardKey = KeyCode.RightArrow;
    [Tooltip("快速后退1小时")]
    public KeyCode rewindKey = KeyCode.LeftArrow;
    [Tooltip("跳到早晨")]
    public KeyCode skipToMorningKey = KeyCode.M;
    [Tooltip("跳到晚上")]
    public KeyCode skipToNightKey = KeyCode.N;
    [Tooltip("增加时间速度")]
    public KeyCode speedUpKey = KeyCode.UpArrow;
    [Tooltip("减少时间速度")]
    public KeyCode speedDownKey = KeyCode.DownArrow;

    private bool isInitialized = false;

    void Awake()
    {
        ResolveTimeSystem();

        if (timeSystem != null)
        {
            RuntimeRefs.RegisterTimeSystem(timeSystem);
        }

        if (timeSystem == null && showDebugInfo)
        {
            Debug.LogWarning("[TimeController] 未找到 GameTimeSystem，等待配置脚本设置...");
        }
    }

    void OnEnable()
    {
        RuntimeRefs.TimeSystemChanged += HandleTimeSystemChanged;
    }

    void OnDisable()
    {
        RuntimeRefs.TimeSystemChanged -= HandleTimeSystemChanged;
    }

    void Start()
    {
        ResolveTimeSystem();

        if (autoInitialize && timeSystem != null)
        {
            Initialize();
        }

        if (autoStart && timeSystem != null)
        {
            Resume();
        }
    }

    private void ResolveTimeSystem()
    {
        if (timeSystem == null)
        {
            timeSystem = RuntimeRefs.TimeSystem;
        }

        if (timeSystem == null)
        {
            timeSystem = Resources.Load<GameTimeSystem>("DefaultTimeSystem");
            if (timeSystem != null && showDebugInfo)
            {
                Debug.Log("[TimeController] 从 Resources 加载了 DefaultTimeSystem");
            }
        }
    }

    private void HandleTimeSystemChanged(GameTimeSystem system)
    {
        if (timeSystem != null || system == null)
        {
            return;
        }

        timeSystem = system;
        if (showDebugInfo)
        {
            Debug.Log("[TimeController] 已从 RuntimeRefs 绑定 GameTimeSystem");
        }
    }

    void Update()
    {
        if (!isInitialized || timeSystem == null) return;

        // 更新时间系统
        timeSystem.Tick(Time.deltaTime);

        // 处理输入
        HandleInput();

        // 显示调试信息
        if (showDebugInfo)
        {
            ShowDebugInfo();
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo || !isInitialized) return;

        GUI.Box(new Rect(10, 10, 250, 200), "时间系统调试");

        int y = 35;
        GUI.Label(new Rect(20, y, 220, 20), $"时间: {timeSystem.TimeString}");
        y += 20;
        GUI.Label(new Rect(20, y, 220, 20), $"日期: {timeSystem.DateString}");
        y += 20;
        GUI.Label(new Rect(20, y, 220, 20), $"季节: {TimeHelpers.GetSeasonName(timeSystem.Season)}");
        y += 20;
        GUI.Label(new Rect(20, y, 220, 20), $"时段: {TimeHelpers.GetTimeOfDayName(timeSystem.CurrentTimeOfDay)}");
        y += 20;
        GUI.Label(new Rect(20, y, 220, 20), $"天气: {timeSystem.GetWeatherName(timeSystem.CurrentWeather)}");
        y += 20;
        GUI.Label(new Rect(20, y, 220, 20), $"速度: {timeSystem.realSecondsPerGameMinute:F1}秒/分钟");
        y += 20;
        GUI.Label(new Rect(20, y, 220, 20), $"状态: {(timeSystem.IsPaused ? "暂停" : "运行")}");
        y += 20;

        // 控制说明
        y += 10;
        GUI.Label(new Rect(20, y, 220, 20), "控制:");
        y += 15;
        GUI.Label(new Rect(20, y, 220, 20), $"P: 暂停/恢复");
        y += 15;
        GUI.Label(new Rect(20, y, 220, 20), $"←/→: 前进/后退1小时");
        y += 15;
        GUI.Label(new Rect(20, y, 220, 20), $"M: 跳到早晨");
        y += 15;
        GUI.Label(new Rect(20, y, 220, 20), $"N: 跳到晚上");
        y += 15;
        GUI.Label(new Rect(20, y, 220, 20), $"↑/↓: 调整速度");
    }

    /// <summary>
    /// 初始化时间系统
    /// </summary>
    public void Initialize()
    {
        if (timeSystem == null)
        {
            Debug.LogError("[TimeController] 无法初始化：TimeSystem 为空！");
            return;
        }

        timeSystem.Initialize();
        isInitialized = true;

        Debug.Log("[TimeController] 时间系统已初始化");
    }

    /// <summary>
    /// 暂停时间
    /// </summary>
    public void Pause()
    {
        if (timeSystem == null) return;
        timeSystem.Pause();
        Debug.Log("[TimeController] 时间已暂停");
    }

    /// <summary>
    /// 恢复时间
    /// </summary>
    public void Resume()
    {
        if (timeSystem == null) return;
        timeSystem.Resume();
        Debug.Log("[TimeController] 时间已恢复");
    }

    /// <summary>
    /// 切换暂停状态
    /// </summary>
    public void TogglePause()
    {
        if (timeSystem == null) return;

        if (timeSystem.IsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    /// <summary>
    /// 快速前进
    /// </summary>
    public void FastForward(int hours = 1)
    {
        if (timeSystem == null) return;
        timeSystem.FastForward(hours);
        Debug.Log($"[TimeController] 时间快速前进 {hours} 小时");
    }

    /// <summary>
    /// 设置指定时间
    /// </summary>
    public void SetTime(int hour, int minute)
    {
        if (timeSystem == null) return;
        timeSystem.SetTime(hour, minute);
        Debug.Log($"[TimeController] 时间设置为 {hour:D2}:{minute:D2}");
    }

    /// <summary>
    /// 设置指定日期
    /// </summary>
    public void SetDate(int year, int month, int day)
    {
        if (timeSystem == null) return;
        timeSystem.SetDate(year, month, day);
        Debug.Log($"[TimeController] 日期设置为 {year}年{month}月{day}日");
    }

    /// <summary>
    /// 跳到早晨（6:00）
    /// </summary>
    public void SkipToMorning()
    {
        if (timeSystem == null) return;
        timeSystem.SetTime(6, 0);
        Debug.Log("[TimeController] 跳到早晨 6:00");
    }

    /// <summary>
    /// 跳到晚上（18:00）
    /// </summary>
    public void SkipToNight()
    {
        if (timeSystem == null) return;
        timeSystem.SetTime(18, 0);
        Debug.Log("[TimeController] 跳到晚上 18:00");
    }

    /// <summary>
    /// 增加时间速度
    /// </summary>
    public void SpeedUp()
    {
        if (timeSystem == null) return;
        float newSpeed = timeSystem.realSecondsPerGameMinute * 0.5f;
        timeSystem.SetTimeScale(newSpeed);
        Debug.Log($"[TimeController] 时间速度: {newSpeed:F1}秒/分钟");
    }

    /// <summary>
    /// 减少时间速度
    /// </summary>
    public void SpeedDown()
    {
        if (timeSystem == null) return;
        float newSpeed = timeSystem.realSecondsPerGameMinute * 2f;
        timeSystem.SetTimeScale(newSpeed);
        Debug.Log($"[TimeController] 时间速度: {newSpeed:F1}秒/分钟");
    }

    /// <summary>
    /// 改变天气
    /// </summary>
    public void ChangeWeather(WeatherType weather)
    {
        if (timeSystem == null) return;
        timeSystem.SetWeather(weather);
        Debug.Log($"[TimeController] 天气变为: {timeSystem.GetWeatherName(weather)}");
    }

    /// <summary>
    /// 随机改变天气
    /// </summary>
    public void RandomWeather()
    {
        if (timeSystem == null) return;
        WeatherType randomWeather = (WeatherType)Random.Range(0, System.Enum.GetValues(typeof(WeatherType)).Length);
        ChangeWeather(randomWeather);
    }

    /// <summary>
    /// 获取时间系统引用
    /// </summary>
    public GameTimeSystem GetTimeSystem()
    {
        return timeSystem;
    }

    /// <summary>
    /// 设置时间系统引用
    /// </summary>
    public void SetTimeSystem(GameTimeSystem system)
    {
        timeSystem = system;
    }

    /// <summary>
    /// 处理输入
    /// </summary>
    private void HandleInput()
    {
        if (!allowTimeControl || timeSystem == null) return;

        // 暂停/恢复
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }

        // 快速前进/后退
        if (Input.GetKeyDown(fastForwardKey))
        {
            FastForward(1);
        }

        if (Input.GetKeyDown(rewindKey))
        {
            FastForward(-1);
        }

        // 跳到早晨/晚上
        if (Input.GetKeyDown(skipToMorningKey))
        {
            SkipToMorning();
        }

        if (Input.GetKeyDown(skipToNightKey))
        {
            SkipToNight();
        }

        // 调整速度
        if (Input.GetKeyDown(speedUpKey))
        {
            SpeedUp();
        }

        if (Input.GetKeyDown(speedDownKey))
        {
            SpeedDown();
        }
    }

    /// <summary>
    /// 显示调试信息
    /// </summary>
    private void ShowDebugInfo()
    {
        // 在 OnGUI 中绘制
    }

    /// <summary>
    /// 获取时间摘要
    /// </summary>
    public string GetTimeSummary()
    {
        if (timeSystem == null) return "时间系统未初始化";
        return timeSystem.GetTimeSummary();
    }

    /// <summary>
    /// 保存时间数据
    /// </summary>
    public TimeSaveData SaveTime()
    {
        return timeSystem != null ? timeSystem.GetSaveData() : null;
    }

    /// <summary>
    /// 加载时间数据
    /// </summary>
    public void LoadTime(TimeSaveData data)
    {
        if (timeSystem != null && data != null)
        {
            timeSystem.LoadSaveData(data);
            Debug.Log("[TimeController] 时间数据已加载");
        }
    }
}
