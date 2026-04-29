# 我的农场 v1.3 Implementation Document

## Version Summary

施工版本：中控模式框架搭建。

本施工手册面向 AI 执行。AI 的目标不是“一次性重写项目”，而是在现有系统可运行的前提下，为后续新功能建立一条稳定接入路径：

```text
新功能 -> 服务接口 -> 命令 -> 功能系统 -> 事件 -> UI/其他系统
               \-> 运行时状态 -> 存档
```

旧系统允许通过适配器接入。`RuntimeRefs` 在本版本中作为兼容层保留，但新增功能不得继续把具体对象塞进 `RuntimeRefs`。

---

## 当前基线

### 已存在的中控相关脚本

- `Assets/Scripts/Core/AppRoot.cs`：跨场景根对象和常驻服务入口。
- `Assets/Scripts/Core/TaskManager.cs`：轻量主线程任务调度。
- `Assets/Scripts/Systems/GameManager.cs`：游戏启动、存档加载、时间初始化。
- `Assets/Scripts/Systems/RuntimeRefs.cs`：全局对象引用注册中心。
- `Assets/Scripts/Save/SaveManager.cs`：统一存档服务和分区存档调度。

### 当前核心问题

- `RuntimeRefs` 过度承担跨系统引用职责。
- UI 与 Gameplay 之间仍存在具体对象互相引用。
- 商店、NPC、对话等系统存在桥接和反射逻辑。
- 新增功能没有统一接入模板，容易出现重复单例、重复事件、重复存档入口。

---

## Milestone 1：中控基线审计与边界冻结

### 施工计划

1. 扫描并记录中心脚本职责。
2. 扫描 `RuntimeRefs` 当前注册对象、事件和使用方。
3. 将现有系统分成四类：
   - 核心服务：时间、存档、任务调度。
   - 玩法服务：背包、钱包、农耕、建造、商店、NPC、传送。
   - 表现层：UI、相机、HUD、对话框。
   - 兼容层：`RuntimeRefs`、旧单例、场景内手动引用。
4. 冻结本版本边界：只搭骨架、做试点、写模板，不全量迁移。

### 关键技术内容

- 使用静态扫描统计 `RuntimeRefs.`、`FindObjectOfType`、`GameObject.Find`、`Instance` 的热点。
- 输出迁移规则：哪些可以立即接入中控，哪些必须等下一版本。
- 明确 `RuntimeRefs` 的兼容使用规则：旧代码可用，新代码禁增。

### 交付物

- `DesignNotes/OrchestrationBaseline.md`
- `DesignNotes/RuntimeRefsMigrationRules.md`

---

## Milestone 2：中控核心骨架落地

### 施工计划

1. 新建中控目录：
   - `Assets/Scripts/Orchestration/`
   - `Assets/Scripts/Orchestration/Contracts/`
   - `Assets/Scripts/Orchestration/Events/`
   - `Assets/Scripts/Orchestration/Commands/`
   - `Assets/Scripts/Orchestration/Adapters/`
2. 新建核心类：
   - `GameRuntimeContext`
   - `GameServiceRegistry`
   - `GameEventBus`
   - `GameCommandBus`
   - `GameStateService`
   - `CommandResult`
3. 在 `AppRoot` 或兼容启动脚本中初始化中控上下文。
4. 确保中控层不直接引用具体 UI 类、不直接引用具体场景对象。

### 关键技术内容

#### GameServiceRegistry

- 使用接口类型作为 key。
- 支持 `Register<TService>`、`TryGet<TService>`、`Unregister<TService>`。
- 重复注册必须有明确覆盖策略或拒绝策略。

#### GameEventBus

- 使用强类型事件结构，不使用裸字符串事件名。
- 支持订阅、取消订阅、发布。
- 取消订阅必须可在 `OnDisable` / `OnDestroy` 调用，避免场景切换残留回调。

#### GameCommandBus

- 使用强类型命令。
- 命令处理器返回 `CommandResult`。
- 未注册命令处理器时必须返回失败结果，而不是抛空引用异常。

#### GameStateService

- 保存最小运行时状态。
- 不持有 UI 引用。
- 可被存档服务读取状态，也可从存档恢复状态。

### 交付物

- 中控核心代码。
- 必要 asmdef 或命名空间调整。
- 编译通过记录。

---

## Milestone 3：试点系统接入与解耦验证

### 施工计划

1. 时间系统接入：
   - 建立 `ITimeService`。
   - 通过适配器包装 `GameTimeSystem` / `TimeSystemAccessor`。
   - 发布时间变化事件。
2. 背包或钱包系统接入：
   - 建立 `IInventoryService` 或 `IWalletService`。
   - 将数量变化、金币变化发布为事件。
   - UI 通过事件刷新，不直接轮询具体系统。
3. 商店或 NPC 对话系统接入：
   - 建立购买、出售、打开对话、关闭对话等命令。
   - UI 发送命令，玩法系统处理命令。
   - 保留旧 UI 路径，直到试点验收完成。

### 关键技术内容

- 试点系统优先使用 Adapter，不强行改掉核心旧脚本。
- 事件只表达已经发生的事实，例如 `WalletChangedEvent`。
- 命令表达玩家或 UI 的意图，例如 `BuyItemCommand`。
- 服务接口只暴露稳定能力，不暴露内部 MonoBehaviour。

### 交付物

- 至少 3 个试点适配器。
- 试点事件和命令定义。
- 试点 UI 或系统改造记录。
- 解耦验证记录。

---

## Milestone 4：AI 新功能接入模板与总体验收

### 施工计划

1. 写出 `NewFeatureIntegrationTemplate.md`。
2. 模拟一个新功能，例如“钓鱼系统”，按模板列出应新增的服务、事件、命令、存档和 UI 接入点。
3. 执行编译检查。
4. 执行主链路回归：
   - 玩家移动
   - 时间推进
   - 存档保存/读取
   - 背包刷新
   - 商店交易
   - NPC 对话
   - 传送
5. 写入 `AcceptanceReport.md`。

### 关键技术内容

- 模板必须让 AI 能够独立新增功能，而不是每次重新设计架构。
- 模板必须明确禁止事项：
  - 不新增万能单例。
  - 不把新模块直接塞入 `RuntimeRefs`。
  - 不让 UI 直接改玩法数据。
  - 不绕过 `SaveManager` 自建长期存档。
- 验收报告必须写明未迁移的旧依赖和下一版本建议。

### 交付物

- `NewFeatureIntegrationTemplate.md`
- `AcceptanceReport.md`
- 编译/回归验证记录

---

## Recommended Implementation Order

```text
1. 输出中控基线审计
2. 输出 RuntimeRefs 迁移规则
3. 建立 Orchestration 目录和核心类
4. 接入 AppRoot 初始化
5. 接入时间系统试点
6. 接入背包或钱包系统试点
7. 接入商店或 NPC 对话命令试点
8. 输出新功能接入模板
9. 编译、回归、验收
```

## AI Construction Constraints

- 每个任务都必须能独立执行和验收。
- 每个任务完成后必须记录修改文件和验证结果。
- 任何旧系统大改前必须确认是否超出本版本边界。
- 新中控层不得依赖 UI 具体类。
- UI 不应直接修改核心玩法数据，应通过命令或服务接口请求。
- 事件订阅必须有取消订阅路径。
- 命令失败必须有可读错误信息。
- 存档接入必须优先复用 `SaveManager`。
