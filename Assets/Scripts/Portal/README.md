# 传送门系统使用说明

## 📋 功能概述

传送门系统允许玩家在场景之间快速传送，适用于：
- 从主菜单场景传送到游戏场景
- 从游戏场景返回主菜单
- 场景之间的任意传送

## 🚀 快速开始

### 1. 打开传送门编辑器

在Unity编辑器中：
- 菜单栏：`Tools > 传送门 > 场景传送快速设置`
- 快捷键：`Ctrl + Shift + P`

### 2. 创建传送门

#### 方法一：使用快速设置
1. 打开 `Tools > 传送门 > 场景传送快速设置`
2. 点击 "在 main 场景创建传送门到 game 场景"
3. 点击 "在 game 场景创建传送门到 main 场景"

#### 方法二：手动创建
1. 在场景中创建一个游戏对象
2. 添加 `Portal` 组件
3. 设置 `Target Scene Name` 为目标场景名称（如 `game` 或 `main`）
4. 可选：设置 `Spawn Point` 为传送后的生成位置

### 3. 配置传送门

| 参数 | 说明 |
|------|------|
| **Target Scene Name** | 目标场景名称（必须） |
| **Spawn Point** | 传送后的生成位置（可选） |
| **Portal Name** | 传送门显示名称 |
| **Interact Key** | 传送按键（留空则自动传送） |
| **Auto Teleport Delay** | 自动传送的延迟时间（秒） |
| **Fade Duration** | 淡入淡出时间 |
| **Portal Color** | 传送门颜色 |

### 4. 确保场景在 Build Settings

1. 打开 `File > Build Settings`
2. 将 `main.unity` 和 `game.unity` 添加到 Scenes in Build
3. 确保顺序正确

## ⚙️ 配置要求

### 玩家对象
- 必须有 `Player` 标签
- 建议有 `TPSInput` 组件（用于禁用输入）
- 传送时会自动禁用/启用玩家输入

### 场景
- 目标场景必须添加到 Build Settings
- 目标场景中必须有一个有 `Player` 标签的游戏对象

## 🎮 使用示例

### 示例1：从主菜单传送到游戏场景

1. 在 `main` 场景中创建传送门
2. 设置 `Target Scene Name` = `game`
3. 设置 `Spawn Point` = 游戏场景中玩家出生点

### 示例2：从游戏场景返回主菜单

1. 在 `game` 场景中创建传送门
2. 设置 `Target Scene Name` = `main`
3. 设置 `Spawn Point` = 主菜单场景中某个位置

### 示例3：设置传送门配对

1. 在 `main` 场景中创建传送门A，目标是 `game`
2. 在 `game` 场景中创建传送门B，目标是 `main`
3. 这样玩家可以在两个场景之间来回传送

## 🔧 高级功能

### 手动触发传送

```csharp
// 获取传送门组件
Portal portal = GetComponent<Portal>();

// 手动开始传送
portal.StartTeleport();
```

### 动态设置目标场景

```csharp
Portal portal = GetComponent<Portal>();
portal.SetTargetScene("new_scene_name");
```

### 动态设置生成点

```csharp
Portal portal = GetComponent<Portal>();
portal.SetSpawnPoint(spawnPointTransform);
```

## 🐛 常见问题

### 问题1：传送后玩家消失
- 检查目标场景是否有 `Player` 标签的游戏对象
- 检查传送门的 `Spawn Point` 是否设置正确

### 问题2：传送后位置不对
- 检查传送门的 `Spawn Point` 是否设置
- 如果没有设置，玩家会出现在传送门位置

### 问题3：按E键没有反应
- 检查 `Interact Key` 是否设置
- 如果设置为自动传送，检查是否在传送门内停留足够时间

### 问题4：传送后控制失效
- 传送门会自动禁用/启用 `TPSInput` 组件
- 如果使用其他输入系统，需要修改 `Portal.cs` 中的相关代码

## 📁 文件结构

```
Assets/Scripts/Portal/
├── Portal.cs                  # 传送门核心脚本
├── PortalVisualGenerator.cs    # 传送门视觉效果生成器
├── PortalEditor.cs            # 传送门编辑器（只运行在编辑器中）
├── PortalQuickSetup.cs        # 快速设置工具
└── README.md                  # 使用说明
```

## 🎨 自定义视觉效果

传送门现在使用 `PortalVisualGenerator` 组件自动生成视觉效果，包括：

### 默认视觉效果
- **外环**：旋转的圆环
- **核心**：呼吸效果的球体
- **粒子**：环绕传送门的粒子系统

### 自定义视觉效果

#### 方法一：修改PortalVisualGenerator参数
选中传送门对象，修改 `PortalVisualGenerator` 组件的参数：

| 参数 | 说明 |
|------|------|
| **Portal Color** | 传送门颜色 |
| **Portal Size** | 传送门大小 |
| **Animation Speed** | 动画速度 |
| **Rotate Effect** | 是否旋转 |
| **Show Particles** | 是否显示粒子 |

#### 方法二：使用自己的3D模型
1. 删除或禁用 `PortalVisualGenerator` 组件
2. 将自己的3D模型作为子对象添加到传送门
3. 确保碰撞体（SphereCollider等）仍然存在

#### 方法三：创建脚本式动画
在 `PortalVisualGenerator.cs` 中修改 `Update()` 方法，自定义动画效果。

## 💡 提示

- 传送门会自动禁用碰撞体的物理效果
- 可以设置 `Show Debug Info` 查看详细的调试日志
- 建议为传送门添加独特的模型或材质，方便识别
- 可以使用多个传送门连接多个场景

## 🔍 调试

启用调试模式：
1. 选中传送门对象
2. 勾选 `Show Debug Info`
3. 运行游戏查看控制台日志

日志会显示：
- 传送门初始化信息
- 玩家进入/离开传送门
- 传送开始/完成
- 错误和警告信息
