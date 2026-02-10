# 🚀 时间系统自动配置指南

## 方式一：使用编辑器向导（推荐）

### 操作步骤

1. **打开向导窗口**
   ```
   Unity 顶部菜单栏 → Tools → Time System → Setup Wizard
   ```

2. **使用一键配置**
   ```
   点击 "🚀 一键完整配置" 按钮
   等待配置完成（约2-3秒）
   点击 "确定" 关闭提示框
   ```

3. **测试运行**
   ```
   点击 Play 按钮
   观察右上角的时间UI
   测试快捷键控制
   ```

---

## 方式二：使用快速设置脚本

### 步骤1：添加脚本到场景

```
1. Hierarchy → 右键 → Create Empty
2. 命名为：TimeSetupHelper
3. Add Component → Time System Quick Setup
```

### 步骤2：运行配置

#### 方法A：在 Inspector 中点击
```
1. 选中 TimeSetupHelper 对象
2. 在 Inspector 中找到 "Time System Quick Setup" 组件
3. 点击 "快速配置" 按钮
```

#### 方法B：使用右键菜单
```
1. 选中 TimeSetupHelper 对象
2. 右键（在 Inspector 或 Hierarchy 中）
3. 找到 "快速配置时间系统"
4. 点击
```

#### 方法C：自动配置
```
1. 在 Inspector 中，勾选 "Auto Setup On Awake"
2. 运行游戏时会自动配置
```

### 步骤3：清除配置（如需重新配置）

```
1. 选中 TimeSetupHelper 对象
2. 点击 "清除所有时间系统" 按钮
3. 确认对话框
4. 然后再次点击 "快速配置"
```

---

## 配置完成后你会得到

### ✅ 创建的对象

```
Hierarchy 结构：

📁 Scene
  ├─ 🎯 TimeSetupHelper（你的设置对象）
  │   └─ 🕐 TimeManager（自动创建）
  │       └─ TimeController (Script)
  │
  ├─ 🌞 Directional Light（已添加组件）
  │   └─ DayNightCycle (Script)
  │
  ├─ 📺 TimeCanvas（自动创建）
  │   ├─ TimePanel
  │   │   ├─ TimeText
  │   │   ├─ DateText
  │   │   ├─ SeasonText
  │   │   └─ WeatherText
  │   └─ TimeUI (Script)
  │
  └─ 🔆 Directional Light（自动创建，如果没有）
```

### ✅ 创建的资源

```
Project 结构：

Assets/
  └─ Resources/
      └─ Game/
          └─ DefaultTimeSystem.asset（时间系统资源）
```

---

## 运行时功能

### 显示效果

```
┌─────────────────────────────┐
│  12:30  2024年1月5日 春季    │
│  晴天                       │
│                             │
│  [游戏画面区域]            │
│                             │
│  ↑ 时间显示在右上角         │
│  ↑ 光照随时间变化           │
└─────────────────────────────┘
```

### 快捷键控制

| 按键 | 功能 |
|------|------|
| **P** | 暂停/恢复时间 |
| **→** | 前进1小时 |
| **←** | 后退1小时 |
| **M** | 跳到早晨 6:00 |
| **N** | 跳到晚上 18:00 |
| **↑** | 加速时间（流逝更快） |
| **↓** | 减速时间（流逝更慢） |

### 调试信息显示

```
要显示调试信息：

方法1：编辑器向导
- 打开 Tools → Time System → Setup Wizard
- 勾选 "Show Debug Info"

方法2：手动设置
- 选中 TimeManager 对象
- Inspector → Time Controller
- 勾选 Show Debug Info

运行后会显示：
┌─────────────────────────────┐
│ 时间系统调试                │
│                             │
│ 时间: 12:30                 │
│ 日期: 2024年1月5日           │
│ 季节: 春季                  │
│ 时段: 中午                  │
│ 天气: 晴天                  │
│ 速度: 1.0秒/分钟            │
│ 状态: 运行中                │
└─────────────────────────────┘
```

---

## 自定义配置

### 修改时间速度

```
方式1：编辑器配置
1. 找到 DefaultTimeSystem 资源
   - Assets/Resources/Game/DefaultTimeSystem
2. Inspector → Time System (Script)
3. 修改 Real Seconds Per Game Minute
   - 0.5 = 2倍速
   - 1.0 = 正常速度
   - 2.0 = 0.5倍速

方式2：运行时调整
- 按 ↑ 或 ↓ 键调整
```

### 修改开始时间

```
1. 找到 DefaultTimeSystem 资源
2. Inspector → Time System (Script)
3. 修改 Start Hour（0-24）
4. 修改 Start Day/Month/Year
```

### 修改光照配置

```
1. 找到 Directional Light
2. Inspector → Day Night Cycle (Script)
3. 调整各项参数：
   - Sunrise/Noon/Sunset Hour：日出/正午/日落时间
   - Max/Min Light Intensity：最大/最小光照强度
   - Day/Night Ambient Color：白天/夜晚环境光颜色
```

---

## 代码访问

### 获取时间系统

```csharp
using UnityEngine;

public class MyScript : MonoBehaviour
{
    void Start()
    {
        // 方法1：查找 TimeController
        var controller = FindObjectOfType<TimeController>();
        var timeSystem = controller.GetTimeSystem();

        // 方法2：直接查找 GameTimeSystem
        var timeSystem2 = FindObjectOfType<GameTimeSystem>();

        // 方法3：从 Resources 加载
        var timeSystem3 = Resources.Load<GameTimeSystem>("Game/DefaultTimeSystem");
    }

    void Update()
    {
        var timeSystem = FindObjectOfType<GameTimeSystem>();

        // 检查是否白天
        if (timeSystem.IsDayTime)
        {
            Debug.Log("现在是白天");
        }

        // 获取时间信息
        Debug.Log($"时间: {timeSystem.TimeString}");
        Debug.Log($"日期: {timeSystem.DateString}");
        Debug.Log($"季节: {TimeHelpers.GetSeasonName(timeSystem.Season)}");
        Debug.Log($"天气: {timeSystem.GetWeatherName(timeSystem.CurrentWeather)}");
    }
}
```

### 监听时间事件

```csharp
using UnityEngine;

public class TimeEventListener : MonoBehaviour
{
    private GameTimeSystem timeSystem;

    void Start()
    {
        timeSystem = FindObjectOfType<GameTimeSystem>();

        // 订阅事件
        timeSystem.onHourChanged.AddListener(OnHourChanged);
        timeSystem.onDayChanged.AddListener(OnDayChanged);
        timeSystem.onSeasonChanged.AddListener(OnSeasonChanged);
        timeSystem.onWeatherChanged.AddListener(OnWeatherChanged);
    }

    void OnDestroy()
    {
        if (timeSystem != null)
        {
            // 取消订阅
            timeSystem.onHourChanged.RemoveListener(OnHourChanged);
            timeSystem.onDayChanged.RemoveListener(OnDayChanged);
            timeSystem.onSeasonChanged.RemoveListener(OnSeasonChanged);
            timeSystem.onWeatherChanged.RemoveListener(OnWeatherChanged);
        }
    }

    void OnHourChanged()
    {
        Debug.Log($"新的一小时: {timeSystem.Hour}:00");
    }

    void OnDayChanged()
    {
        Debug.Log($"新的一天: {timeSystem.Month}月{timeSystem.Day}日");
    }

    void OnSeasonChanged()
    {
        Debug.Log($"新的季节: {TimeHelpers.GetSeasonName(timeSystem.Season)}");
    }

    void OnWeatherChanged()
    {
        Debug.Log($"天气变化: {timeSystem.GetWeatherName(timeSystem.CurrentWeather)}");
    }
}
```

---

## 常见问题

### Q1: 点击一键配置后没有反应

```
解决方法：
1. 检查 Console 窗口是否有错误信息
2. 尝试刷新场景：File → Save，然后重新打开
3. 尝试使用方式二的快速设置脚本
```

### Q2: 时间UI没有显示

```
解决方法：
1. 检查 Canvas 的 Render Mode 是否为 Screen Space - Overlay
2. 检查 TimeUI 组件是否已添加
3. 检查 Text 引用是否已连接
4. 检查 Canvas 的位置是否正确
```

### Q3: 光照不变化

```
解决方法：
1. 检查 Directional Light 是否存在
2. 检查 DayNightCycle 组件是否已添加
3. 检查 TimeSystem 引用是否已连接
4. 点击 Play 测试运行
```

### Q4: 想要重新配置

```
使用方式二的清除功能：
1. 选中 TimeSetupHelper 对象
2. 点击 "清除所有时间系统" 按钮
3. 确认对话框
4. 再次点击 "快速配置"
```

### Q5: 如何禁用快捷键

```
1. 找到 TimeManager 对象
2. Inspector → Time Controller (Script)
3. 取消勾选 Allow Time Control
```

---

## 下一步

### 美化UI

```
1. 调整 TimePanel 的大小和位置
2. 修改文本颜色和字体
3. 添加图标
4. 添加背景图片或渐变
```

### 添加功能

```
- 睡觉功能（床交互）
- 日历UI
- 天气预告
- 季节特效（如冬天下雪）
- NPC 按时间出现
- 商店按时间开关
```

---

## 技术支持

```
遇到问题？
1. 查看 Console 的错误信息
2. 查看脚本注释
3. 参考 TIME_SYSTEM_SETUP_GUIDE.md
```

---

## 文件位置

```
所有脚本都在：Assets/Scripts/Time/

- TimeOfDay.cs - 枚举和工具类
- GameTimeSystem.cs - 时间系统核心
- TimeController.cs - 时间控制器
- DayNightCycle.cs - 昼夜循环
- TimeUI.cs - 时间UI
- TimeSystemSetupWizard.cs - 编辑器向导
- TimeSystemQuickSetup.cs - 快速设置脚本
- TIME_SYSTEM_SETUP_GUIDE.md - 详细设置指南
- AUTO_SETUP_GUIDE.md - 本文档
```

---

## 🎉 完成！

现在你的游戏拥有了完整的时间系统！

开始享受游戏开发的乐趣吧！🚀
