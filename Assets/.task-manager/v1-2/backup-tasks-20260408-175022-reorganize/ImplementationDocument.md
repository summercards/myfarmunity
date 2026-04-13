# 我的农场 v1.2 Implementation Document

## Version Summary

交付卧室功能闭环（不含美术）：双向传送、DIY可编辑与持久化、床睡觉进入灵之空间、唯一入口治理、联调发布。

---

## Version Tasks

> 注：本版本任务以“复用现有系统”为前提，避免新增并行框架。

---

## Stages

### 设计与拆解

- **Status**: todo
- **Summary**: 先完成架构对齐，再进入开发，避免返工

#### 架构对齐结论（必须遵守）

1. **传送系统统一**
   - 复用现有 `Portal.cs` + `PortalManager.cs` + `SpawnPoint.cs`
   - 不新增并行 `TeleportPoint` 主链路实现

2. **存档策略统一**
   - 优先接入 `SaveManager` (`ISaveParticipant`)
   - 仅在无 `SaveManager` 场景允许 PlayerPrefs fallback

3. **DIY能力复用**
   - 复用现有 `BuildCatalogSO` / `PlayerBuilder` / `PlacedObject`
   - 在卧室场景补充编辑态控制与区域限制

4. **唯一入口治理**
   - 灵之空间入口仅允许“卧室床”触发
   - 必须有排查清单和回归验证项

#### 数据模型设计

**1. 卧室DIY数据模型**

创建 `Assets/Scripts/BedroomSystem/BedroomData.cs`：
```csharp
[System.Serializable]
public class BedroomData
{
    public List<FurniturePlacement> furniturePlacements = new();
}

[System.Serializable]
public class FurniturePlacement
{
    public string itemId;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}
```

**2. 床入口配置模型**

创建 `Assets/Scripts/BedroomSystem/BedEntranceConfig.cs`：
```csharp
[CreateAssetMenu(fileName = "BedEntranceConfig", menuName = "MyFarm/Bedroom/BedEntranceConfig")]
public class BedEntranceConfig : ScriptableObject
{
    public string spiritSceneName = "spiritspace";
    public string spiritSpawnId = "spiritspace_bed_entry";
    public float sleepDelaySeconds = 1.5f;
}
```

**3. 唯一入口治理配置**

创建 `Assets/Scripts/BedroomSystem/SpiritEntrancePolicy.cs`：
```csharp
[CreateAssetMenu(fileName = "SpiritEntrancePolicy", menuName = "MyFarm/Bedroom/SpiritEntrancePolicy")]
public class SpiritEntrancePolicy : ScriptableObject
{
    public string allowedEntranceId = "bedroom_bed_entrance";
    public bool blockOtherEntrances = true;
}
```

#### 开发顺序建议

```text
1. 架构对齐 -> 2. 场景与传送点 -> 3. 床入口 -> 4. DIY编辑 -> 5. DIY存档 -> 6. 唯一入口治理 -> 7. 联调与发布
```

---

### 卧室功能开发

- **Status**: todo
- **Summary**: 按“场景基础 -> 核心功能 -> 治理”顺序实现

#### 任务1：创建场景与基础点位

**文件路径**：
- `Assets/Scenes/Bedroom.unity`
- `Assets/Scenes/spiritspace.unity`

**实现步骤**：
1. 创建 `Bedroom.unity`（可白模）
2. 创建 `spiritspace.unity`（入口承接场景）
3. 在两个场景创建 `SpawnPoint`：
   - `bedroom_entry_from_cloudcity`
   - `cloudcity_entry_from_bedroom`
   - `spiritspace_bed_entry`
4. 将两个场景加入 Build Settings

**验收标准**：
- [ ] 场景可加载
- [ ] SpawnPoint ID 唯一且可被 `RuntimeRefs` 注册
- [ ] Build Settings 中可见并可运行

---

#### 任务2：接入云层↔卧室双向传送

**文件路径**：
- `Assets/Scripts/Portal/Portal.cs`（复用配置）
- `Assets/Scripts/Portal/PortalManager.cs`（复用）
- `Assets/Scenes/cloudcity.scene`
- `Assets/Scenes/Bedroom.unity`

**实现步骤**：
1. 在 cloudcity 放置指向卧室的 Portal
2. 在 Bedroom 放置返回 cloudcity 的 Portal
3. 配置 `targetScene` 和 `targetSpawnID`
4. 验证多次连续传送稳定性

**验收标准**：
- [ ] 双向传送可用
- [ ] 玩家落点正确
- [ ] 不出现重复触发或卡死

---

#### 任务3：实现床睡觉入口

**文件路径**：
- `Assets/Scripts/BedroomSystem/BedSleepEntrance.cs`
- `Assets/Scripts/BedroomSystem/BedEntranceConfig.cs`

**实现步骤**：
1. 床触发区内按 `E` 进入睡觉流程
2. 播放睡觉状态（动画或状态切换）
3. 延迟后调用 `PortalManager.Teleport("spiritspace", "spiritspace_bed_entry")`
4. 处理重复触发保护

**验收标准**：
- [ ] E键可触发
- [ ] 睡觉状态可见
- [ ] 正确进入灵之空间场景

---

#### 任务4：实现DIY编辑能力

**文件路径**：
- `Assets/Scripts/BedroomSystem/BedroomEditModeController.cs`
- `Assets/Scripts/BedroomSystem/BedroomFurnitureEditController.cs`
- `Assets/Scripts/Build/PlayerBuilder.cs`（复用或最小扩展）

**实现步骤**：
1. 进入卧室后可切换编辑模式
2. 编辑模式支持放置、移动、删除
3. 仅允许在卧室可编辑区域操作
4. 退出编辑模式后恢复正常移动/交互

**验收标准**：
- [ ] 放置/移动/删除可用
- [ ] 编辑区约束生效
- [ ] 不影响玩家基础控制

---

#### 任务5：实现DIY持久化（SaveManager优先）

**文件路径**：
- `Assets/Scripts/BedroomSystem/BedroomDIYSaveParticipant.cs`
- `Assets/Scripts/BedroomSystem/BedroomData.cs`

**实现步骤**：
1. 实现 `ISaveParticipant`，接入 `SaveManager`
2. 保存卧室内家具清单（位置/旋转/缩放）
3. 无 `SaveManager` 时回退 PlayerPrefs（key: `BedroomDIYData`）
4. 读档后恢复并避免重复生成

**验收标准**：
- [ ] SaveManager 路径可保存/恢复
- [ ] fallback 路径可保存/恢复
- [ ] 多次读档无重复家具

---

#### 任务6：唯一入口治理

**文件路径**：
- `Assets/Scripts/BedroomSystem/SpiritEntrancePolicy.cs`
- `Assets/Scripts/BedroomSystem/SpiritEntranceValidator.cs`

**实现步骤**：
1. 扫描当前工程中的灵之空间入口点
2. 保留卧室床入口，禁用其他入口
3. 增加校验（启动时或编辑器菜单）
4. 回归脚本中加入唯一入口断言

**验收标准**：
- [ ] 仅卧室床可进入灵之空间
- [ ] 误配置可被校验脚本发现

---

### 联调与发布

- **Status**: todo
- **Summary**: 确保功能可上线且不回归

#### 联调清单

- [ ] 传送系统与云层场景联调
- [ ] DIY系统与主循环联调
- [ ] 床入口与灵之空间场景联调
- [ ] 存档与加载流程联调
- [ ] 唯一入口治理联调

#### 回归测试

- [ ] 云层功能不受影响
- [ ] NPC对话系统不受影响
- [ ] 商店功能不受影响
- [ ] 玩家移动不受影响
- [ ] 现有保存加载不受影响

#### 发布准备

- [ ] 代码审查通过
- [ ] 文档整理完成
- [ ] 测试报告无严重bug
- [ ] CHANGELOG更新
- [ ] 版本打标签

---

## 附录

### 文件结构（v1.2 功能版）

```text
Assets/
├── Scenes/
│   ├── Bedroom.unity
│   └── spiritspace.unity
├── Scripts/
│   ├── Portal/
│   │   ├── Portal.cs
│   │   ├── PortalManager.cs
│   │   └── SpawnPoint.cs
│   └── BedroomSystem/
│       ├── BedroomData.cs
│       ├── BedroomDIYSaveParticipant.cs
│       ├── BedroomEditModeController.cs
│       ├── BedroomFurnitureEditController.cs
│       ├── BedSleepEntrance.cs
│       ├── BedEntranceConfig.cs
│       ├── SpiritEntrancePolicy.cs
│       └── SpiritEntranceValidator.cs
```

### 实施原则

- 不重复造传送轮子，优先复用已有稳定链路。
- 存档遵循主干架构，避免局部方案与全局冲突。
- 任务验收必须可执行、可复现、可回归。
