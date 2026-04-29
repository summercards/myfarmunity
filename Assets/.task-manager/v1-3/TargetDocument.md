# 我的农场 v1.3 Target Document

## Version Summary

版本名称：中控模式框架搭建。

本版本目标是建立一套可持续迭代的中控模式框架，用中控系统串联时间、存档、背包、钱包、UI、NPC、农耕、建造、商店、传送等系统。框架必须允许单个系统独立开发、独立替换、独立验收，不因为新增一个功能而直接修改大量其他系统。

本版本不是重写整个项目，也不是一次性移除所有旧依赖。施工策略是先搭建中控骨架，再选择少量试点系统接入，最后形成新增功能的标准接入流程。

---

## Version Goal

- [ ] 建立统一中控层，明确启动、服务注册、事件、命令、运行时状态、存档调度的边界。
- [ ] 降低 `RuntimeRefs` 继续扩张的风险，将新增功能接入路径从“直接找对象”调整为“服务接口 + 事件 + 命令”。
- [ ] 完成至少 3 个现有系统的试点接入，证明单系统可以独立迭代。
- [ ] 建立 AI 可执行的新增功能接入模板，让后续钓鱼、烹饪、宠物、任务等系统可按模板接入。
- [ ] 保证现有主循环、存档、玩家控制、背包、商店、NPC、传送流程不出现回退。

---

## Scope

### 纳入范围

1. 中控层基础设施
   - `GameRuntimeContext`
   - `GameServiceRegistry`
   - `GameEventBus`
   - `GameCommandBus`
   - `GameStateService`
   - 必要的接口、事件、命令和适配器目录

2. 现有系统兼容接入
   - 保留 `AppRoot`、`TaskManager`、`GameManager`、`SaveManager` 当前职责。
   - 通过适配器接入现有服务，不直接大规模重构旧系统。
   - 将 `RuntimeRefs` 作为兼容层处理，新增功能不得继续扩展其具体对象清单。

3. 试点系统
   - 时间系统：作为只读/事件型服务试点。
   - 背包或钱包系统：作为数据变化和 UI 刷新试点。
   - 商店或 NPC 对话系统：作为 UI 发送命令、玩法系统处理命令的试点。

4. 文档和 AI 施工规范
   - 中控架构说明。
   - 新功能接入模板。
   - 试点系统接入记录。
   - 验收清单和回归清单。

### 不纳入范围

- 不重写全部玩法系统。
- 不引入 ECS、行为树、复杂依赖注入框架或第三方消息总线。
- 不强制迁移所有已有 `RuntimeRefs` 调用。
- 不改变存档文件格式，除非为兼容中控接口添加向后兼容字段。
- 不新增大规模 UI 重做。

---

## Milestones

### 里程碑 1：中控基线审计与边界冻结

**Status**: todo

**目标**：AI 先审计现有中心脚本和跨系统引用，冻结本版本允许改造的边界，避免施工时无限扩大范围。

**必须产物**：
- `Assets/.task-manager/v1-3/DesignNotes/OrchestrationBaseline.md`
- `Assets/.task-manager/v1-3/DesignNotes/RuntimeRefsMigrationRules.md`

**验收标准**：
- [ ] 明确列出当前中控相关脚本：`AppRoot`、`GameManager`、`RuntimeRefs`、`TaskManager`、`SaveManager`。
- [ ] 明确哪些职责保留、哪些职责迁移、哪些职责本版本不动。
- [ ] 明确新增功能禁止直接扩展 `RuntimeRefs` 具体对象清单。
- [ ] 明确本版本试点系统，且数量不少于 3 个。
- [ ] 审计结果能直接指导后续 AI 编码任务，不依赖口头解释。

---

### 里程碑 2：中控核心骨架落地

**Status**: todo

**目标**：建立最小可运行中控层，不破坏旧系统，同时给新增系统提供统一入口。

**建议落点**：
- `Assets/Scripts/Orchestration/`
- `Assets/Scripts/Orchestration/Contracts/`
- `Assets/Scripts/Orchestration/Events/`
- `Assets/Scripts/Orchestration/Commands/`
- `Assets/Scripts/Orchestration/Adapters/`

**验收标准**：
- [ ] 存在清晰的中控命名空间，例如 `FarmGame.Orchestration`。
- [ ] `GameServiceRegistry` 可注册、获取、注销接口服务。
- [ ] `GameEventBus` 可发布和订阅强类型事件，并支持取消订阅。
- [ ] `GameCommandBus` 可注册和执行强类型命令处理器，并能返回成功/失败结果。
- [ ] `GameStateService` 可保存最小运行时状态快照，不直接依赖具体 UI 或场景对象。
- [ ] 中控层可以由 `AppRoot` 或等效启动入口初始化。
- [ ] 编译无错误，现有主流程不因中控层加入而改变。

---

### 里程碑 3：试点系统接入与解耦验证

**Status**: todo

**目标**：选择至少 3 个现有系统通过适配器接入中控层，证明“单系统可独立迭代”的实际效果。

**建议试点**：
1. 时间系统：发布 `GameHourChanged`、`GameDayChanged`、`SeasonChanged` 等事件。
2. 背包或钱包系统：发布 `InventoryChanged` 或 `WalletChanged`，供 UI 监听。
3. 商店或 NPC 对话系统：UI 通过命令请求购买、出售、打开对话或关闭对话。

**验收标准**：
- [ ] 至少 3 个试点系统完成中控接入。
- [ ] 试点 UI 不再必须直接持有对应玩法系统的具体 MonoBehaviour 引用。
- [ ] 试点系统可以通过接口、事件或命令进行通信。
- [ ] 旧场景仍可运行，旧配置不需要一次性重做。
- [ ] 任一试点系统禁用或替换时，其他试点系统不会出现编译级依赖错误。

---

### 里程碑 4：AI 新功能接入模板与总体验收

**Status**: todo

**目标**：把中控模式沉淀为 AI 可以重复执行的施工模板，并完成回归验收。

**必须产物**：
- `Assets/.task-manager/v1-3/NewFeatureIntegrationTemplate.md`
- `Assets/.task-manager/v1-3/AcceptanceReport.md`

**验收标准**：
- [ ] 新功能模板必须包含：服务接口、事件、命令、运行时状态、存档接入、UI 接入、回归测试。
- [ ] 模板必须说明 AI 新增功能时允许创建哪些文件、禁止修改哪些中心文件。
- [ ] 完成一次模拟新功能接入评审，例如“钓鱼系统”或“烹饪系统”，证明模板可执行。
- [ ] 回归验证覆盖玩家移动、时间、存档、背包、商店、NPC、传送。
- [ ] 没有新增编译错误。
- [ ] 版本完成结论明确写入 `AcceptanceReport.md`。

---

## Version Acceptance

本版本完成必须同时满足：

- [ ] 4 个里程碑全部通过。
- [ ] 总任务全部完成，且所有验收类任务状态为 done。
- [ ] 中控层代码编译通过。
- [ ] 至少 3 个系统完成试点接入。
- [ ] 新增功能模板可被 AI 直接拿来执行。
- [ ] 没有把 `RuntimeRefs` 继续扩大成新的全局对象清单。
- [ ] 不破坏现有玩家移动、存档、时间、背包、商店、NPC、传送主链路。

## AI Execution Rules

- AI 执行任务前必须先阅读本文件和 `ImplementationDocument.md`。
- AI 编码时优先新增中控层和适配器，不直接大改旧玩法系统。
- AI 不得把中控层做成一个巨大的万能 MonoBehaviour。
- AI 必须保持服务、事件、命令、状态四类职责分离。
- AI 每完成一个试点系统，都必须记录接入路径、改动文件、验证结果和剩余风险。
