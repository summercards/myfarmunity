# 🕐 时间系统设置指南

## 📋 系统概述

时间系统包含以下功能：
- ⏰ 游戏时间管理（小时、分钟）
- 📅 日期系统（年、月、日）
- 🍂 季节系统（春、夏、秋、冬）
- 🌤️ 天气系统（晴、云、雨、雪等）
- 🌅 昼夜循环（光照、环境光、雾效）
- 📊 时间UI显示

---

## 第一步：创建时间系统资源

### 操作1：创建 GameTimeSystem 资源

```
1. 打开 Project 窗口
2. 在 Assets/ 文件夹右键
3. Create → Game → Time System
4. 命名为：DefaultTimeSystem
```

### 操作2：配置时间系统参数

在 Inspector 中配置 DefaultTimeSystem：

```
┌─────────────────────────────────────────────────────────┐
│ Inspector: DefaultTimeSystem                            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ⏰ 时间配置                                             │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Real Seconds Per Game Minute  [ 1.0          ]   │ ← 1现实秒=1游戏分钟
│  │ Start Hour                     [ 6            ]   │ ← 开始时间6点
│  │ Start Day                      [ 1            ]   │ ← 第1天
│  │ Start Month                    [ 1            ]   │ ← 1月
│  │ Start Year                     [ 2024         ]   │ ← 2024年
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  🍂 季节配置                                             │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Start Season                   [ Spring       ▼ ]│ ← 春季开始
│  │ Days Per Season                [ 28           ]   │ ← 每季28天
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  🌤️ 天气配置                                             │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Current Weather                 [ Sunny        ▼ ]│ ← 初始天气
│  │ Weather Change Interval        [ 6            ]   │ ← 6小时换天气
│  └─────────────────────────────────────────────────┘   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**建议配置**：
- `Real Seconds Per Game Minute`: 1.0（标准速度）
  - 想要更快：0.5（2倍速）或0.25（4倍速）
  - 想要更慢：2.0（0.5倍速）
- `Start Hour`: 6（早晨开始）
- `Days Per Season`: 28（每季一个月）

---

## 第二步：添加时间控制器

### 操作1：创建 TimeController 对象

```
1. 在 Hierarchy 中右键空白处
2. Create Empty
3. 命名为：TimeManager
```

### 操作2：添加 TimeController 组件

```
1. 选中 TimeManager 对象
2. Inspector → Add Component
3. 搜索：TimeController
4. 添加组件
```

### 操作3：配置 TimeController

```
┌─────────────────────────────────────────────────────────┐
│ Inspector: TimeManager                                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  🕐 时间系统引用                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Time System  [ DefaultTimeSystem (GameTime)  ○ ] │ ← 拖入资源
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  ⚙️ 运行时配置                                           │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Auto Initialize  [ ✓ ]                        │ ← 自动初始化
│  │ Auto Start         [ ✓ ]                        │ ← 自动运行
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  🔧 调试选项                                             │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Show Debug Info  [ ]                            │ ← 勾选显示调试面板
│  │ Allow Time Control[ ✓ ]                         │ ← 允许键盘控制
│  │                                                 │   │
│  │ 快捷键配置                                      │   │
│  │ Pause Key        [ P                          ] │ ← 暂停
│  │ Fast Forward Key [ RightArrow                 ] │ ← 前进1小时
│  │ Rewind Key       [ LeftArrow                  ] │ ← 后退1小时
│  │ Skip To Morning  [ M                          ] │ ← 跳到早晨
│  │ Skip To Night    [ N                          ] │ ← 跳到晚上
│  │ Speed Up         [ UpArrow                    ] │ ← 加速
│  │ Speed Down       [ DownArrow                  ] │ ← 减速
│  └─────────────────────────────────────────────────┘   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**重要**：将 `DefaultTimeSystem` 资源拖到 `Time System` 字段

---

## 第三步：配置昼夜循环

### 操作1：找到或创建 Directional Light

```
1. 在 Hierarchy 中寻找 Directional Light
   - 如果场景中没有：
   a. 右键 Hierarchy → Light → Directional Light
   b. 命名为：SunLight
```

### 操作2：添加 DayNightCycle 组件

```
方法A：添加到 Directional Light
1. 选中 Directional Light
2. Add Component → DayNightCycle

方法B：添加到 TimeManager
1. 选中 TimeManager
2. Add Component → DayNightCycle
```

### 操作3：配置 DayNightCycle

```
┌─────────────────────────────────────────────────────────┐
│ Inspector: DayNightCycle                                │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  🕐 时间系统引用                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Time System      [ DefaultTimeSystem      ○ ]  │ ← 拖入资源
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  💡 光照配置                                             │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Directional Light  [ SunLight (Light)      ○ ]  │ ← 拖入光源
│  │ Skybox            [ None                  ○ ]  │ ← 可选
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  ☀️ 太阳配置                                             │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Sunrise Rotation     [ 10, -90, 0      ]      │   │
│  │ Noon Rotation        [ 90, 0, 0       ]      │   │
│  │ Sunset Rotation      [ 170, 90, 0      ]      │   │
│  │ Night Rotation       [ 270, 0, 0      ]      │   │
│  │                                                 │   │
│  │ Sunrise Hour          [ 6                  ]   │ ← 日出时间
│  │ Noon Hour             [ 12                 ]   │ ← 正午时间
│  │ Sunset Hour           [ 18                 ]   │ ← 日落时间
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  🔆 光照强度配置                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Max Light Intensity    [ 1.2               ]   │ ← 最大亮度
│  │ Min Light Intensity    [ 0.1               ]   │ ← 最小亮度
│  │ Light Transition Speed [ 1                 ]   │ ← 过渡速度
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  🌈 环境光配置                                           │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Day Ambient Color     [ 0.6, 0.6, 0.7    ◼ ]  │ ← 白天环境光
│  │ Night Ambient Color    [ 0.1, 0.1, 0.2   ◼ ]  │ ← 夜晚环境光
│  │ Ambient Transition    [ 1                 ]   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  🌫️ 雾效配置                                             │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Day Fog Color         [ 0.8, 0.9, 1      ◼ ]  │ ← 白天雾色
│  │ Night Fog Color       [ 0.1, 0.1, 0.2   ◼ ]  │ ← 夜晚雾色
│  │ Use Fog               [ ✓ ]              │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**配置要点**：
- `Directional Light`: 拖入你的 Directional Light 对象
- `Sunrise/Noon/Sunset Hour`: 根据需要调整
- `Max/Min Light Intensity`: 控制昼夜亮度差异

---

## 第四步：创建时间UI

### 操作1：创建 TimeUI Canvas（如果没有Canvas）

```
1. Hierarchy → 右键 → UI → Canvas
2. 命名为：TimeCanvas
3. Canvas Scaler 配置：
   - Render Mode: Screen Space - Overlay
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080
```

### 操作2：创建时间显示面板

```
1. 选中 TimeCanvas
2. 右键 → UI → Empty
3. 命名为：TimePanel

4. 配置 Rect Transform：
   - Anchor: Top Right
   - Pos X: -10, Pos Y: -10
   - Width: 200, Height: 150
```

### 操作3：创建时间文本

```
1. 选中 TimePanel
2. 右键 → UI → Text - TextMeshPro
3. 命名为：TimeText

4. 配置 Rect Transform：
   - Anchor: Top Left
   - Pos X: 10, Pos Y: -10
   - Width: 180, Height: 30

5. 配置 TextMeshPro：
   - Text: 06:00
   - Font Size: 28
   - Alignment: 左对齐
   - Color: 白色
```

### 操作4：创建日期文本

```
1. 复制 TimeText (Ctrl+D)
2. 命名为：DateText

3. 配置：
   - Pos Y: -45（在时间下方）
   - Font Size: 18
   - Text: 2024年1月1日
```

### 操作5：创建季节文本

```
1. 复制 DateText
2. 命名为：SeasonText

3. 配置：
   - Pos Y: -70
   - Font Size: 16
   - Text: 春季
```

### 操作6：创建天气文本

```
1. 复制 SeasonText
2. 命名为：WeatherText

3. 配置：
   - Pos Y: -95
   - Font Size: 16
   - Text: 晴天
```

### 操作7：添加 TimeUI 组件

```
1. 选中 TimePanel（或 TimeCanvas）
2. Add Component → TimeUI

3. 配置：
   ┌─────────────────────────────────────────────────┐
   │ Time UI (Script)                                 │
   │                                                 │
   │ 🕐 时间系统引用                                 │
   │ ┌──────────────────────────────────────────┐   │
   │ │ Time System  [ DefaultTimeSystem     ○ ]│   │
   │ └──────────────────────────────────────────┘   │
   │                                                 │
   │ 📝 时间显示                                     │
   │ ┌──────────────────────────────────────────┐   │
   │ │ Time Text          [ TimeText (TMP)  ○ ]│   │
   │ │ Date Text          [ DateText (TMP)  ○ ]│   │
   │ │ Season Text        [ SeasonText (TMP)○ ]│   │
   │ │ Time Of Day Text   [ None            ○ ]│ ← 可选
   │ │ Weather Text       [ WeatherText (TMP)○]│   │
   │ └──────────────────────────────────────────┘   │
   │                                                 │
   │ 🖼️ 图标显示                                     │
   │ ┌──────────────────────────────────────────┐   │
   │ │ Weather Icon       [ None            ○ ]│ ← 可选
   │ │ Time Of Day Icon   [ None            ○ ]│ ← 可选
   │ └──────────────────────────────────────────┘   │
   │                                                 │
   │ ⚙️ 显示选项                                     │
   │ ┌──────────────────────────────────────────┐   │
   │ │ Show Time       [ ✓ ]                   │   │
   │ │ Show Date       [ ✓ ]                   │   │
   │ │ Show Season     [ ✓ ]                   │   │
   │ │ Show TimeOfDay  [ ]                     │ ← 暂不显示
   │ │ Show Weather    [ ✓ ]                   │   │
   │ │ Show Icons      [ ]                     │ ← 暂不显示图标
   │ └──────────────────────────────────────────┘   │
   │                                                 │
   │ ⏱️ 更新间隔                                      │
   │ ┌──────────────────────────────────────────┐   │
   │ │ Update Interval  [ 0.5               ]   │   │
   │ └──────────────────────────────────────────┘   │
   │                                                 │
   │ 📅 时间格式                                     │
   │ ┌──────────────────────────────────────────┐   │
   │ │ Use 24 Hour Format[ ✓ ]                 │   │
   │ │ Show Seconds     [ ]                     │   │
   │ └──────────────────────────────────────────┘   │
   └─────────────────────────────────────────────┘
```

**连接引用**：
- `Time System`: 拖入 DefaultTimeSystem
- `Time Text`: 拖入 TimeText
- `Date Text`: 拖入 DateText
- `Season Text`: 拖入 SeasonText
- `Weather Text`: 拖入 WeatherText

---

## 第五步：测试

### 运行测试

```
1. 点击 Play 按钮
2. 观察：
   - 右上角显示时间、日期、季节、天气
   - 光照随时间变化
   - 环境光随时间变化

3. 测试快捷键：
   - P: 暂停/恢复时间
   - →: 快速前进1小时
   - ←: 后退1小时
   - M: 跳到早晨（6:00）
   - N: 跳到晚上（18:00）
   - ↑: 加速时间
   - ↓: 减速时间
```

### 测试时间速度

```
1. 选中 TimeManager
2. 勾选 Show Debug Info
3. 运行游戏
4. 屏幕左上角会显示调试信息
```

---

## 🎨 美化建议

### 调整UI样式

```
1. 给 TimePanel 添加背景：
   - TimePanel → Add Component → Image
   - Color: 黑色，Alpha 150
   - 添加阴影（可选）

2. 调整字体颜色：
   - TimeText: 金色
   - DateText: 白色
   - SeasonText: 绿色
   - WeatherText: 蓝色
```

### 添加图标（可选）

```
如果想要显示图标：

1. 创建天气图标对象：
   TimePanel → UI → Image
   - 命名为：WeatherIcon
   - 位置：在 WeatherText 右侧

2. 创建时间段图标：
   TimePanel → UI → Image
   - 命名为：TimeOfDayIcon
   - 位置：在 SeasonText 右侧

3. 准备图标资源（从Unity Asset Store或自己绘制）
4. 拖入 TimeUI 组件的图标字段
```

---

## 📚 代码示例

### 访问时间系统

```csharp
using UnityEngine;

public class ExampleScript : MonoBehaviour
{
    private GameTimeSystem timeSystem;

    void Start()
    {
        // 获取时间系统
        timeSystem = FindObjectOfType<GameTimeSystem>();

        // 或通过 TimeController
        var controller = FindObjectOfType<TimeController>();
        timeSystem = controller.GetTimeSystem();
    }

    void Update()
    {
        if (timeSystem == null) return;

        // 检查是否为白天
        if (timeSystem.IsDayTime)
        {
            Debug.Log("现在是白天");
        }

        // 获取当前时间
        Debug.Log($"时间: {timeSystem.TimeString}");
        Debug.Log($"日期: {timeSystem.DateString}");
        Debug.Log($"季节: {TimeHelpers.GetSeasonName(timeSystem.Season)}");
    }
}
```

### 监听时间事件

```csharp
using UnityEngine;

public class TimeEventSubscriber : MonoBehaviour
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
        // 取消订阅
        if (timeSystem != null)
        {
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

### 睡觉功能示例

```csharp
using UnityEngine;
using UnityEngine.UI;

public class BedInteractable : MonoBehaviour
{
    public Button sleepButton;

    void Start()
    {
        sleepButton.onClick.AddListener(OnSleep);
    }

    void OnSleep()
    {
        var controller = FindObjectOfType<TimeController>();
        if (controller != null)
        {
            controller.SleepToNextDay();
            Debug.Log("睡到第二天早晨");
        }
    }
}
```

---

## 🔧 故障排除

### 问题1：时间不流动

```
可能原因：
1. TimeController 没有运行
2. TimeSystem 为 null
3. 时间被暂停

解决方法：
1. 检查 TimeController 的 Auto Start 是否勾选
2. 确认 TimeSystem 字段已拖入资源
3. 按 P 切换暂停状态
```

### 问题2：光照不变化

```
可能原因：
1. DayNightCycle 组件未添加
2. Directional Light 未连接
3. TimeSystem 未连接

解决方法：
1. 确认 DayNightCycle 组件已添加
2. 拖入 Directional Light 到字段
3. 拖入 TimeSystem 资源
```

### 问题3：UI不更新

```
可能原因：
1. TimeUI 组件未添加
2. Text 组件未连接
3. TimeSystem 未连接

解决方法：
1. 确认 TimeUI 组件已添加
2. 连接所有 Text 字段
3. 拖入 TimeSystem 资源
```

---

## ✅ 完成检查清单

- [ ] DefaultTimeSystem 资源已创建
- [ ] TimeManager 对象已创建
- [ ] TimeController 组件已添加
- [ ] DefaultTimeSystem 已连接到 TimeController
- [ ] DayNightCycle 组件已添加
- [ ] Directional Light 已连接到 DayNightCycle
- [ ] TimeCanvas 已创建
- [ ] TimeUI 组件已添加
- [ ] 所有 Text 组件已连接到 TimeUI
- [ ] 运行测试正常

---

## 🎉 完成！

恭喜你完成了时间系统的设置！现在你的游戏具有：

✅ 动态时间系统
✅ 日期和季节
✅ 天气系统
✅ 昼夜循环光照
✅ 时间UI显示

祝你游戏开发顺利！
