# v1.3 试点系统接入记录

更新时间：2026-04-29

## 接入概览

| 试点 | 接入方式 | 改动文件 | 当前状态 |
|---|---|---|---|
| 时间系统 | `ITimeService` + 时间事件 + `TimeServiceAdapter` | `Assets/Scripts/Orchestration/Contracts/OrchestrationContracts.cs`、`Assets/Scripts/Orchestration/Events/CoreGameplayEvents.cs`、`Assets/Scripts/Orchestration/Adapters/LegacyOrchestrationAdapters.cs` | 已完成代码接入 |
| 钱包系统 | `IWalletService` + `WalletChangedEvent` + `WalletServiceAdapter` | `Assets/Scripts/Orchestration/Contracts/OrchestrationContracts.cs`、`Assets/Scripts/Orchestration/Events/CoreGameplayEvents.cs`、`Assets/Scripts/Orchestration/Adapters/LegacyOrchestrationAdapters.cs` | 已完成代码接入 |
| 商店购买 | `BuyShopItemCommand` + `ShopCommandAdapter` | `Assets/Scripts/Orchestration/Commands/ShopCommands.cs`、`Assets/Scripts/Shop/MiniShop.cs`、`Assets/Scripts/UI/SimpleShopUI.cs` | 已完成代码接入，旧购买链路保留 fallback |

## 中控核心

新增中控层：

- `GameRuntimeContext`
- `GameServiceRegistry`
- `GameEventBus`
- `GameCommandBus`
- `GameStateService`
- `CommandResult`

启动策略：

- `GameRuntimeContext` 使用 `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` 自动创建。
- 创建后通过 `AppRoot.AttachPersistent` 挂到 `__AppRoot` 下。
- 不修改 `AppRoot` 所在的 `FarmGame.Core` 反向引用 gameplay 层，避免程序集循环。

## 时间系统试点

### 接入路径

```text
RuntimeRefs.TimeSystem
  -> TimeServiceAdapter
  -> GameServiceRegistry.Register<ITimeService>
  -> GameEventBus.Publish(GameHourChangedEvent / GameDayChangedEvent / SeasonChangedEvent / WeatherChangedEvent)
```

### 保留兼容

- `TimeController` 不改。
- `TimeSystemAccessor` 不改。
- `SaveManager` 继续监听原有 `GameTimeSystem.onHourChanged`。

### 验证点

- 时间系统存在时，中控层注册 `ITimeService`。
- 小时、日期、季节、天气变化时发布强类型事件。
- 适配器销毁时移除 UnityEvent 监听，避免残留回调。

## 钱包系统试点

### 接入路径

```text
RuntimeRefs.PlayerWallet
  -> WalletServiceAdapter
  -> GameServiceRegistry.Register<IWalletService>
  -> GameEventBus.Publish(WalletChangedEvent)
```

### UI 监听路径

`MiniShop` 与 `SimpleShopUI` 增加了 `WalletChangedEvent` 监听：

```text
WalletServiceAdapter -> WalletChangedEvent -> MiniShop.UpdateWalletText() / SimpleShopUI.UpdateWalletText()
```

旧路径 `PlayerWallet.onCoinsChanged` 保留，所以中控事件不可用时商店 UI 仍能刷新。

## 商店购买命令试点

### 接入路径

```text
MiniShop / SimpleShopUI
  -> GameCommandBus.Execute(BuyShopItemCommand)
  -> ShopCommandAdapter
  -> IWalletService.TrySpend
  -> RuntimeRefs.InventoryBridge.TryAdd
  -> CommandResult
```

### fallback 策略

- 如果没有注册 `BuyShopItemCommand` 处理器，`MiniShop` 和 `SimpleShopUI` 会继续走旧购买逻辑。
- 如果命令处理器已注册但购买失败，会返回可读失败信息，例如金币不足、背包桥接缺失、添加物品失败。
- 添加物品失败时会退回金币。

## 编译记录

命令：

```text
dotnet build .\FarmGame.Gameplay.csproj --no-restore -v:minimal
```

结果：

- 0 个 C# 错误。
- 4 个既有程序集版本 warning，集中在 `System.Net.Http` 和 `System.Security.Cryptography.Algorithms`，与本次中控代码无关。

说明：

- 团结桌面编辑器当前占用项目，batchmode 无法打开同一工程执行 Unity 编译。
- 为了让外部 `dotnet build` 看到新脚本，临时补全了生成的 `FarmGame.Gameplay.csproj` 中 `Orchestration` 新文件条目。

## 剩余风险

- 尚未执行 Play Mode 回归，无法证明场景内运行时实例顺序完全符合预期。
- 新增 `.cs` 文件已配套 `.meta`，仍需要在编辑器刷新后确认 Unity 导入状态。
- `RuntimeRefs.InventoryBridge` 仍作为商店购买命令的库存落地路径，本版本未迁移为 `IInventoryService`。
- `SimpleShopUI` 与 `MiniShop` 均已接入命令，但旧购买实现仍存在，后续可在稳定后统一收敛。
