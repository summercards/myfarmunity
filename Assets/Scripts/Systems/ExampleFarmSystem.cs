// Assets/Scripts/Systems/ExampleFarmSystem.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 示例农场系统
/// 演示如何创建一个时间相关的、可存档的游戏系统
/// </summary>
public class ExampleFarmSystem : MonoBehaviour, IFarmSaveable, ISaveParticipant
{
    [Header("作物配置")]
    public List<CropData> cropDatabase = new List<CropData>();

    [Header("农场配置")]
    public int gridWidth = 10;
    public int gridHeight = 10;

    // 运行时数据
    private Dictionary<Vector2Int, Plot> plots = new Dictionary<Vector2Int, Plot>();

    public SaveSection Section => SaveSection.Farm;
    public UnityEngine.Object Owner => this;
    public string ParticipantName => GetType().Name;

    void OnEnable()
    {
        RuntimeRefs.SaveServiceChanged += HandleSaveServiceChanged;
        RegisterToSaveService();
    }

    void Start()
    {
        InitializePlots();
        SubscribeToTimeEvents();
    }

    void OnDisable()
    {
        RuntimeRefs.SaveServiceChanged -= HandleSaveServiceChanged;
        UnregisterFromSaveService();
    }

    void OnDestroy()
    {
        UnsubscribeFromTimeEvents();
    }

    /// <summary>
    /// 初始化农田格子
    /// </summary>
    private void InitializePlots()
    {
        plots.Clear();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                plots[new Vector2Int(x, y)] = new Plot();
            }
        }
    }

    /// <summary>
    /// 订阅时间事件
    /// </summary>
    private void SubscribeToTimeEvents()
    {
        TimeSystemAccessor.SubscribeToDayChange(OnNewDay);
        TimeSystemAccessor.SubscribeToHourChange(OnHourChanged);
        TimeSystemAccessor.SubscribeToLoadComplete(OnTimeLoaded);
        TimeSystemAccessor.SubscribeToSeasonChange(OnSeasonChanged);
    }

    /// <summary>
    /// 取消订阅时间事件
    /// </summary>
    private void UnsubscribeFromTimeEvents()
    {
        // 只有时间系统可用时才取消订阅，避免销毁时警告
        if (TimeSystemAccessor.IsAvailable)
        {
            TimeSystemAccessor.UnsubscribeFromDayChange(OnNewDay);
            TimeSystemAccessor.UnsubscribeFromHourChange(OnHourChanged);
            TimeSystemAccessor.UnsubscribeFromLoadComplete(OnTimeLoaded);
            TimeSystemAccessor.UnsubscribeFromSeasonChange(OnSeasonChanged);
        }
    }

    private void HandleSaveServiceChanged(ISaveService _)
    {
        RegisterToSaveService();
    }

    private void RegisterToSaveService()
    {
        RuntimeRefs.SaveService?.RegisterParticipant(this);
    }

    private void UnregisterFromSaveService()
    {
        RuntimeRefs.SaveService?.UnregisterParticipant(this);
    }

    // === 时间事件处理 ===

    /// <summary>
    /// 新的一天：检查作物生长
    /// </summary>
    private void OnNewDay()
    {
        Debug.Log($"[FarmSystem] 新的一天开始！日期: {TimeSystemAccessor.DateString}");
        UpdateAllCrops();
    }

    /// <summary>
    /// 每小时检查
    /// </summary>
    private void OnHourChanged()
    {
        // 可以在这里做更细致的时间检查
    }

    /// <summary>
    /// 时间加载完成：恢复作物状态
    /// </summary>
    private void OnTimeLoaded()
    {
        Debug.Log($"[FarmSystem] 时间已加载！更新作物状态...");
        UpdateAllCrops();
    }

    /// <summary>
    /// 季节变化：检查作物是否适应新季节
    /// </summary>
    private void OnSeasonChanged()
    {
        Season newSeason = TimeSystemAccessor.Season;
        Debug.Log($"[FarmSystem] 季节变为: {TimeSystemAccessor.SeasonName}");

        // 检查是否有作物不适应当前季节
        foreach (var plot in plots.Values)
        {
            if (plot.HasCrop && !plot.crop.IsSeasonCompatible(newSeason))
            {
                Debug.LogWarning($"[FarmSystem] 作物 {plot.crop.cropName} 不适应 {TimeSystemAccessor.SeasonName}，可能会枯萎");
            }
        }
    }

    // === 农场操作 ===

    /// <summary>
    /// 种植作物
    /// </summary>
    public void PlantCrop(Vector2Int position, string cropId)
    {
        if (!plots.ContainsKey(position))
        {
            Debug.LogWarning($"[FarmSystem] 位置 {position} 不存在");
            return;
        }

        Plot plot = plots[position];
        if (plot.HasCrop)
        {
            Debug.LogWarning($"[FarmSystem] 位置 {position} 已有作物");
            return;
        }

        CropData cropData = cropDatabase.FirstOrDefault(c => c.cropId == cropId);
        if (cropData == null)
        {
            Debug.LogWarning($"[FarmSystem] 未找到作物: {cropId}");
            return;
        }

        plot.PlantCrop(cropData, TimeSystemAccessor.Year, TimeSystemAccessor.DayOfYear);
        Debug.Log($"[FarmSystem] 种植了 {cropData.cropName} 在位置 {position}");
    }

    /// <summary>
    /// 收获作物
    /// </summary>
    public void HarvestCrop(Vector2Int position)
    {
        if (!plots.ContainsKey(position))
        {
            Debug.LogWarning($"[FarmSystem] 位置 {position} 不存在");
            return;
        }

        Plot plot = plots[position];
        if (!plot.HasCrop)
        {
            Debug.LogWarning($"[FarmSystem] 位置 {position} 没有作物");
            return;
        }

        if (!plot.crop.IsFullyGrown())
        {
            Debug.LogWarning($"[FarmSystem] 作物尚未成熟");
            return;
        }

        string cropName = plot.crop.cropName;
        plot.ClearCrop();
        Debug.Log($"[FarmSystem] 收获了 {cropName}");
    }

    /// <summary>
    /// 更新所有作物
    /// </summary>
    private void UpdateAllCrops()
    {
        foreach (var plot in plots.Values)
        {
            if (plot.HasCrop)
            {
                plot.crop.UpdateGrowth(TimeSystemAccessor.DayOfYear, TimeSystemAccessor.Year);
            }
        }
    }

    /// <summary>
    /// 清除作物（如被铲除）
    /// </summary>
    public void ClearCrop(Vector2Int position)
    {
        if (plots.ContainsKey(position))
        {
            plots[position].ClearCrop();
        }
    }

    // === IFarmSaveable 接口实现 ===

    /// <summary>
    /// 获取存档数据
    /// </summary>
    public object GetSaveData()
    {
        FarmSaveData data = new FarmSaveData
        {
            gridWidth = gridWidth,
            gridHeight = gridHeight,
            plots = new List<PlotSaveData>()
        };

        foreach (var kvp in plots)
        {
            PlotSaveData plotData = new PlotSaveData
            {
                x = kvp.Key.x,
                y = kvp.Key.y
            };

            if (kvp.Value.HasCrop)
            {
                plotData.cropId = kvp.Value.crop.cropId;
                plotData.plantYear = kvp.Value.crop.plantYear;
                plotData.plantDayOfYear = kvp.Value.crop.plantDayOfYear;
                plotData.growthStage = kvp.Value.crop.growthStage;
                plotData.isWatered = kvp.Value.isWatered;
            }

            data.plots.Add(plotData);
        }

        return data;
    }

    /// <summary>
    /// 加载存档数据
    /// </summary>
    public void LoadSaveData(string jsonData, GameTimeSystem timeSystem)
    {
        if (string.IsNullOrEmpty(jsonData)) return;

        try
        {
            FarmSaveData data = JsonUtility.FromJson<FarmSaveData>(jsonData);

            gridWidth = data.gridWidth;
            gridHeight = data.gridHeight;

            // 重建农田
            plots.Clear();
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    plots[new Vector2Int(x, y)] = new Plot();
                }
            }

            // 恢复作物
            foreach (var plotData in data.plots)
            {
                Vector2Int pos = new Vector2Int(plotData.x, plotData.y);
                if (plots.ContainsKey(pos))
                {
                    if (!string.IsNullOrEmpty(plotData.cropId))
                    {
                        CropData cropData = cropDatabase.FirstOrDefault(c => c.cropId == plotData.cropId);
                        if (cropData != null)
                        {
                            Plot plot = plots[pos];
                            plot.PlantCrop(cropData, plotData.plantYear, plotData.plantDayOfYear);
                            plot.crop.growthStage = plotData.growthStage;
                            plot.isWatered = plotData.isWatered;
                        }
                    }
                }
            }

            Debug.Log($"[FarmSystem] 存档已加载，共 {data.plots.Count} 个地块");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FarmSystem] 加载存档失败: {e.Message}");
        }
    }

    public object CaptureSaveData()
    {
        return GetSaveData();
    }

    public void RestoreSaveData(string jsonData, GameTimeSystem timeSystem)
    {
        LoadSaveData(jsonData, timeSystem);
    }
}

// === 农场数据结构 ===

/// <summary>
/// 农田地块
/// </summary>
[System.Serializable]
public class Plot
{
    public Crop crop = null;
    public bool isWatered = false;

    public bool HasCrop => crop != null;

    public void PlantCrop(CropData cropData, int year, int dayOfYear)
    {
        crop = new Crop(cropData, year, dayOfYear);
    }

    public void ClearCrop()
    {
        crop = null;
        isWatered = false;
    }
}

/// <summary>
/// 作物
/// </summary>
[System.Serializable]
public class Crop
{
    public string cropId;
    public string cropName;
    public int growthStage;        // 当前生长阶段（0开始）
    public int totalGrowthStages;   // 总生长阶段数
    public int daysPerStage;        // 每阶段所需天数
    public int plantYear;
    public int plantDayOfYear;

    private CropData data;

    public Crop(CropData cropData, int year, int dayOfYear)
    {
        data = cropData;
        cropId = cropData.cropId;
        cropName = cropData.cropName;
        totalGrowthStages = cropData.totalGrowthStages;
        daysPerStage = cropData.daysPerStage;
        plantYear = year;
        plantDayOfYear = dayOfYear;
        growthStage = 0;
    }

    /// <summary>
    /// 更新生长状态
    /// </summary>
    public void UpdateGrowth(int currentDayOfYear, int currentYear)
    {
        if (IsFullyGrown()) return;

        int daysPassed = CalculateDaysPassed(currentDayOfYear, currentYear);
        int expectedStage = Mathf.Min(totalGrowthStages - 1, daysPassed / daysPerStage);
        growthStage = expectedStage;
    }

    private int CalculateDaysPassed(int currentDayOfYear, int currentYear)
    {
        return (currentYear * 365 + currentDayOfYear) - (plantYear * 365 + plantDayOfYear);
    }

    public bool IsFullyGrown()
    {
        return growthStage >= totalGrowthStages - 1;
    }

    public float GetGrowthProgress()
    {
        return (float)growthStage / (totalGrowthStages - 1);
    }

    /// <summary>
    /// 检查作物是否适应指定季节
    /// </summary>
    public bool IsSeasonCompatible(Season season)
    {
        return data != null && data.IsSeasonCompatible(season);
    }
}

/// <summary>
/// 作物数据
/// </summary>
[Serializable]
public class CropData
{
    public string cropId;
    public string cropName;
    public int totalGrowthStages = 3;      // 总生长阶段数
    public int daysPerStage = 2;           // 每阶段所需天数
    public List<Season> compatibleSeasons = new List<Season> { Season.Spring, Season.Summer }; // 适宜季节

    /// <summary>
    /// 检查是否适应季节
    /// </summary>
    public bool IsSeasonCompatible(Season season)
    {
        return compatibleSeasons.Contains(season);
    }
}

// === 存档数据结构 ===

/// <summary>
/// 农场存档数据
/// </summary>
[Serializable]
public class FarmSaveData
{
    public int gridWidth;
    public int gridHeight;
    public List<PlotSaveData> plots;
}

/// <summary>
/// 地块存档数据
/// </summary>
[Serializable]
public class PlotSaveData
{
    public int x;
    public int y;
    public string cropId;
    public int plantYear;
    public int plantDayOfYear;
    public int growthStage;
    public bool isWatered;
}
