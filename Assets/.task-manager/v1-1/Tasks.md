# 我的农场 v1.1 任务文件

> 这个文件里的每个 `## 任务：...` 块，都会被 TaskManager 解析成任务。
> 新增任务时，复制下面的模板块并修改标题；如果你想让任务改名后仍然保持同一条记录，可以补一个唯一的“任务键”。
> 文档 / 阶段留空时，会自动归到默认归属。

## 使用说明

1. 每个任务都从 `## 任务：请把这里改成任务标题` 这种标题开始。
2. 支持字段：编号、对应阶段、状态(todo / in_progress / done)、优先级(low / medium / high)、进度(0-100)。
3. 描述支持多行，写在 `- 描述：` 的下一行并保持两个空格缩进。
4. 每个任务都必须明确对应到某个文档阶段；建议同时填写“对应阶段”以及下面的“文档 / 阶段”字段。
5. 编号越小代表越先执行；如果不填编号，系统会按文件顺序自动补号。
6. 保存文件后，回到桌面端点击一次“刷新”（或执行一次“从文件夹恢复当前项目”），就会把新增任务读回 TaskManager。

## 默认归属
- 文档：施工文档 v1.1
- 阶段：云城规划 开发

## 新增任务模板
## 任务：请把这里改成任务标题
- 编号：01
- 对应阶段：施工文档 v1.1 / 云城规划 开发
- 状态：todo
- 优先级：medium
- 进度：0
- 文档：施工文档 v1.1
- 阶段：云城规划 开发
- 标签：
- 目标：
- 任务键：
- 描述：
  在这里写任务说明。

## 当前任务
## 任务：确认 v1.1 NPC 模块化边界与交付清单
- 编号：01
- 对应阶段：目标文档 v1.1 / 版本目标与范围
- 状态：done
- 优先级：high
- 进度：100
- 文档：目标文档 v1.1
- 阶段：版本目标与范围
- 标签：v1.1, 规划, NPC系统
- 目标：先把 v1.1 的最小闭环、明确不做范围、试点 NPC 和验收口径一次性定清楚，避免后续模块实现发散
- 任务键：myfarm-v1-1-scope-definition
- 描述：
  请阅读 `Assets/.task-manager/v1-1/TargetDocument.md` 与 `Assets/.task-manager/v1-1/ImplementationDocument.md`，输出一份 v1.1 边界确认结论。
  必须明确 4 个纳入范围：台词分组、属性模块、技能模块、低频 tick。
  必须明确 4 个不纳入范围：ECS、行为树、复杂技能树/技能图、条件 DSL 编辑器。
  必须指定 1 个试点 NPC，并说明为什么它最适合作为本版本验证对象。
  必须列出版本完成的判定标准，至少覆盖：对话主链路正常、首次/重复台词切换、属性可读写、技能冷却闭环、低频 tick 无残留回调。
  输出结果应可直接作为本版本开发过程中的统一约束说明，供后续所有任务引用，不允许只写抽象目标。

## 任务：设计台词分组与 NPC 扩展数据结构
- 编号：02
- 对应阶段：施工文档 v1.1 / 设计与拆解
- 状态：done
- 优先级：high
- 进度：100
- 文档：施工文档 v1.1
- 阶段：设计与拆解
- 标签：数据结构, Dialogue, SO
- 目标：把台词分组、属性、技能的数据入口设计成可序列化、可扩展、兼容旧 NPC 的轻量结构
- 任务键：myfarm-v1-1-data-model-design
- 描述：
  请基于当前 `ActorSystem` 与 `NPCDefinition` 的现有结构，设计 v1.1 所需的数据模型，不要引入复杂框架。
  需要明确 `DialogueSetSO`、`DialogueGroup`、`StatEntry`、`SkillDefinitionSO`、`NPCDefinition` 新增字段的最终字段清单与用途说明。
  所有新增结构优先使用 `List<>` + ScriptableObject 引用，避免 Dictionary 序列化问题。
  需要保证旧 NPC 在不配置 `dialogueSet`、`baseStats`、`skills` 时仍可运行，不报错、不丢失原有对话和交互能力。
  输出应包含每个字段的类型、默认值策略、缺省行为、编辑器写入路径，以及和旧数据的兼容方式。
  结果应足够详细，让后续编码任务可以直接按这个结构实现而不需要再次推翻方案。

## 任务：实现 DialogueSetSO 与 DialogueResolver 分组选择逻辑
- 编号：03
- 对应阶段：施工文档 v1.1 / 云城规划 开发
- 状态：done
- 优先级：high
- 进度：100
- 文档：施工文档 v1.1
- 阶段：云城规划 开发
- 标签：Dialogue, Resolver, NPC
- 目标：让 NPC 在保留默认台词 fallback 的前提下，支持首次会面、日常、好感度分组切换
- 任务键：myfarm-v1-1-dialogue-grouping
- 描述：
  请新增 `Assets/Scripts/ActorSystem/Dialogue/DialogueSetSO.cs`，实现 `DialogueGroup { string key; List<string> lines; }` 与 `DialogueSetSO { List<DialogueGroup> groups; }`。
  请修改 `Assets/Scripts/ActorSystem/DialogueResolver.cs`，新增 `dialogueSet` 输入，并在 `GetCurrentDialogue()` 中实现轻量选择顺序：`first_meet` 优先，其次按好感度映射 `stranger/acquaintance/friend/close_friend`，再回退 `daily`，最后回退 `ActorIdentity.GetDefaultDialog()`。
  要求保证没有分组资源时仍能走旧逻辑，不能让现有 NPC 对话失效。
  需要处理空组、空文本、缺失 key、重复触发首次会面等边界情况。
  交付时请给出至少 1 个试点 NPC 的验证步骤：首次对话命中 `first_meet`，再次对话命中 `daily` 或好感度分组。
  描述中请记录所有可能破坏旧链路的风险点，并写明如何规避。

## 任务：扩展 NPCPrefabBuilder 写入台词分组内容管线
- 编号：04
- 对应阶段：施工文档 v1.1 / 云城规划 开发
- 状态：done
- 优先级：medium
- 进度：100
- 文档：施工文档 v1.1
- 阶段：云城规划 开发
- 标签：Editor, Builder, Dialogue
- 目标：把内容资产中的台词分组稳定写入 prefab，使策划配置可以真正进入运行时
- 任务键：myfarm-v1-1-dialogue-builder-pipeline
- 描述：
  请检查 `Assets/Scripts/Editor/NPC/NPCPrefabBuilder.cs` 当前的构建流程，并在不破坏旧构建逻辑的前提下，把 `NPCDefinition.dialogueSet` 写入 `DialogueResolver.dialogueSet`。
  如果 `dialogueSet` 为空，构建结果必须与旧版本一致，不能强制挂载无意义组件或产生空引用。
  需要确认构建产物中的组件顺序、引用注入时机和保存逻辑，避免出现 prefab 已挂模块但未写入资源引用的情况。
  请补充验证步骤：重新构建试点 NPC prefab，进入场景后对话逻辑应与配置一致。
  输出结果要明确说明：策划在哪个资产上填数据，编辑器在哪一步写入，运行时从哪个组件读取。

## 任务：实现 ActorStatsModule 与基础属性读写能力
- 编号：05
- 对应阶段：施工文档 v1.1 / 云城规划 开发
- 状态：done
- 优先级：high
- 进度：100
- 文档：施工文档 v1.1
- 阶段：云城规划 开发
- 标签：Stats, Module, Actor
- 目标：给 NPC 提供一个统一的轻量属性入口，为后续技能和关系逻辑提供基础支撑
- 任务键：myfarm-v1-1-stats-module
- 描述：
  请新增 `Assets/Scripts/ActorSystem/ActorStatsModule.cs`，实现 `StatEntry { string key; float value; }` 与 `Get/Set/Add` 三个核心接口。
  模块必须支持按 key 查询属性，不存在时返回默认值，且对缺省 NPC 安全无报错。
  请同步扩展 `Assets/Scripts/NPCSystem/NPCDefinition.cs` 的 `baseStats` 输入结构，并在 `NPCPrefabBuilder` 中实现“有数据则挂模块，无数据则保持旧 NPC 不变”的写入规则。
  需要给出 1 个明确示例属性，比如 `friendship_gain_multiplier`，并验证读取、覆盖、累加三个路径都能工作。
  请确保实现不会引入战斗框架或复杂数值系统，当前只做轻量 key/value 属性层。
  交付结果需要附带模块职责说明、使用方式、缺省行为和一轮试点 NPC 的验证记录。

## 任务：实现 SkillDefinitionSO 与 SkillModule 冷却闭环
- 编号：06
- 对应阶段：施工文档 v1.1 / 云城规划 开发
- 状态：done
- 优先级：high
- 进度：100
- 文档：施工文档 v1.1
- 阶段：云城规划 开发
- 标签：Skill, Cooldown, SO
- 目标：让 NPC 拥有数据驱动的技能列表、触发入口和冷却状态，但不引入复杂技能树
- 任务键：myfarm-v1-1-skill-module
- 描述：
  请新增 `Assets/Scripts/ActorSystem/Skills/SkillDefinitionSO.cs` 与 `Assets/Scripts/ActorSystem/Skills/SkillModule.cs`。
  `SkillDefinitionSO` 需要至少包含：`skillId`、`displayName`、`cooldownSeconds` 以及少量必要参数；`SkillModule` 需要至少提供 `TryCast`、`IsReady`、`Tick(float dt)`。
  运行时冷却表不要求序列化，但必须保证同一技能触发后进入冷却，冷却结束前不能重复成功施放。
  请同步扩展 `NPCDefinition.skills` 输入，并在 `NPCPrefabBuilder` 中实现“配置了技能才挂 SkillModule”的写入策略。
  需要补充 1 个试点技能的验证脚本：首次触发成功、冷却期间失败、冷却结束后再次成功。
  全过程必须保持轻量，不做技能树、复杂条件组合或行为图编辑器。

## 任务：实现 ActorTickModule 并用低频调度驱动技能刷新
- 编号：07
- 对应阶段：施工文档 v1.1 / 云城规划 开发
- 状态：done
- 优先级：high
- 进度：100
- 文档：施工文档 v1.1
- 阶段：云城规划 开发
- 标签：Tick, TaskManager, Scheduler
- 目标：建立 NPC 独立调度能力，让周期逻辑统一走低频 tick，而不是新增大量 Update
- 任务键：myfarm-v1-1-actor-tick-module
- 描述：
  请新增 `Assets/Scripts/ActorSystem/ActorTickModule.cs`，并基于现有 `FarmGame.Core.TaskManager.ScheduleRepeating` 实现重复调度。
  模块至少需要支持 `tickInterval`、`useUnscaledTime`，并在启用时注册、禁用或销毁时可靠取消，避免场景切换后残留回调。
  当前版本的最小闭环是：每次 tick 调用 `SkillModule.Tick(tickInterval)`，让技能冷却通过低频调度刷新，而不是依赖 `Update()`。
  需要验证 3 个关键场景：正常运行时冷却递减、禁用 NPC 后停止调度、切场景或销毁对象后无悬挂回调。
  如果现有代码里还有其他逐帧逻辑涉及 NPC 状态刷新，请在不扩大版本范围的前提下评估是否可迁移到本模块。
  输出中要明确说明 tick 周期选择依据，以及为什么当前实现不会带来额外性能负担。

## 任务：补齐编辑器校验器，防止配置与 prefab 不一致
- 编号：08
- 对应阶段：施工文档 v1.1 / 联调与发布
- 状态：done
- 优先级：medium
- 进度：100
- 文档：施工文档 v1.1
- 阶段：联调与发布
- 标签：Validator, Editor, QA
- 目标：让编辑器在构建和验收前就能发现“配了数据但没挂模块”这类配置错误
- 任务键：myfarm-v1-1-validator
- 描述：
  请检查并修改 `Assets/Scripts/Editor/NPC/Phase04NpcValidator.cs`，新增对 `dialogueSet`、`baseStats`、`skills` 的一致性校验。
  校验规则至少包括：配置了台词分组就必须存在对应引用，配置了属性就必须有 `ActorStatsModule`，配置了技能就必须有 `SkillModule`，启用了低频调度闭环时必须存在 `ActorTickModule` 或等效驱动路径。
  校验输出需要可读，能直接告诉开发或策划缺了什么、在哪个 prefab 或资产上缺、下一步应该补什么。
  需要避免把“可选模块为空”误判成错误，重点抓“数据已配置但运行时组件没挂上”的不一致问题。
  交付时请附带至少 2 个正例和 2 个反例，确保校验器真的能挡住错误构建。

## 任务：执行 v1.1 试点 NPC 全链路回归验收
- 编号：09
- 对应阶段：目标文档 v1.1 / 版本验收与发布
- 状态：done
- 优先级：high
- 进度：100
- 文档：目标文档 v1.1
- 阶段：版本验收与发布
- 标签：验收, 回归, Playtest
- 目标：基于试点 NPC 验证本版本的五个核心目标全部达成，并形成可复用的回归脚本
- 任务键：myfarm-v1-1-playtest
- 描述：
  请基于试点 NPC 执行一轮完整回归，覆盖对话打开、首次/重复台词切换、商店或原有功能链路、属性读写、技能触发与冷却、低频 tick 调度、禁用/切场景后的回调清理。
  需要结合 `VersionDocs/phase-04-playtest-script.md`（如果存在）与当前 v1.1 文档目标，整理一份实际执行清单；如果该脚本不存在，就按本任务要求补一份最小可执行脚本。
  验收结果必须按“通过 / 失败 / 风险待观察”分类记录，并对每个失败项给出复现步骤、影响范围和建议修复方向。
  如果主链路（对话、交互、商店）出现任何回退，应直接阻断版本完成判定，不能用“模块功能已完成”掩盖主流程退化。
  最终输出应明确说明：v1.1 是否满足发布条件，如果不满足，剩余阻塞项是什么。
