# 背包预制体说明

## 如何创建背包预制体

由于代码已修改为单例模式，你需要在 Unity 编辑器中创建背包预制体：

### 步骤：

1. **创建空对象**
   - 在 Hierarchy 中右键 > Create Empty
   - 命名为 `PlayerInventoryHolder`

2. **添加组件**
   - 选中新创建的对象
   - 在 Inspector 中点击 Add Component
   - 搜索并添加 `PlayerInventoryHolder`

3. **配置组件**
   - `Item Database`: 拖入你的 `ItemDatabaseSO` 资源
   - `Capacity`: 设置背包容量（默认 24）

4. **保存为预制体**
   - 将对象拖入 `Assets/Prefabs/Player/` 文件夹
   - 命名为 `PlayerInventoryHolder.prefab`

5. **场景配置**
   - 在 `main.unity` 场景中添加此预制体
   - 在其他场景（`game.unity`、`couldcity.scene`）中删除现有的 `PlayerInventoryHolder` 对象

## 验证

运行游戏后，你应该在控制台看到：
```
[PlayerInventoryHolder] 背包系统已初始化（DontDestroyOnLoad）
```

切换场景时，如果检测到重复实例，会看到：
```
[PlayerInventoryHolder] 检测到已存在的背包实例，销毁重复对象
```
