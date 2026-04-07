# 我的农场 v1.1 Implementation Document（NPC 模块化与独立调度，轻量）

## Version Summary

在现有 `ActorSystem` 基础上做轻量扩展：台词分组、属性模块、技能模块、低频 tick 调度，并且保持旧 NPC 可运行。

## 已有代码现状（可直接复用）

当前项目里已经具备以下基础（v1.1 继续沿用）：

- NPC 主线已收口到 `ActorSystem`：交互/对话/商店桥接不依赖旧 `NPCSystem` 运行时。
- `ActorModule` 已存在：模块化能力的基础形态已具备。
- 轻量调度器 `FarmGame.Core.TaskManager` 已存在：支持延迟/重复任务并可取消。
- `DialogueModule` 已使用 `TaskManager` 驱动打字机与延迟关闭：减少协程碎片与残留回调风险。
- `ActorBrain` 已移除逐帧 `Update()` 并修复状态持续时间计算。

## 施工约束（v1.1 统一口径）

1. 不引入重量级框架：不做 ECS、行为树、复杂技能树/技能图、条件 DSL 编辑器。
2. 新能力以“可选模块”落地：缺少模块时不影响对话/交互/商店主链路。
3. 数据结构优先可序列化与可扩展：`List<>` + SO 引用，避免 Dictionary 序列化陷阱。
4. 周期逻辑优先低频 tick：统一通过 `TaskManager` 注册，可取消且不产生大量 `Update()`。

## Version Tasks（建议拆分为可跟踪条目）

- T1. 台词分组数据结构 + `DialogueResolver` 选择逻辑
- T2. `ActorStatsModule`（属性）+ 内容管线写入
- T3. `SkillModule` + `SkillDefinitionSO`（技能与冷却）+ 内容管线写入
- T4. `ActorTickModule`（低频 tick）+ 驱动技能冷却闭环
- T5. 编辑器校验（Validator）+ 试点 NPC 回归验收

## 分阶段实现（写明用哪些具体代码）

### 阶段 1：台词分组（轻量）落地

目标：保留默认台词列表，同时支持少量分组与简单选择规则。

新增代码（建议）：
- `Assets/Scripts/ActorSystem/Dialogue/DialogueSetSO.cs`
  - `DialogueGroup { string key; List<string> lines; }`
  - `DialogueSetSO { List<DialogueGroup> groups; }`

修改代码：
- `Assets/Scripts/ActorSystem/DialogueResolver.cs`
  - 新增字段：`public DialogueSetSO dialogueSet;`
  - 在 `GetCurrentDialogue()` 里增加选择规则（建议顺序）：
    1. 若 `ActorMemory.HasCompletedFirstDialogue == false` 且存在 `first_meet` 组，优先 `first_meet`
    2. 否则按好感度分级映射 `stranger/acquaintance/friend/close_friend`（存在则用）
    3. 否则 `daily`（存在则用）
    4. 最后回退 `ActorIdentity.GetDefaultDialog()`

内容输入（二选一，建议 A 更轻量）：
- A) 扩展 `Assets/Scripts/NPCSystem/NPCDefinition.cs`：增加 `public DialogueSetSO dialogueSet;`（可选）
- B) 新建 `ActorProfileSO` 并让 `NPCDefinition` 引用 profile（改动更大，后续可做）

编辑器构建：
- `Assets/Scripts/Editor/NPC/NPCPrefabBuilder.cs`
  - 构建时把 `NPCDefinition.dialogueSet` 写入 `DialogueResolver.dialogueSet`

验收（试点 1 个 NPC）：
- 首次对话走 `first_meet`，重复对话走 `daily` 或好感度分组。

### 阶段 2：属性模块 ActorStatsModule

目标：NPC 有统一属性入口，不引入战斗框架。

新增代码：
- `Assets/Scripts/ActorSystem/ActorStatsModule.cs`
  - `StatEntry { string key; float value; }`
  - 字段：`List<StatEntry> baseStats`
  - API：
    - `float Get(string key, float defaultValue = 0)`
    - `void Set(string key, float value)`
    - `void Add(string key, float delta)`

内容输入：
- 扩展 `Assets/Scripts/NPCSystem/NPCDefinition.cs`：增加 `List<StatEntry> baseStats`（可选）

编辑器构建：
- `Assets/Scripts/Editor/NPC/NPCPrefabBuilder.cs`
  - baseStats 非空则挂 `ActorStatsModule` 并写入数据
  - baseStats 为空则不挂载（旧 NPC 不受影响）

验收（试点 1 个 NPC）：
- 能读写一个属性（例如 `friendship_gain_multiplier`），缺省 NPC 不报错。

### 阶段 3：技能模块 SkillModule（数据驱动，轻量）

目标：NPC 有技能列表 + 冷却 + 触发，不做技能树。

新增代码：
- `Assets/Scripts/ActorSystem/Skills/SkillDefinitionSO.cs`
  - 字段：`skillId`、`displayName`、`cooldownSeconds`、少量参数（比如数值/范围/文本）
- `Assets/Scripts/ActorSystem/Skills/SkillModule.cs`
  - 字段：`List<SkillDefinitionSO> skills`
  - 运行时冷却表（不序列化）
  - API：
    - `bool TryCast(string skillId)`
    - `bool IsReady(string skillId)`
    - `void Tick(float dt)`（用于冷却刷新）

内容输入：
- 扩展 `Assets/Scripts/NPCSystem/NPCDefinition.cs`：增加 `List<SkillDefinitionSO> skills`（可选）

编辑器构建：
- `Assets/Scripts/Editor/NPC/NPCPrefabBuilder.cs`
  - skills 非空则挂 `SkillModule` 并写入引用

验收（试点 1 个 NPC）：
- 触发技能后进入冷却，冷却结束可再次触发。

### 阶段 4：独立调度 ActorTickModule（低频 tick，轻量）

目标：NPC 不靠 `Update()`，而是低频被调度执行自身逻辑。

新增代码：
- `Assets/Scripts/ActorSystem/ActorTickModule.cs`
  - 字段：`float tickInterval = 0.5f`、`bool useUnscaledTime`
  - 内部：用 `FarmGame.Core.TaskManager.ScheduleRepeating` 注册重复任务
  - 生命周期：`Enable()` 注册，`Disable()`/`OnDestroy()` 取消

最小闭环（建议）：
- `ActorTickModule` 每 tick 调用 `SkillModule.Tick(tickInterval)`（若存在 SkillModule）

验收：
- 技能冷却由 tick 驱动更新，禁用/切场景无残留回调。

### 阶段 5：编辑器校验与回归

目标：避免“配置了数据但 prefab 没挂模块/引用”。

修改建议：
- `Assets/Scripts/Editor/NPC/Phase04NpcValidator.cs`
  - 若配置了 `dialogueSet/baseStats/skills`，则 prefab 必须包含对应模块/引用

回归建议：
- 走 `VersionDocs/phase-04-playtest-script.md` 的关键项（对话/切换/商店）
- 再追加 1 轮：台词分组、属性读写、技能冷却、tick 调度

## Quick Wins（基于现有代码的轻量优化建议，可选）

1. `ActorMemory.Flags` 当前是逗号字符串 + `Contains()`：
   - 易误判子串，且 split/join 开销大
   - 建议：运行时 `HashSet<string>`，序列化层仍可用 string 或 `List<string>`
2. `DialogueResolver` 的 `_hasCheckedFlaggedDialogue` 类缓存逻辑容易让运行时变化失效：
   - 建议：每次按需检查，或在 `SetFlag/SetFlaggedDialogue` 时重置缓存
3. `ActorIdentity.DialogLines` 在 `_dialogLines == null` 时反复 new list：
   - 建议：初始化时保证非 null 或返回静态空列表
