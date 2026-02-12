# 时间系统配置指南

## 快速设置

### 步骤 1：创建场景中的必要组件

在你的游戏场景中，按顺序创建以下 GameObject：

#### 1. 创建 GameManager
```
GameObject: GameManager
├─ Script: GameManager (Assets/Scripts/Systems/GameManager.cs)
│  ├─ autoLoadLatestSave: ✓ 勾选（启动时自动加载存档）
│  ├─ defaultSaveName: autosave
│  └─ showStartupLog: ✓ 勾选（显示启动日志）
```

#### 2. 创建 SaveManager
```
GameObject: SaveManager
├─ Script: SaveManager (Assets/Scripts/Save/SaveManager.cs)
│  ├─ Time System: 拖入 GameTimeSystem 资源（或通过 GameObject 查找）
│  ├─ Auto Save Enabled: ✓ 勾选
│  ├─ Auto Save Interval: 300（5分钟）
│  └─ Max Save Slots: 10
```

#### 3. 创建 TimeManager
```
GameObject: TimeManager
├─ Script: TimeController (Assets/Scripts/Time/TimeController.cs)
│  ├─ Time System: 拖入 GameTimeSystem 资源
│  ├─ Auto Initialize: ✓ 勾选
│  ├─ Auto Start: ✓ 勾选
│  ├─ Allow Time Control: ✓ 勾选
│  └─ Show Debug Info: 可选勾选
```

#### 4. 创建 GameTimeSystem 资源
```
1. 在 Project 窗口右键 → Create → Game → Time System
2. 命名为: DefaultTimeSystem
3. 配置参数：
   ├─ Start Hour: 6
   ├─ Start Day: 1
   ├─ Start Month: 1
   ├─ Start Year: 2024
   ├─ Start Season: Spring
   └─ Real Seconds Per Game Minute: 1
```

### 步骤 2：组件关系配置

| 组件 | 需要引用 |
|------|----------|
| GameManager | SaveManager（通过代码自动查找） |
| SaveManager | GameTimeSystem（通过 TimeController 查找） |
| TimeController | GameTimeSystem（手动拖入） |
| DayNightCycle | GameTimeSystem（手动拖入） |
| TimeUI | GameTimeSystem（手动拖入） |

### 步骤 3：添加你的游戏系统

每个游戏系统（如农场、商店、NPC等）需要：

1. **实现对应接口**：
   - `IFarmSaveable` - 农场/作物系统
   - `IShopSaveable` - 商店系统
   - `INPCSaveable` - NPC系统
   - `IQuestSaveable` - 任务系统
   - `IInventorySaveable` - 背包系统
   - `IPlayerSaveable` - 玩家系统

2. **在 SaveManager 中配置引用**：
   - 将你的系统 GameObject 拖入对应的字段

3. **在 Start 中订阅时间事件**：
```csharp
void Start()
{
    // 订阅时间事件
    TimeSystemAccessor.SubscribeToDayChange(OnNewDay);
    TimeSystemAccessor.SubscribeToLoadComplete(OnTimeLoaded);
}

private void OnNewDay()
{
    Debug.Log($"新的一天: {TimeSystemAccessor.DateString}");
    // 更新你的游戏逻辑
}

private void OnTimeLoaded()
{
    Debug.Log("存档已加载");
    // 更新你的游戏状态
}
```

## 工作流程

### 游戏启动流程
```
1. GameManager.Awake()
   └─ 创建单例，DontDestroyOnLoad

2. TimeController.Start()
   └─ 查找/初始化 GameTimeSystem

3. SaveManager.Start()
   └─ 初始化存档路径

4. GameManager.Start()
   └─ 延迟 0.1 秒后调用 InitializeGame()

5. GameManager.InitializeGame()
   ├─ 检查 SaveManager
   ├─ 检查 TimeSystem
   ├─ 查找最新存档
   ├─ 调用 SaveManager.LoadGame()
   │  └─ 调用 GameTimeSystem.LoadSaveData()
   │     └─ 触发 onLoadComplete 事件
   │        └─ 所有订阅的系统更新状态
   └─ 完成
```

### 保存流程
```
用户触发保存
  ↓
GameManager.SaveGame()
  ↓
SaveManager.SaveGame()
  ├─ 收集 GameTimeSystem.GetSaveData()
  ├─ 收集各子系统 GetSaveData()
  └─ 写入 JSON 文件
```

### 加载流程
```
用户加载游戏
  ↓
GameManager.LoadGame()
  ↓
SaveManager.LoadGame()
  ├─ 读取 JSON 文件
  ├─ 调用 GameTimeSystem.LoadSaveData()
  │  └─ 触发 onLoadComplete 事件
  ├─ 各子系统响应 onLoadComplete
  └─ 更新游戏状态
```

## 常见问题

### Q1: 游戏启动后时间重置了？
**原因**：没有设置 GameManager，或者 autoLoadLatestSave 没有勾选

**解决**：
1. 在场景中添加 GameManager
2. 勾选 autoLoadLatestSave
3. 确保之前有保存过存档

### Q2: 存档加载后作物没有恢复？
**原因**：农场系统没有订阅 onLoadComplete 事件

**解决**：在你的系统 Start() 中添加：
```csharp
TimeSystemAccessor.SubscribeToLoadComplete(OnTimeLoaded);
```

### Q3: 场景切换后时间系统丢失？
**原因**：没有使用 DontDestroyOnLoad

**解决**：
1. GameManager 和 SaveManager 已经自动使用 DontDestroyOnLoad
2. 确保其他持久化系统也添加 DontDestroyOnLoad

### Q4: 如何禁用自动保存？
**解决**：在 SaveManager 中取消勾选 autoSaveEnabled

### Q5: 如何修改存档路径？
**解决**：在 SaveManager 中修改 saveFolder（默认：saves）

## 调试技巧

### 启用调试日志
```
GameManager.showStartupLog = ✓
TimeController.showDebugInfo = ✓
```

### 查看存档列表
```csharp
var saves = SaveManager.Instance.GetSaveList();
foreach (var save in saves)
{
    Debug.Log($"存档: {save.saveName}, 时间: {save.saveTime}, 日期: {save.GetDisplayDate()}");
}
```

### 手动触发保存
```csharp
SaveManager.Instance.SaveGame("test_save");
```

### 手动触发加载
```csharp
SaveManager.Instance.LoadGame("test_save");
```

### 清空所有存档
```csharp
var saves = SaveManager.Instance.GetSaveList();
foreach (var save in saves)
{
    SaveManager.Instance.DeleteSave(save.saveName);
}
```

## 测试步骤

1. **首次运行**：
   - 创建好所有组件
   - 运行游戏
   - 观察控制台日志

2. **测试保存**：
   - 等待一段时间（或手动保存）
   - 退出游戏

3. **测试加载**：
   - 重新运行游戏
   - 检查时间是否继续
   - 检查各系统状态是否恢复

4. **测试新游戏**：
   - 调用 `GameManager.Instance.NewGame()`
   - 检查时间是否重置
