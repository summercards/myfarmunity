# ✅ 时间系统配置指南（编译错误已修复）

## 🎉 MCP工具测试成功！

---

## 🚀 配置方式选择

### 方式A：使用TestTimeSetup脚本（推荐，最简单）

#### 步骤1：在场景中添加测试脚本

```
1. 找到 Hierarchy 中的任意对象（如 Systems）
2. 右键 → Add Component
3. 搜索：TestTimeSetup
4. 添加组件
```

#### 步骤2：运行游戏

```
1. 点击 Play 按钮
2. 查看 Console 输出
3. 时间系统会自动设置完成
```

#### 步骤3：测试功能

```
运行后：
- 时间会自动流动
- 光照会随时间变化
- 使用快捷键控制时间：
  * P: 暂停/恢复
  * →: 前进1小时
  * ↑/↓: 调整速度
```

---

### 方式B：使用编辑器向导

#### 步骤1：打开向导

```
Unity 顶部菜单 → Tools → Time System → Setup Wizard
```

#### 步骤2：一键配置

```
点击 "🚀 一键完整配置" 按钮
等待配置完成
点击确定
```

#### 步骤3：测试运行

```
点击 Play 测试系统
```

---

### 方式C：使用TimeSystemQuickSetup脚本

#### 步骤1：创建设置对象

```
Hierarchy → 右键 → Create Empty
命名为：TimeSetupHelper
```

#### 步骤2：添加组件

```
Add Component → Time System Quick Setup
```

#### 步骤3：运行配置

```
在 Inspector 中点击 "快速配置" 按钮
或右键对象 → 快速配置时间系统
```

---

## 🎮 配置完成后你得到

### 场景对象

```
Hierarchy 中会创建：

📁 Scene
  ├─ 🕐 TimeManager（时间控制器）
  │   └─ TimeController (Script)
  │
  ├─ 🌞 Directional Light（已添加 DayNightCycle）
  │
  └─ 📺 TimeCanvas（可选）
      ├─ TimePanel
      └─ TimeUI (Script)
```

### 资源文件

```
Project 中会创建（如果使用方式B）：

Assets/Resources/Game/
  └─ DefaultTimeSystem.asset
```

---

## ⌨️ 快捷键控制

| 按键 | 功能 |
|------|------|
| **P** | 暂停/恢复时间 |
| **→** | 前进1小时 |
| **←** | 后退1小时 |
| **M** | 跳到早晨 6:00 |
| **N** | 跳到晚上 18:00 |
| **↑** | 加速时间 |
| **↓** | 减速时间 |

---

## 🔧 自定义配置

### 修改时间速度

```csharp
// 获取时间系统
var timeSystem = FindObjectOfType<GameTimeSystem>();

// 设置速度（1现实秒 = X游戏分钟）
timeSystem.SetTimeScale(0.5f); // 2倍速
timeSystem.SetTimeScale(1.0f); // 正常速度
timeSystem.SetTimeScale(2.0f); // 0.5倍速
```

### 修改时间

```csharp
var timeSystem = FindObjectOfType<GameTimeSystem>();

// 设置具体时间
timeSystem.SetTime(12, 30); // 12:30

// 快速前进
timeSystem.FastForward(2); // 前进2小时

// 睡到第二天
timeSystem.SleepToNextDay();
```

---

## 📊 代码示例

### 检查时间段

```csharp
void Update()
{
    var timeSystem = FindObjectOfType<GameTimeSystem>();

    if (timeSystem.IsDayTime)
    {
        Debug.Log("现在是白天");
    }

    if (timeSystem.IsNightTime)
    {
        Debug.Log("现在是夜晚");
    }
}
```

### 监听时间事件

```csharp
void Start()
{
    var timeSystem = FindObjectOfType<GameTimeSystem>();

    timeSystem.onHourChanged.AddListener(() => {
        Debug.Log("新的一小时");
    });

    timeSystem.onDayChanged.AddListener(() => {
        Debug.Log("新的一天");
    });
}
```

---

## 🐛 故障排除

### 问题1：运行后什么都没发生

```
解决：
1. 检查 Console 是否有错误
2. 确认 TestTimeSetup 脚本已添加到对象
3. 尝试重启 Unity
```

### 问题2：光照不变化

```
解决：
1. 确认场景中有 Directional Light
2. 运行游戏后等待几秒
3. 检查 DayNightCycle 组件是否已添加
```

### 问题3：快捷键不工作

```
解决：
1. 确保 TimeController 的 Allow Time Control 已勾选
2. 确保游戏处于运行状态
3. 尝试切换一下输入焦点
```

---

## 📁 文件位置

```
Assets/Scripts/Time/
  ├─ TimeOfDay.cs
  ├─ GameTimeSystem.cs
  ├─ TimeController.cs
  ├─ DayNightCycle.cs
  ├─ TimeUI.cs
  ├─ TimeSystemSetupWizard.cs ✅（已修复）
  ├─ TimeSystemQuickSetup.cs
  ├─ TestTimeSetup.cs ✅（新增）
  └─ CONFIG_GUIDE.md
```

---

## 🎉 开始使用

### 最简单的方式（3步完成）

```
1. Hierarchy → 任意对象 → Add Component → TestTimeSetup
2. 点击 Play
3. 完成！
```

### 完整配置（编辑器方式）

```
1. Tools → Time System → Setup Wizard
2. 点击 "一键完整配置"
3. 点击 Play
4. 完成！
```

---

## 💡 提示

- 所有配置脚本都不会破坏现有场景
- 可以重复配置，会跳过已存在的组件
- 使用 TestTimeSetup 最适合快速测试
- 使用 Setup Wizard 最适合完整配置

---

## 🎯 推荐使用方式

**初学者/快速测试**：使用 TestTimeSetup 脚本
**正式开发**：使用 Setup Wizard 向导
**运行时配置**：使用 TimeSystemQuickSetup

祝游戏开发顺利！🚀
