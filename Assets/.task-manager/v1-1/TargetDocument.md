# 我的农场 v1.1 Target Document（NPC 系统模块化与独立调度，轻量）

## Version Summary

v1.1 完善 NPC 系统，使每个 NPC 成为可独立调度的模块化个体：台词、属性、技能等内容可组合、可扩展，并且不引入重量级框架。

## 系统定位（v1.1 的 NPC 系统是什么）

v1.1 目标里的“NPC 系统”以 `FarmGame.ActorSystem` 为运行时主线：

- 一个 NPC = 一个 `Actor` GameObject + 一组组件（Identity/Memory/Brain/Modules）
- `FarmGame.NPCSystem.NPCDefinition` 继续作为内容资产与编辑器构建输入，不作为运行时主链路依赖
- 核心交互链路保持不变：`PlayerInteractor` -> `IInteractable` -> `IDialogSubject` -> `IDialogUI`

## 主要目标

### G1. NPC 成为“独立个体”（Actor）

- 角色结构标准化：每个 NPC 至少具备 Identity/Memory/Brain/Interaction 这条主线组件结构。
- 可被识别和调度：NPC 的能力以模块形式挂载，缺省也可运行。

### G2. 模块化（台词/属性/技能），保持轻量

- 台词：
  - 保留默认台词列表作为 fallback；
  - 支持少量分组（2-4 组）与简单选择规则（首次/重复、好感度分级），不做复杂条件树/DSL。
- 属性：
  - 提供统一属性入口（建议 `ActorStatsModule`）；
  - 用 `List<StatEntry>`（key/value）承载，方便序列化与扩展。
- 技能：
  - 提供轻量技能模块（建议 `SkillModule`）；
  - 数据驱动（建议 ScriptableObject），包含冷却与触发能力。

### G3. NPC 可独立调度（低频 tick）

- NPC 可以以低频 tick（例如 0.2s 或 0.5s）刷新技能冷却、日常作息、状态等逻辑。
- 不在每个模块里新增 `Update()`；优先使用统一调度器 `FarmGame.Core.TaskManager` 注册可取消任务。

### G4. 扩展性与迁移友好

- 新增模块不需要改动对话/交互/商店主链路。
- 允许增量迁移：旧 NPC 不配置新字段时仍可运行，新能力按“可选字段/可选模块”启用。

## 使用方法（怎么用这套 NPC 系统）

### 1) 内容制作（策划/关卡）

目标：不写代码也能做 NPC，且能逐步增强能力。

1. 创建/配置内容资产：
   - 创建 `Farm/NPC Definition`（`FarmGame.NPCSystem.NPCDefinition`）。
   - 填写 `npcId/npcName/dialogLines`、模型/动画、功能配置（商店/任务等）。
2. 构建 NPC Prefab（编辑器）：
   - 通过 `NPCPrefabBuilder` 按 Actor 主线构建。
   - 预制体应包含：`Actor`、`ActorIdentity`、`ActorInteraction`、`ActorBrain`、`ActorMemory`、`ActorDialogue`、`DialogueResolver`、`ActorView`（以及功能模块）。
3. 场景摆放与验证：
   - 靠近提示可见，按 E 可对话；
   - 对话中可就近切换到另一个 NPC；
   - 功能型 NPC（商店/任务）按钮可用。

增强能力（v1.1 目标，按需启用）：
- 台词分组：配置 `first_meet/daily/friend/close_friend` 等 2-4 组，运行时按简单规则选择。
- 属性：配置基础属性条目（key/value），用于技能或关系等逻辑。
- 技能：配置技能列表（技能定义与冷却），按交互点触发。
- 调度：启用低频 tick，用于技能冷却/日常刷新。

### 2) 程序扩展（怎么加一个新模块）

约定：
- 新模块继承 `FarmGame.ActorSystem.ActorModule`。
- 模块必须可选挂载：缺失时不影响对话/交互主链路。
- 模块需要周期刷新时：
  - 优先订阅 `ActorTickModule` 的 tick；或
  - 自行使用 `TaskManager.ScheduleRepeating` 注册，并在 `OnDisabled()`/`OnDestroy()` 取消。
- 模块序列化数据尽量简单：`List<>`、SO 引用、少量字段；避免复杂嵌套与反射链。

## Stages

### 阶段 A：版本目标与边界确认

- Status: todo
- Summary: 确定 v1.1 的最小闭环（台词分组 + 属性模块 + 技能模块 + 低频 tick），明确不做 ECS/行为树/复杂技能图。

### 阶段 B：NPC 数据模型（轻量化）

- Status: todo
- Summary: 为台词分组/属性/技能定义可序列化、易扩展的数据结构（优先 list + SO）。

### 阶段 C：NPC 模块化能力（台词/属性/技能）

- Status: todo
- Summary: 落地台词分组选择、`ActorStatsModule`、`SkillModule`，并保证旧 NPC 缺省不报错。

### 阶段 D：NPC 独立调度（轻量调度）

- Status: todo
- Summary: 落地 `ActorTickModule`（基于 `TaskManager`），并用它驱动至少一个真实闭环（如技能冷却刷新）。

### 阶段 E：版本验收与发布

- Status: todo
- Summary: 形成验收清单、回归脚本与发布条件，明确回滚策略与试点 NPC 清单。

## 验收口径（v1.1 最小闭环）

1. 主链路不回退：对话打开/切换/关闭、商店等功能仍可用。
2. 台词模块化：至少 2 组台词能按简单条件切换。
3. 属性模块化：至少 1 个 NPC 可读写属性条目（缺省 NPC 不报错）。
4. 技能模块化：至少 1 个 NPC 可触发技能且有冷却（禁用/切场景无残留回调）。
5. 独立调度：至少 1 个 NPC 开启低频 tick，并且没有新增大量 `Update()`。
