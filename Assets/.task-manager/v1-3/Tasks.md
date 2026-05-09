# 我的农场 v1.3 任务文件

> 本版本任务面向 AI 施工与验收。任务总数控制在 9 个，只设置“执行类任务”和“验收类任务”。

## 使用说明

1. 每个任务都从 `## 任务：...` 开始。
2. 支持字段：编号、对应阶段、状态(todo / in_progress / done)、优先级(low / medium / high)、进度(0-100)。
3. 每个任务必须对应 `TargetDocument.md` 和 `ImplementationDocument.md` 中的里程碑。
4. 执行类任务必须产出代码、文档或配置变更。
5. 验收类任务必须产出验证记录或验收报告。

## 默认归属
- 文档：施工文档 v1.3
- 阶段：中控核心骨架落地

## 当前任务

## 任务：执行：输出中控基线审计与RuntimeRefs迁移规则
- 编号：01
- 对应阶段：施工文档 v1.3 / 中控基线审计与边界冻结
- 状态：done
- 优先级：high
- 进度：100
- 文档：施工文档 v1.3
- 阶段：中控基线审计与边界冻结
- 标签：v1.3, 执行, 中控, 审计
- 目标：冻结中控施工边界，明确现有中心脚本职责和 RuntimeRefs 迁移规则。
- 任务键：myfarm-v1-3-exec-orchestration-baseline
- 描述：
  AI 阅读 `TargetDocument.md` 与 `ImplementationDocument.md`，扫描 `AppRoot`、`GameManager`、`RuntimeRefs`、`TaskManager`、`SaveManager` 及其主要使用方，输出 `DesignNotes/OrchestrationBaseline.md` 和 `DesignNotes/RuntimeRefsMigrationRules.md`。文档必须列明保留职责、迁移职责、本版本不动范围、至少 3 个试点系统，以及新增功能禁止继续扩展 RuntimeRefs 具体对象清单的规则。

  完成记录：已输出 `DesignNotes/OrchestrationBaseline.md` 与 `DesignNotes/RuntimeRefsMigrationRules.md`。静态扫描记录了 `RuntimeRefs.`、`FindObjectOfType`、`GameObject.Find`、`.Instance` 热点，并冻结 v1.3 兼容层策略。

## 任务：执行：建立中控核心目录与基础契约
- 编号：02
- 对应阶段：施工文档 v1.3 / 中控核心骨架落地
- 状态：done
- 优先级：high
- 进度：100
- 文档：施工文档 v1.3
- 阶段：中控核心骨架落地
- 标签：v1.3, 执行, Orchestration, Contracts
- 目标：创建中控层目录、命名空间、基础接口和结果类型。
- 任务键：myfarm-v1-3-exec-orchestration-contracts
- 描述：
  AI 新建 `Assets/Scripts/Orchestration` 及 `Contracts`、`Events`、`Commands`、`Adapters` 子目录，建立 `FarmGame.Orchestration` 命名空间。实现基础契约和结果类型，至少包括服务注册接口、事件总线接口、命令总线接口、运行时状态接口、`CommandResult`。不得让中控契约依赖具体 UI 类或场景对象。

  完成记录：已新增 `Assets/Scripts/Orchestration` 目录、基础契约、`CommandResult`、时间/钱包接口、事件和命令类型。

## 任务：执行：实现ServiceRegistry、EventBus、CommandBus与GameStateService
- 编号：03
- 对应阶段：施工文档 v1.3 / 中控核心骨架落地
- 状态：done
- 优先级：high
- 进度：100
- 文档：施工文档 v1.3
- 阶段：中控核心骨架落地
- 标签：v1.3, 执行, ServiceRegistry, EventBus, CommandBus
- 目标：实现中控最小可运行闭环。
- 任务键：myfarm-v1-3-exec-core-orchestration-services
- 描述：
  AI 实现 `GameServiceRegistry`、`GameEventBus`、`GameCommandBus`、`GameStateService` 和 `GameRuntimeContext`。要求服务可注册/获取/注销，事件可订阅/取消/发布，命令可注册/执行并返回成功或失败，运行时状态不持有 UI 引用。命令未注册时必须返回可读失败结果。事件订阅必须有取消订阅路径。

  完成记录：已实现中控核心服务，`GameCommandBus` 支持 `HasHandler`、未注册命令失败返回和异常保护。

## 任务：执行：接入AppRoot初始化并保持旧系统兼容
- 编号：04
- 对应阶段：施工文档 v1.3 / 中控核心骨架落地
- 状态：done
- 优先级：high
- 进度：100
- 文档：施工文档 v1.3
- 阶段：中控核心骨架落地
- 标签：v1.3, 执行, AppRoot, 兼容
- 目标：让中控上下文随游戏启动创建，同时不破坏现有主流程。
- 任务键：myfarm-v1-3-exec-approot-runtime-context
- 描述：
  AI 将 `GameRuntimeContext` 接入 `AppRoot` 或等效启动入口，确保中控层跨场景可用。保留 `GameManager`、`SaveManager`、`TaskManager`、`RuntimeRefs` 现有职责，不做大规模改写。完成后记录初始化顺序和兼容策略。

  完成记录：`GameRuntimeContext` 和 `LegacyOrchestrationAdapters` 通过 `RuntimeInitializeOnLoadMethod` 自动创建，并使用 `AppRoot.AttachPersistent` 挂到 `__AppRoot` 下，未修改旧中心脚本。

## 任务：执行：接入时间系统试点
- 编号：05
- 对应阶段：施工文档 v1.3 / 试点系统接入与解耦验证
- 状态：done
- 优先级：high
- 进度：100
- 文档：施工文档 v1.3
- 阶段：试点系统接入与解耦验证
- 标签：v1.3, 执行, Time, Adapter
- 目标：将时间系统作为只读服务和事件发布试点接入中控。
- 任务键：myfarm-v1-3-exec-time-system-adapter
- 描述：
  AI 定义 `ITimeService` 和时间事件，使用适配器包装现有 `GameTimeSystem` / `TimeSystemAccessor`。发布游戏小时、日期、季节等变化事件。不得破坏现有 TimeController、TimeUI 和 SaveManager 对时间系统的使用。

  完成记录：已实现 `ITimeService`、时间事件和 `TimeServiceAdapter`，旧时间主链路未改动。

## 任务：执行：接入背包或钱包试点并改造一个UI监听路径
- 编号：06
- 对应阶段：施工文档 v1.3 / 试点系统接入与解耦验证
- 状态：done
- 优先级：high
- 进度：100
- 文档：施工文档 v1.3
- 阶段：试点系统接入与解耦验证
- 标签：v1.3, 执行, Inventory, Wallet, UI
- 目标：证明数据变化可以通过事件驱动 UI 刷新。
- 任务键：myfarm-v1-3-exec-inventory-wallet-ui-adapter
- 描述：
  AI 选择背包或钱包作为试点，定义对应服务接口和变化事件，通过适配器接入现有系统，并改造一个最小 UI 监听路径，使 UI 可以通过中控事件刷新。保留旧路径作为兼容 fallback，避免场景配置一次性失效。

  完成记录：已选择钱包试点，实现 `IWalletService`、`WalletChangedEvent`、`WalletServiceAdapter`，并让 `MiniShop` 监听中控事件刷新金币文本，旧监听保留。

## 任务：执行：接入商店或NPC对话命令试点
- 编号：07
- 对应阶段：施工文档 v1.3 / 试点系统接入与解耦验证
- 状态：done
- 优先级：high
- 进度：100
- 文档：施工文档 v1.3
- 阶段：试点系统接入与解耦验证
- 标签：v1.3, 执行, CommandBus, Shop, NPC
- 目标：证明 UI 可以通过命令请求玩法行为，而不是直接操作具体系统。
- 任务键：myfarm-v1-3-exec-shop-or-dialogue-command-pilot
- 描述：
  AI 选择商店或 NPC 对话作为命令试点，定义至少一个强类型命令和处理器，例如购买物品、出售物品、打开对话或关闭对话。UI 或交互入口发送命令，玩法系统处理命令并返回 `CommandResult`。旧交互链路必须保持可用。

  完成记录：已选择商店购买试点，实现 `BuyShopItemCommand` 和 `ShopCommandAdapter`。`MiniShop` 与 `SimpleShopUI` 优先尝试命令，未注册命令时保留旧购买 fallback。

## 任务：验收：执行编译检查与中控核心行为验证
- 编号：08
- 对应阶段：施工文档 v1.3 / AI新功能接入模板与总体验收
- 状态：in_progress
- 优先级：high
- 进度：75
- 文档：施工文档 v1.3
- 阶段：AI新功能接入模板与总体验收
- 标签：v1.3, 验收, 编译, 中控
- 目标：确认中控核心可编译、可注册服务、可发布事件、可执行命令。
- 任务键：myfarm-v1-3-accept-core-orchestration-validation
- 描述：
  AI 执行编译检查，并通过最小验证脚本或编辑器验证记录证明：服务注册/获取/注销正常，事件订阅/取消/发布正常，命令注册/执行/失败返回正常，GameStateService 不依赖具体 UI 或场景对象。结果写入 `AcceptanceReport.md`。

  进展记录：`dotnet build .\FarmGame.Gameplay.csproj --no-restore -v:minimal` 已复查通过，0 个 C# 错误。中控目录 `.meta` 配套检查通过。Play Mode 行为验证因团结编辑器占用工程且 Unity MCP 不可用，仍待执行。

## 任务：验收：完成试点回归与新功能接入模板
- 编号：09
- 对应阶段：施工文档 v1.3 / AI新功能接入模板与总体验收
- 状态：in_progress
- 优先级：high
- 进度：65
- 文档：施工文档 v1.3
- 阶段：AI新功能接入模板与总体验收
- 标签：v1.3, 验收, 回归, 模板
- 目标：确认试点系统无回退，并沉淀 AI 新功能接入模板。
- 任务键：myfarm-v1-3-accept-pilot-regression-and-template
- 描述：
  AI 输出 `NewFeatureIntegrationTemplate.md`，模板必须包含服务接口、事件、命令、状态、存档、UI、回归测试和禁止事项。随后回归玩家移动、时间、存档、背包、商店、NPC、传送主链路，写入 `AcceptanceReport.md`。验收结论必须说明 v1.3 是否满足完成条件，以及剩余未迁移风险。

  进展记录：已输出 `NewFeatureIntegrationTemplate.md`、`PilotIntegrationRecord.md` 和 `AcceptanceReport.md`，并补齐 `SimpleShopUI` 的钱包事件刷新路径。Play Mode 回归尚未执行，v1.3 暂不判定为全量完成。
