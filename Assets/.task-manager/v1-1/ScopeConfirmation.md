# 我的农场 v1.1 NPC 模块化边界确认文档

> 本文档为 v1.1 版本开发的统一约束说明，所有后续任务必须严格遵循此边界。
>
> 创建时间：2026-04-07
> 状态：已确认
> 版本：v1.1

---

## 一、纳入范围（4 个核心能力）

### 1. 台词分组（Dialogue Grouping）

**目标**：在保留默认台词 fallback 的前提下，支持少量分组与简单选择规则。

**实现方案**：
- 新增 `DialogueSetSO.cs` ScriptableObject
  - 数据结构：`DialogueGroup { string key; List<string> lines; }`
  - 容器：`DialogueSetSO { List<DialogueGroup> groups; }`
- 修改 `DialogueResolver.cs`
  - 新增字段：`public DialogueSetSO dialogueSet;`
  - 选择逻辑优先级：
    1. 若 `ActorMemory.HasCompletedFirstDialogue == false` 且存在 `first_meet` 组 → 优先使用
    2. 否则按好感度映射 `stranger/acquaintance/friend/close_friend` → 存在则使用
    3. 否则使用 `daily` 组 → 存在则使用
    4. 最后回退 `ActorIdentity.GetDefaultDialog()`

**验收标准**：
- 存在 `first_meet` 组时，首次对话必须命中该组
- 重复对话按好感度分组切换正常
- 无分组资源时，必须走旧逻辑，现有 NPC 对话不能失效
- 处理空组、空文本、缺失 key、重复触发首次会面等边界情况

---

### 2. 属性模块（Actor Stats Module）

**目标**：提供统一属性入口，为后续技能和关系逻辑提供基础支撑。

**实现方案**：
- 新增 `ActorStatsModule.cs`（继承 `ActorModule`）
  - 数据结构：`StatEntry { string key; float value; }`
  - 字段：`List<StatEntry> baseStats`
  - 核心接口：
    - `float Get(string key, float defaultValue = 0)`
    - `void Set(string key, float value)`
    - `void Add(string key, float delta)`
- 扩展 `NPCDefinition.cs`
  - 新增字段：`List<StatEntry> baseStats`（可选）
- 修改 `NPCPrefabBuilder.cs`
  - baseStats 非空 → 挂载 `ActorStatsModule` 并写入数据
  - baseStats 为空 → 不挂载模块，保持旧 NPC 不变

**验收标准**：
- 至少验证 1 个示例属性（如 `friendship_gain_multiplier`）
- 读取、覆盖、累加三个路径都能正常工作
- 缺省 NPC 不报错，安全无异常
- 不引入战斗框架或复杂数值系统，仅做轻量 key/value 层

---

### 3. 技能模块（Skill Module）

**目标**：让 NPC 拥有数据驱动的技能列表、触发入口和冷却状态。

**实现方案**：
- 新增 `SkillDefinitionSO.cs` ScriptableObject
  - 最少字段：`skillId`、`displayName`、`cooldownSeconds`
  - 可选字段：少量必要参数（数值/范围/文本）
- 新增 `SkillModule.cs`（继承 `ActorModule`）
  - 字段：`List<SkillDefinitionSO> skills`
  - 运行时冷却表（不序列化）
  - 核心接口：
    - `bool TryCast(string skillId)` → 尝试施放
    - `bool IsReady(string skillId)` → 检查冷却
    - `void Tick(float dt)` → 刷新冷却
- 扩展 `NPCDefinition.cs`
  - 新增字段：`List<SkillDefinitionSO> skills`（可选）
- 修改 `NPCPrefabBuilder.cs`
  - skills 非空 → 挂载 `SkillModule` 并写入引用
  - skills 为空 → 不挂载模块

**验收标准**：
- 同一技能触发后进入冷却，冷却结束前不能重复成功施放
- 至少验证 1 个试点技能：首次触发成功、冷却期间失败、冷却结束后再次成功
- 不做技能树、复杂条件组合或行为图编辑器
- 保持轻量，数据驱动

---

### 4. 低频 tick（Actor Tick Module）

**目标**：建立 NPC 独立调度能力，让周期逻辑统一走低频 tick，不新增大量 `Update()`。

**实现方案**：
- 新增 `ActorTickModule.cs`（继承 `ActorModule`）
  - 字段：`float tickInterval = 0.5f`、`bool useUnscaledTime`
  - 调度方式：基于 `FarmGame.Core.TaskManager.ScheduleRepeating` 注册重复任务
  - 生命周期：
    - `OnEnabled()` → 注册重复任务
    - `OnDisabled()`/`OnDestroy()` → 取消注册，避免残留回调
- 最小闭环：每次 tick 调用 `SkillModule.Tick(tickInterval)`，让技能冷却通过低频调度刷新

**验收标准**：
- 正常运行时冷却递减正常
- 禁用 NPC 后停止调度
- 切场景或销毁对象后无悬挂回调
- tick 周期选择合理（建议 0.2s 或 0.5s），不带来额外性能负担
- 评估现有逐帧逻辑，可迁移的优先迁移到本模块（不扩大版本范围）

---

## 二、不纳入范围（4 个明确排除）

### 1. ECS（Entity Component System）

**排除原因**：
- 项目已有成熟的 `ActorSystem` 架构，重构为 ECS 成本过高
- v1.1 目标是轻量扩展，非架构重写
- ECS 超出当前版本需求

**约束**：
- 所有新增模块必须继承 `ActorModule`
- 不引入任何 ECS 相关库或模式

---

### 2. 行为树（Behavior Tree）

**排除原因**：
- 当前版本不需要复杂 AI 决策逻辑
- NPC 行为简单，条件选择已足够
- 行为树会增加系统复杂度，违背"轻量"原则

**约束**：
- 不引入行为树编辑器或运行时
- 不设计复杂的状态机转换逻辑
- 行为逻辑保持在简单条件判断范围内

---

### 3. 复杂技能树/技能图

**排除原因**：
- v1.1 只需基础技能触发 + 冷却，不需要技能升级、前置条件、组合技等复杂系统
- 技能树/图会引入大量 UI 编辑器工作，超出轻量范围

**约束**：
- 技能数据结构保持扁平（SO + 简单字段）
- 不设计技能依赖、技能连招、技能升级路径
- 不制作技能图可视化编辑器

---

### 4. 条件 DSL 编辑器（Domain Specific Language）

**排除原因**：
- 当前版本对话选择规则已足够简单（首次/重复 + 好感度）
- DSL 编辑器会大幅增加开发和维护成本
- 违背"不做重量级框架"的约束

**约束**：
- 对话选择逻辑硬编码在 `DialogueResolver` 中
- 不实现可扩展的条件语言解析器
- 不提供可视化条件编辑器

---

## 三、试点 NPC

### 选择：商人（shopman）

**NPC 信息**：
- **文件路径**：`/Users/summercards/myfarmunity/Assets/NPCs/shopman.asset`
- **NPC ID**：shopman
- **NPC 名称**：商人
- **已配置功能**：
  - 基础对话（2 条台词）
  - 商店功能（`function = 1`，已配置 `defaultShopCatalog`）
  - 日常作息动画（Sleep/Idle 状态切换）
  - 模型与碰撞体

**选择理由**：

1. **功能完整性**：
   - 已具备对话、商店、日常作息三大核心功能，适合验证 v1.1 所有新能力
   - 能同时验证台词分组、属性读写、技能触发、低频 tick 四个模块

2. **交互频次高**：
   - 商人 NPC 是玩家最常交互的角色之一
   - 高频交互场景最适合测试冷却和调度机制

3. **扩展性强**：
   - 可以为商人设计专属技能（如"折扣祝福"技能）
   - 可以验证属性模块对商店价格的影响（如 `shop_discount_multiplier`）
   - 可以测试好感度分组的台词切换

4. **回归价值高**：
   - 作为功能型 NPC，验证主链路不回退是关键
   - 商店功能若因新模块失效，会直接影响用户体验

**试点验证计划**：
1. 对话模块：配置 `first_meet`、`daily`、`friend` 三组台词，验证首次/重复/好感度切换
2. 属性模块：配置 `shop_discount_multiplier` 属性，验证商店折扣逻辑（需在 ShopModule 中读取）
3. 技能模块：配置"折扣祝福"技能（冷却 60 秒），验证触发 + 冷却闭环
4. Tick 模块：启用低频 tick（0.5s），验证技能冷却递减和回调清理

---

## 四、版本完成判定标准

### 1. 对话主链路正常

**判定内容**：
- 对话打开/关闭正常
- 对话切换（从一个 NPC 切换到另一个）正常
- 对话文本显示正确
- 商店功能按钮可点击

**验收方法**：
- 在场景中与商人 NPC 交互
- 对话期间点击其他 NPC，验证切换正常
- 点击商店按钮，验证商店界面打开
- 回归原有对话逻辑，确保无回退

**阻塞条件**：
- 任何主链路功能回退 → **阻塞版本完成**

---

### 2. 首次/重复台词切换

**判定内容**：
- 首次对话命中 `first_meet` 组
- 重复对话按好感度分组切换（`stranger` → `acquaintance` → `friend` → `close_friend`）
- 无分组资源时回退默认台词

**验收方法**：
- 清空 NPC 记忆（删除 `ActorMemory` 标志）
- 首次交互 → 验证显示 `first_meet` 台词
- 再次交互 → 验证显示 `daily` 或对应好感度分组台词
- 修改好感度 → 验证台词切换正常
- 移除 `dialogueSet` 引用 → 验证回退默认台词

**验收脚本**：
```csharp
// 1. 首次对话
Assert.Contains(merchant.GetComponent<DialogueResolver>().GetCurrentDialogue(), "欢迎来到我的商店");

// 2. 标记首次对话完成
merchant.GetComponent<ActorMemory>().SetFlag("first_meet_dialogue_done", true);

// 3. 重复对话（好感度 stranger）
Assert.Contains(merchant.GetComponent<DialogueResolver>().GetCurrentDialogue(), "今天有什么需要吗？");

// 4. 提升好感度至 friend
merchant.GetComponent<ActorMemory>().SetFriendshipLevel(ActorMemory.FriendshipLevel.Friend);
Assert.Contains(merchant.GetComponent<DialogueResolver>().GetCurrentDialogue(), "老朋友，给你打九折！");
```

---

### 3. 属性可读写

**判定内容**：
- `ActorStatsModule` 能读取配置的属性
- `ActorStatsModule` 能修改属性值
- `ActorStatsModule` 能累加属性值
- 缺省 NPC 不报错

**验收方法**：
- 配置商人的 `baseStats` 包含 `shop_discount_multiplier: 0.9`（九折）
- 读取属性：验证 `Get("shop_discount_multiplier")` 返回 0.9
- 修改属性：`Set("shop_discount_multiplier", 0.8)` → 验证返回 0.8
- 累加属性：`Add("shop_discount_multiplier", 0.1)` → 验证返回 0.9
- 测试缺省 NPC：创建不配置 `baseStats` 的新 NPC → 验证不报错

**验收脚本**：
```csharp
var statsModule = merchant.GetComponent<ActorStatsModule>();

// 1. 读取配置属性
Assert.AreEqual(0.9f, statsModule.Get("shop_discount_multiplier", 1.0f));

// 2. 修改属性
statsModule.Set("shop_discount_multiplier", 0.8f);
Assert.AreEqual(0.8f, statsModule.Get("shop_discount_multiplier", 1.0f));

// 3. 累加属性
statsModule.Add("shop_discount_multiplier", 0.1f);
Assert.AreEqual(0.9f, statsModule.Get("shop_discount_multiplier", 1.0f));

// 4. 读取不存在的属性（返回默认值）
Assert.AreEqual(1.0f, statsModule.Get("nonexistent_key", 1.0f));
```

---

### 4. 技能冷却闭环

**判定内容**：
- 技能首次触发成功
- 技能触发后进入冷却
- 冷却期间不能重复施放
- 冷却结束后可再次施放

**验收方法**：
- 配置商人技能"折扣祝福"（cooldown: 60 秒）
- 首次触发 → 验证 `TryCast("discount_blessing")` 返回 true
- 立即再次触发 → 验证 `TryCast("discount_blessing")` 返回 false
- 等待 60 秒 → 验证 `TryCast("discount_blessing")` 返回 true
- 验证 `IsReady("discount_blessing")` 与冷却状态一致

**验收脚本**：
```csharp
var skillModule = merchant.GetComponent<SkillModule>();

// 1. 首次施放成功
Assert.IsTrue(skillModule.TryCast("discount_blessing"));

// 2. 冷却期间施放失败
Assert.IsFalse(skillModule.TryCast("discount_blessing"));

// 3. 技能未就绪
Assert.IsFalse(skillModule.IsReady("discount_blessing"));

// 4. 等待冷却结束
yield return new WaitForSeconds(60f);

// 5. 冷却结束后可再次施放
Assert.IsTrue(skillModule.IsReady("discount_blessing"));
Assert.IsTrue(skillModule.TryCast("discount_blessing"));
```

---

### 5. 低频 tick 无残留回调

**判定内容**：
- 低频 tick 驱动技能冷却刷新
- 禁用 NPC 后停止调度
- 切场景或销毁对象后无悬挂回调

**验收方法**：
- 启用 `ActorTickModule`（tickInterval: 0.5s）
- 验证技能冷却每 0.5s 递减一次
- 禁用 NPC → 验证冷却停止递减
- 销毁 NPC → 验证 `TaskManager` 中无残留任务
- 切换场景 → 验证前场景 NPC 的 tick 已取消

**验收脚本**：
```csharp
var tickModule = merchant.GetComponent<ActorTickModule>();
var skillModule = merchant.GetComponent<SkillModule>();

// 1. 触发技能，进入冷却
skillModule.TryCast("discount_blessing");
float initialCooldown = skillModule.GetRemainingCooldown("discount_blessing");

// 2. 等待 1 个 tick（0.5s）
yield return new WaitForSeconds(0.5f);
float afterTick = skillModule.GetRemainingCooldown("discount_blessing");
Assert.Less(afterTick, initialCooldown); // 冷却应递减

// 3. 禁用 NPC
merchant.SetActive(false);
yield return new WaitForSeconds(1f);
float afterDisable = skillModule.GetRemainingCooldown("discount_blessing");
Assert.AreEqual(afterTick, afterDisable); // 冷却停止递减

// 4. 销毁 NPC
Destroy(merchant.gameObject);
yield return null;

// 5. 验证 TaskManager 中无残留任务（通过日志或内部 API）
// 这里需要依赖 TaskManager 提供的检查接口
```

---

## 五、开发约束与最佳实践

### 数据结构规范

1. **优先使用 List<>**：
   - 避免使用 Dictionary 序列化（Unity 序列化支持有限）
   - 使用 `List<T>` 替代，运行时根据需要转换为 Dictionary

2. **ScriptableObject 引用**：
   - 所有复杂数据结构（台词分组、技能定义）都使用 SO
   - 便于编辑器配置和资产管理

3. **可选字段策略**：
   - 新增字段必须标记为可选（`[Tooltip("(可选)")]`）
   - 空值必须有明确的 fallback 行为

### 模块化规范

1. **继承 ActorModule**：
   - 所有新模块必须继承 `ActorModule`
   - 实现 `OnEnabled()` 和 `OnDisabled()` 生命周期

2. **模块可选挂载**：
   - 缺少模块时不影响主链路（对话/交互/商店）
   - 模块内部必须处理 null 检查

3. **生命周期管理**：
   - 调度任务必须在 `OnDisabled()`/`OnDestroy()` 取消
   - 避免场景切换后的残留回调

### 编辑器构建规范

1. **NPCPrefabBuilder 扩展**：
   - 新增模块遵循"有数据则挂，无数据则不挂"原则
   - 保持与旧版本的兼容性

2. **数据一致性校验**：
   - `Phase04NpcValidator` 必须检查"配置了数据但没挂模块"的情况
   - 校验输出必须清晰（缺什么、在哪、怎么补）

### 性能约束

1. **避免新增 Update()**：
   - 周期逻辑统一使用 `ActorTickModule` 或 `TaskManager`
   - 不允许新增 `void Update()` 方法

2. **低频 tick 选择**：
   - 默认 tickInterval 建议 0.5s
   - 特殊场景可调整为 0.2s，但不低于 0.1s

3. **回调清理**：
   - 禁用/销毁时必须取消所有调度
   - 使用 `TaskManager.Cancel()` 确保无残留

---

## 六、风险与回滚策略

### 风险识别

1. **主链路回退风险**：
   - 新模块破坏现有对话/交互/商店逻辑
   - **缓解措施**：每完成一个模块，立即回归主链路

2. **回调残留风险**：
   - 场景切换后 TaskManager 仍有旧 NPC 的 tick 任务
   - **缓解措施**：强制所有模块在 `OnDestroy()` 取消调度

3. **兼容性风险**：
   - 旧 NPC 不配置新字段时报错
   - **缓解措施**：所有新字段必须支持 null，且有 fallback 逻辑

### 回滚策略

1. **代码回滚**：
   - 使用 Git 版本控制，每个模块完成后打 tag
   - 发现问题可快速回滚到上一个稳定版本

2. **数据回滚**：
   - NPCDefinition 新增字段不影响旧资产（Unity 默认为 null）
   - 无需修改现有 NPCDefinition 文件

3. **Prefab 回滚**：
   - NPCPrefabBuilder 保留旧版本构建逻辑
   - 可选择不启用新模块（不配置新字段）

---

## 七、文档引用

本边界确认文档引用以下文档：

- **目标文档**：`/Users/summercards/myfarmunity/Assets/.task-manager/v1-1/TargetDocument.md`
- **施工文档**：`/Users/summercards/myfarmunity/Assets/.task-manager/v1-1/ImplementationDocument.md`
- **任务列表**：`/Users/summercards/myfarmunity/Assets/.task-manager/v1-1/Tasks.md`

所有后续任务必须遵循本边界约束，不得随意扩大范围或引入 excluded 的内容。

---

**文档状态**：✅ 已确认
**确认时间**：2026-04-07
**约束级别**：强制执行
