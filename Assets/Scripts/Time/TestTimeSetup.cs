// Assets/Scripts/Time/TestTimeSetup.cs
using UnityEngine;

/// <summary>
/// 测试脚本：在运行时设置时间系统
/// </summary>
public class TestTimeSetup : MonoBehaviour
{
    void Start()
    {
        Debug.Log("开始测试时间系统设置...");

        // 创建或加载 GameTimeSystem
        GameTimeSystem timeSystem = Resources.Load<GameTimeSystem>("Game/DefaultTimeSystem");

        if (timeSystem == null)
        {
            timeSystem = ScriptableObject.CreateInstance<GameTimeSystem>();
            timeSystem.realSecondsPerGameMinute = 1f;
            timeSystem.startHour = 6f;
            timeSystem.startDay = 1;
            timeSystem.startMonth = 1;
            timeSystem.startYear = 2024;
            timeSystem.startSeason = Season.Spring;
            timeSystem.currentWeather = WeatherType.Sunny;
            timeSystem.Initialize();

            Debug.Log("创建了临时 GameTimeSystem 实例");
        }
        else
        {
            timeSystem.Initialize();
            Debug.Log("加载了 GameTimeSystem 资源");
        }

        // 创建 TimeManager
        GameObject timeManager = GameObject.Find("TimeManager");
        if (timeManager == null)
        {
            timeManager = new GameObject("TimeManager");
        }

        TimeController controller = timeManager.GetComponent<TimeController>();
        if (controller == null)
        {
            controller = timeManager.AddComponent<TimeController>();
        }

        controller.timeSystem = timeSystem;
        controller.allowTimeControl = true;
        controller.showDebugInfo = false;
        controller.autoInitialize = true;
        controller.autoStart = true;

        Debug.Log("TimeManager 设置完成");

        // 配置 DayNightCycle
        Light directionalLight = FindObjectOfType<Light>();
        if (directionalLight != null)
        {
            DayNightCycle dayNightCycle = directionalLight.GetComponent<DayNightCycle>();
            if (dayNightCycle == null)
            {
                dayNightCycle = directionalLight.gameObject.AddComponent<DayNightCycle>();
            }

            dayNightCycle.timeSystem = timeSystem;
            dayNightCycle.directionalLight = directionalLight;
            dayNightCycle.useFog = true;

            Debug.Log("DayNightCycle 设置完成");
        }
        else
        {
            Debug.LogWarning("未找到 Directional Light");
        }

        Debug.Log("时间系统设置完成！");
    }
}
