# 时间系统快速配置指南

## 🚀 一键自动配置（推荐）

按照以下步骤快速配置：

### 步骤 1：创建测试场景
在 Unity 编辑器中：
1. 点击菜单栏：**Tools → 时间系统 → 创建测试场景**
2. 等待场景创建完成

### 步骤 2：运行自动配置
1. 点击菜单栏：**Tools → 时间系统 → 自动配置**
2. 等待自动配置完成
3. 查看控制台日志确认配置成功

### 步骤 3：测试运行
1. 点击 Unity 播放按钮 ▶️
2. 观察控制台日志
3. 检查场景中的 GameObject 是否正确创建

---

## ✅ 自动配置创建的组件

| GameObject | 组件 | 说明 |
|-----------|------|------|
| GameManager | GameManager | 游戏启动管理器，自动加载存档 |
| SaveManager | SaveManager | 存档管理器 |
| TimeManager | TimeController | 时间控制器 |
| Directional Light | Light + DayNightCycle | 方向光 + 昼夜循环 |
| TimeUI | TimeUI | 时间UI显示（可选） |
| ExampleFarmSystem | ExampleFarmSystem | 示例农场系统 |
| Resources/DefaultTimeSystem | GameTimeSystem | 时间系统资源 |

---

## 🎮 测试功能

配置完成后，可以使用以下菜单快速测试：

### 存档测试
```
Tools → 时间系统 → 测试 - 快速保存
Tools → 时间系统 → 测试 - 快速加载
Tools → 时间系统 → 测试 - 新游戏
```

### 存档管理
```
Tools → 时间系统 → 测试 - 查看存档列表
Tools → 时间系统 → 测试 - 清空所有存档
Tools → 时间系统 → 测试 - 打开存档文件夹
```

---

## 🎮 游戏内控制

配置完成后，可以使用以下快捷键：

| 快捷键 | 功能 |
|--------|------|
| **P** | 暂停/恢复时间 |
| **←** | 后退1小时 |
| **→** | 前进1小时 |
| **M** | 跳到早晨 (6:00) |
| **N** | 跳到晚上 (18:00) |
| **↑** | 加快时间（减少秒/分钟） |
| **↓** | 减慢时间（增加秒/分钟） |

---

## 📋 手动配置（可选）

如果你想手动配置，请参考 `README_TimeSystemSetup.md` 文档。

---

## 🔍 验证配置

运行游戏后，检查以下内容：

### 控制台日志
```
[GameManager] 游戏管理器已初始化
[GameManager] 开始初始化游戏...
[GameManager] 正在加载存档: autosave
[SaveManager] 游戏已保存: autosave
```

### 场景检查
- ✓ GameManager 存在且是单例
- ✓ SaveManager 存在且是单例
- ✓ TimeManager 存在且引用了 GameTimeSystem
- ✓ Directional Light 存在且有 DayNightCycle 组件
- ✓ TimeUI 存在且引用了 GameTimeSystem

### 存档文件检查
```
位置: Application.persistentDataPath/saves/
文件: autosave.json
```

点击 `Tools → 时间系统 → 测试 - 打开存档文件夹` 查看存档文件。

---

## 🐛 常见问题

### Q1: 自动配置后报错？
**解决**：检查控制台错误信息，通常是因为脚本编译错误。修复所有编译错误后重新运行。

### Q2: 时间仍然重置？
**原因**：存档未正确加载。

**检查清单**：
- ✓ GameManager 是否创建了？
- ✓ SaveManager 是否创建了？
- ✓ autoLoadLatestSave 是否勾选？
- ✓ 之前是否保存过存档？

**解决**：手动触发一次保存（Tools → 快速保存），然后重新运行游戏。

### Q3: 组件创建失败？
**原因**：脚本编译错误或缺少引用。

**解决**：
1. 检查 Unity Console 是否有编译错误
2. 确保所有脚本都在正确的文件夹中
3. 重新运行自动配置

### Q4: 场景中看不到 Directional Light？
**原因**：自动配置脚本会在现有 Directional Light 上添加 DayNightCycle，如果没有则会创建新的。

**解决**：检查场景中是否有 Directional Light，或在 Inspector 中查看。

---

## 📚 文档

详细配置文档：`Assets/Scripts/Time/README_TimeSystemSetup.md`

---

## 🎉 配置完成！

配置完成后，你的游戏将具备：
- ✅ 自动保存/加载存档
- ✅ 时间系统（日期/季节/天气）
- ✅ 昼夜循环（光照/天空）
- ✅ 可扩展的子系统接口

开始开发你的农场游戏吧！🚜
