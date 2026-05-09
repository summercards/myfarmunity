# AI 新功能接入模板

适用版本：v1.3 及后续功能版本。

本模板用于让 AI 新增功能时按统一路径施工，避免继续扩大 `RuntimeRefs` 和散落单例。

## 1. 功能边界

新增功能前，AI 必须先写清：

- 功能名称：
- 核心玩法：
- 是否需要 UI：
- 是否需要存档：
- 是否需要时间驱动：
- 是否需要和背包、钱包、商店、NPC、传送交互：
- 不纳入范围：

## 2. 目录建议

```text
Assets/Scripts/<Feature>/
Assets/Scripts/<Feature>/Data/
Assets/Scripts/<Feature>/Runtime/
Assets/Scripts/<Feature>/Save/
Assets/Scripts/<Feature>/UI/
Assets/Scripts/Orchestration/Adapters/<Feature>Adapter.cs
Assets/Scripts/Orchestration/Events/<Feature>Events.cs
Assets/Scripts/Orchestration/Commands/<Feature>Commands.cs
```

## 3. 服务接口

每个功能先定义稳定服务接口，不让其他系统直接依赖具体 MonoBehaviour。

示例：

```csharp
public interface IFishingService
{
    bool CanStartFishing();
    CommandResult StartFishing(string spotId);
    CommandResult TryCatch();
}
```

注册路径：

```text
FeatureAdapter -> GameServiceRegistry.Register<IFeatureService>()
```

## 4. 事件

事件只表达已经发生的事实。

示例：

```csharp
public readonly struct FishCaughtEvent
{
    public string FishId { get; }
    public int Count { get; }
}
```

使用规则：

- UI 可以监听事件刷新表现。
- 其他系统可以监听事件做反应。
- 事件不应该直接要求别人执行复杂行为。

## 5. 命令

命令表达请求执行的意图。

示例：

```csharp
public readonly struct StartFishingCommand
{
    public string SpotId { get; }
}
```

处理路径：

```text
UI/交互入口 -> GameCommandBus.Execute(StartFishingCommand)
FeatureCommandHandler -> FeatureService -> CommandResult
```

命令失败必须返回可读信息，不允许静默失败。

## 6. 运行时状态

短期共享状态进入 `GameStateService`。

示例：

```text
GameStateService.Set("Fishing.CurrentSpot", spotId)
GameStateService.TryGet("Fishing.CurrentSpot", out string spotId)
```

规则：

- 不存 UI 引用。
- 不存场景对象长期引用。
- 不把状态服务当数据库。

## 7. 存档接入

需要长期保存的功能必须优先接入 `SaveManager`。

建议：

- 创建 `<Feature>SaveParticipant`。
- 实现 `ISaveParticipant`。
- 使用专属 DTO，避免直接序列化 MonoBehaviour。
- 加载时必须处理缺字段和旧存档。

## 8. UI 接入

UI 不直接改玩法数据。

推荐路径：

```text
UI 点击 -> CommandBus.Execute
功能完成 -> EventBus.Publish
UI 监听事件刷新
```

允许 fallback：

- 旧场景还没挂适配器时，可保留旧 Inspector 引用路径。
- fallback 必须有注释或文档说明，后续版本再移除。

## 9. 禁止事项

AI 新增功能时不得：

- 在 `RuntimeRefs` 新增具体对象字段。
- 新增万能 `Manager.Instance` 作为跨系统入口。
- 让 UI 直接修改钱包、背包、任务、NPC 等核心数据。
- 绕过 `SaveManager` 写长期存档。
- 在一个巨大脚本里同时做配置、逻辑、UI、存档和场景生成。

## 10. 验收清单

- [ ] 服务接口已定义。
- [ ] 事件已定义，且只表达事实。
- [ ] 命令已定义，且失败有可读信息。
- [ ] 适配器已注册服务、事件或命令。
- [ ] UI 通过命令或事件接入。
- [ ] 需要存档时已接入 `SaveManager`。
- [ ] 没有新增 `RuntimeRefs` 具体对象字段。
- [ ] 编译通过。
- [ ] 回归不影响玩家移动、时间、存档、背包、商店、NPC、传送。

## 11. 模拟：钓鱼系统接入

推荐新增：

- `IFishingService`
- `StartFishingCommand`
- `TryCatchFishCommand`
- `FishingStartedEvent`
- `FishCaughtEvent`
- `FishingState`
- `FishingSaveParticipant`
- `FishingOrchestrationAdapter`
- `FishingUI`

数据流：

```text
FishingUI -> StartFishingCommand
FishingCommandHandler -> IFishingService.StartFishing
FishingService -> GameStateService.Set("Fishing.ActiveSpot")
FishingService -> FishCaughtEvent
FishingUI -> 监听 FishCaughtEvent 刷新结果
FishingSaveParticipant -> SaveManager
```

不允许：

- `RuntimeRefs.FishingSystem`
- `FishingManager.Instance` 被其他系统到处调用
- UI 直接 `playerInventory.AddItem`
