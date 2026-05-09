# v1.3 中控基线审计

审计日期：2026-04-29

## 结论

当前工程已经存在中控雏形，但职责分散在多个中心脚本中：

- `AppRoot` 负责跨场景根对象和常驻服务生命周期。
- `TaskManager` 负责轻量主线程任务调度。
- `GameManager` 负责启动、等待关键系统、加载存档或初始化新游戏。
- `RuntimeRefs` 负责运行时对象引用注册和变更事件。
- `SaveManager` 负责统一存档、读档和存档参与者调度。

v1.3 不应重写这些系统。正确施工路径是新增 `Orchestration` 中控层，并把旧系统通过适配器接入。`RuntimeRefs` 在本版本保留为兼容层，但新增功能不得继续扩展它的具体对象清单。

## 静态扫描摘要

| 扫描项 | 数量 | 结论 |
|---|---:|---|
| `RuntimeRefs.` 调用 | 212 | 当前跨系统引用大量依赖 RuntimeRefs，不能一次性全量移除 |
| `FindObjectOfType` 调用 | 18 | 仍有运行时查找对象路径，可逐步替换为服务注册或适配器 |
| `GameObject.Find` 调用 | 32 | 场景对象硬查找仍存在，后续应优先治理 UI/工具外的调用 |
| `.Instance` 调用 | 62 | 单例使用较多，v1.3 不强制清理旧单例，只限制新增模式 |

## 当前中心脚本职责

| 脚本 | 当前职责 | v1.3 处理策略 |
|---|---|---|
| `Assets/Scripts/Core/AppRoot.cs` | 创建 `__AppRoot`，跨场景保留常驻对象，提供 `RuntimeService.TryClaimSingleton` | 保留。允许中控上下文通过启动入口初始化，但不把 AppRoot 变成万能控制器 |
| `Assets/Scripts/Core/TaskManager.cs` | 提供 `ScheduleOnce`、`ScheduleRepeating`、取消任务能力 | 保留。作为任务调度基础服务接入中控，不迁移其内部实现 |
| `Assets/Scripts/Systems/GameManager.cs` | 等待 `SaveManager` 和时间系统，决定加载存档或初始化时间 | 保留。v1.3 不改变启动流程，只补充中控初始化 |
| `Assets/Scripts/Systems/RuntimeRefs.cs` | 注册玩家、背包、钱包、商店 UI、对话 UI、相机、时间、存档、作物、出生点等引用 | 作为兼容层保留。新增功能禁止继续向这里添加具体对象字段 |
| `Assets/Scripts/Save/SaveManager.cs` | 统一存档服务，按 `SaveSection` 调度 `ISaveParticipant` | 保留。中控层通过接口引用它，不改变存档格式 |

## 系统分类

### 核心服务

- 时间：`GameTimeSystem`、`TimeController`、`TimeSystemAccessor`
- 存档：`SaveManager`、`ISaveService`、`ISaveParticipant`
- 调度：`TaskManager`
- 启动：`AppRoot`、`GameManager`

### 玩法服务

- 背包与物品：`Inventory`、`PlayerInventoryHolder`、`InventoryPersistence`
- 钱包与商店：`PlayerWallet`、`MiniShop`、`SimpleShopUI`、`InventoryBridge`
- 农耕：`CropPlant`、`PlayerPlanter`、`CropSaveManager`
- 建造：`PlayerBuilder`、`BuildSaveManager`、`PlacedObject`
- NPC/Actor：`Actor`、`DialogueModule`、`ShopModule`、`GiftModule`、`QuestModule`、`SkillModule`
- 传送：`Portal`、`PortalManager`、`SpawnPoint`

### 表现层

- 背包 UI：`InventoryUI`、`InventorySlotUI`
- 商店 UI：`MiniShop`、`SimpleShopUI`
- 对话 UI：`NPCDialogUI`、`NPCDialogWorldBridge`
- HUD：`TimeUI`、`CharacterStatsUI`、`PickupHUD`
- 相机：`TPSOrbitCamera`、`FixedCameraSystem`、`CameraModeManager`

### 兼容层

- `RuntimeRefs`
- 旧单例 `Instance`
- 场景内 Inspector 引用
- 少量 `FindObjectOfType` / `GameObject.Find`
- 商店和对话中的反射桥接

## 本版本保留职责

- `AppRoot` 继续负责跨场景根对象。
- `TaskManager` 继续负责延迟和重复任务。
- `GameManager` 继续负责游戏启动和初始化判定。
- `SaveManager` 继续负责存档格式、存档参与者和读写流程。
- `RuntimeRefs` 继续为旧系统提供引用兼容。

## 本版本迁移职责

- 新增功能的跨系统通信迁移到 `GameEventBus` 和 `GameCommandBus`。
- 新增功能的服务发现迁移到 `GameServiceRegistry`。
- 新增功能的运行时共享状态迁移到 `GameStateService`。
- 时间、背包或钱包、商店或 NPC 对话作为试点通过适配器接入。

## 本版本不动范围

- 不全量移除 `RuntimeRefs`。
- 不重写 `SaveManager` 存档格式。
- 不重写玩家控制、相机、背包、商店、NPC、传送主链路。
- 不引入第三方依赖注入框架。
- 不新增大规模 UI 重做。

## v1.3 试点系统

| 试点 | 接入方式 | 验证目标 |
|---|---|---|
| 时间系统 | `ITimeService` + 时间事件适配器 | 时间变化可通过事件发布，旧 `TimeController` / `TimeUI` 不回退 |
| 背包或钱包系统 | `IInventoryService` 或 `IWalletService` + 数据变化事件 | UI 可通过事件刷新，不必须直接持有玩法对象 |
| 商店或 NPC 对话 | 强类型命令 + 命令处理器 | UI 或交互入口通过命令触发玩法行为，并得到 `CommandResult` |

## AI 施工边界

AI 后续施工必须遵守：

- 优先新增 `Assets/Scripts/Orchestration` 下的中控代码和适配器。
- 新功能不得新增 `RuntimeRefs.SomeNewSystem` 这类具体对象字段。
- 旧代码可以继续读取 `RuntimeRefs`，但新试点应先包装成接口或事件。
- 事件只描述已经发生的事实，例如 `WalletChangedEvent`。
- 命令只描述请求执行的意图，例如 `BuyItemCommand`。
- 服务接口只暴露稳定能力，不暴露内部 MonoBehaviour。
- 每个试点都必须记录接入路径、改动文件、验证结果和剩余风险。
