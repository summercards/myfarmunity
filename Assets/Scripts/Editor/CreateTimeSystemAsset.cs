// Assets/Scripts/Editor/CreateTimeSystemAsset.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 创建 GameTimeSystem 资源
/// </summary>
public class CreateTimeSystemAsset
{
    [MenuItem("Tools/时间系统/创建 DefaultTimeSystem 资源")]
    public static void CreateDefaultTimeSystemAsset()
    {
        // 确保 Resources 文件夹存在
        string resourcesPath = "Assets/Resources";
        if (!System.IO.Directory.Exists(resourcesPath))
        {
            System.IO.Directory.CreateDirectory(resourcesPath);
        }

        // 检查是否已存在
        string assetPath = "Assets/Resources/DefaultTimeSystem.asset";
        GameTimeSystem existingAsset = AssetDatabase.LoadAssetAtPath<GameTimeSystem>(assetPath);

        if (existingAsset != null)
        {
            if (EditorUtility.DisplayDialog("资源已存在", "DefaultTimeSystem.asset 已存在，是否覆盖？", "覆盖", "取消"))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
            else
            {
                return;
            }
        }

        // 创建资源
        GameTimeSystem timeSystem = ScriptableObject.CreateInstance<GameTimeSystem>();

        // 配置默认值
        timeSystem.startHour = 6f;
        timeSystem.startDay = 1;
        timeSystem.startMonth = 1;
        timeSystem.startYear = 2024;
        timeSystem.startSeason = Season.Spring;
        timeSystem.daysPerSeason = 28;
        timeSystem.realSecondsPerGameMinute = 1f;
        timeSystem.currentWeather = WeatherType.Sunny;
        timeSystem.weatherChangeInterval = 6f;

        // 保存资源
        AssetDatabase.CreateAsset(timeSystem, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("已创建 DefaultTimeSystem 资源");
        Selection.activeObject = timeSystem;
    }
}
#endif
