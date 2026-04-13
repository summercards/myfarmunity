# 我的农场 v1.1 NPC 系统使用指南

## 目录
1. [系统概述](#系统概述)
2. [新特性说明](#新特性说明)
3. [如何使用新特性](#如何使用新特性)
4. [常见问题](#常见问题)

---

## 系统概述

### 什么是 NPC 系统？

v1.1 版本完善了 NPC 系统，使每个 NPC 成为可独立调度的模块化个体。简单来说：

**一个 NPC = 一个 Actor + 一组功能模块**

- **Actor**：NPC 的核心，包含身份、记忆、AI 大脑等
- **功能模块**：台词、属性、技能等可组合的能力

### 核心优势

| 优势 | 说明 |
|------|------|
| **模块化** | 台词、属性、技能等能力可以灵活组合 |
| **轻量级** | 不引入重量级框架，保持简单高效 |
| **可扩展** | 新增功能不影响现有 NPC，支持增量迁移 |
| **独立调度** | NPC 可以低频刷新，不占用太多性能 |

---

## 新特性说明

### 1. 台词分组

#### 什么是台词分组？

台词分组是指把 NPC 的台词分成不同的组，根据不同情况自动选择合适的台词。

#### 支持的分组

| 分组名称 | 触发条件 | 示例 |
|---------|---------|------|
| `first_meet` | 首次对话 | "你好，欢迎来到我的商店！" |
| `daily` | 重复对话（每天不同） | "今天需要什么？" |
| `friend` | 好感度 >= 50 | "老朋友，来看看新商品吗？" |
| `close_friend` | 好感度 >= 80 | "最好的朋友，给你特别折扣！" |

#### 特点

- ✅ **自动切换**：根据好感度自动选择合适的台词
- ✅ **保持兼容**：如果不配置台词分组，仍使用旧的默认台词列表
- ✅ **轻量设计**：不支持复杂条件树，保持简单

---

### 2. 属性系统

#### 什么是属性系统？

属性系统允许 NPC 拥有自定义属性（如好感度、生命值、幸运值等），可以在游戏中读写。

#### 支持的属性

| 属性名称 | 类型 | 说明 |
|---------|------|------|
| `friendship_gain_multiplier` | float | 好感度加成倍率（如 1.5 表示好感度提升 1.5 倍） |
| `max_hp` | float | 最大生命值 |
| `luck` | float | 幸运值 |
| `speed` | float | 移动速度 |
| `smithing_skill` | float | 锻造技能等级 |
| ... | ... | 可自定义任意属性 |

#### 特点

- ✅ **自定义**：可以添加任意属性
- ✅ **易读写**：简单的 Get/Set 接口
- ✅ **可选配置**：不配置属性也可以正常运行
- ✅ **轻量设计**：使用简单的键值对结构

---

### 3. 技能系统

#### 什么是技能系统？

技能系统允许 NPC 拥有特殊能力（如折扣祝福、特价优惠、治愈术等），技能有冷却时间。

#### 支持的技能

| 技能 ID | 显示名称 | 冷却时间 | 说明 |
|---------|---------|---------|------|
| `discount_blessing` | 折扣祝福 | 60 秒 | 给玩家提供折扣（如 8 折） |
| `special_offer` | 特价优惠 | 120 秒 | 特价商品（如 50 金币） |
| `heal` | 治愈术 | 30 秒 | 恢复玩家生命值 |
| ... | ... | ... | 可自定义任意技能 |

#### 技能参数

每个技能可以自定义参数，例如：

- `discount_rate`：折扣率（如 0.2 表示 8 折）
- `special_price`：特价价格（如 50 金币）
- `heal_amount`：治愈量（如 100 生命值）

#### 特点

- ✅ **冷却机制**：技能施放后进入冷却，冷却期间无法再次施放
- ✅ **数据驱动**：使用 ScriptableObject 配置，无需修改代码
- ✅ **自定义参数**：每个技能可以有不同的参数
- ✅ **可选配置**：不配置技能也可以正常运行
- ✅ **轻量设计**：不支持复杂技能树，保持简单

---

### 4. 低频调度

#### 什么是低频调度？

低频调度是指 NPC 可以以较低的频率（如每秒 1 次）刷新技能冷却、日常作息、状态等逻辑，而不是每帧（每秒 60 次）都刷新。

#### 为什么需要低频调度？

**性能优化**：

| 方式 | 调用频率 | 性能影响 |
|------|---------|---------|
| Update() | 每秒 60 次 | 高 |
| 低频 Tick（默认 1 秒） | 每秒 1 次 | 低（减少 60 倍） |

**特点**：

- ✅ **性能优化**：减少 60 倍的调用频率
- ✅ **统一管理**：所有周期逻辑由 TaskManager 统一管理
- ✅ **可配置**：可以调整 Tick 周期（默认 1 秒）
- ✅ **可选启用**：不启用低频调度也可以正常运行
- ✅ **安全可靠**：禁用或销毁 NPC 时自动取消调度，无残留回调

---

## 如何使用新特性

### 1. 台词分组使用指南

#### 步骤 1：创建台词分组资产

1. 在 Project 视图中右键 → Create → Farm → Dialogue Set
2. 命名为 `ShopmanDialogueSet`
3. 在 Inspector 中配置台词分组

#### 步骤 2：配置台词分组

在 `ShopmanDialogueSet` 中添加以下分组：

```yaml
first_meet:
  - "你好，欢迎来到我的商店！"
  - "有什么需要帮助的吗？"

daily:
  - "今天需要什么？"
  - "新到的商品很受欢迎！"

friend:
  - "老朋友，来看看新商品吗？"
  - "给你个折扣！"

close_friend:
  - "最好的朋友，给你特别折扣！"
  - "随便挑，都算你 8 折！"
```

#### 步骤 3：关联台词分组到 NPC

1. 找到 NPC 的 `NPCDefinition` 资产
2. 将 `ShopmanDialogueSet` 拖拽到 `Dialogue Set` 字段
3. 保存资产

#### 步骤 4：构建 NPC Prefab

1. 选中 NPC 的 `NPCDefinition` 资产
2. 点击菜单 → 工具 → NPC → 构建选中 NPC 预制体
3. 等待构建完成

#### 步骤 5：测试台词分组

1. 运行游戏
2. 与 NPC 对话（首次）：应该触发 `first_meet` 分组
3. 再次对话（重复）：应该触发 `daily` 分组
4. 提升好感度到 50：应该触发 `friend` 分组
5. 提升好感度到 80：应该触发 `close_friend` 分组

---

### 2. 属性系统使用指南

#### 步骤 1：配置属性

1. 找到 NPC 的 `NPCDefinition` 资产
2. 在 `Base Stats` 列表中添加属性：

| Key | Value | 说明 |
|-----|-------|------|
| `friendship_gain_multiplier` | 1.5 | 好感度加成 1.5 倍 |
| `max_hp` | 100 | 最大生命值 100 |
| `luck` | 50 | 幸运值 50 |

3. 保存资产

#### 步骤 2：构建 NPC Prefab

1. 选中 NPC 的 `NPCDefinition` 资产
2. 点击菜单 → 工具 → NPC → 构建选中 NPC 预制体
3. 等待构建完成

#### 步骤 3：测试属性读写

在游戏中通过 Console 测试：

```csharp
// 获取 NPC
GameObject shopman = GameObject.Find("shopman");
ActorStatsModule statsModule = shopman.GetComponent<ActorStatsModule>();

// 读取属性
float friendshipMultiplier = statsModule.GetStat("friendship_gain_multiplier");
Debug.Log($"好感度加成倍率: {friendshipMultiplier}");

// 修改属性
statsModule.SetStat("friendship_gain_multiplier", 2.0f);
Debug.Log($"修改后好感度加成倍率: {statsModule.GetStat("friendship_gain_multiplier")}");

// 添加新属性
statsModule.AddStat("new_attribute", 100);
Debug.Log($"新属性值: {statsModule.GetStat("new_attribute")}");

// 删除属性
statsModule.RemoveStat("new_attribute");
Debug.Log($"删除后的属性值: {statsModule.GetStat("new_attribute", 0)}");
```

---

### 3. 技能系统使用指南

#### 步骤 1：创建技能定义资产

1. 在 Project 视图中右键 → Create → Farm → Skill Definition
2. 命名为 `DiscountBlessing`
3. 在 Inspector 中配置技能参数

#### 步骤 2：配置技能参数

在 `DiscountBlessing` 中配置：

| 字段 | 值 | 说明 |
|------|-----|------|
| `Skill ID` | `discount_blessing` | 技能唯一 ID |
| `Display Name` | `折扣祝福` | 显示名称 |
| `Description` | `给玩家提供 8 折折扣` | 描述 |
| `Cooldown Seconds` | `60` | 冷却时间 60 秒 |
| `Parameters` | `discount_rate: 0.2` | 折扣率 20% |

#### 步骤 3：创建更多技能（可选）

类似地创建 `SpecialOffer` 技能：

| 字段 | 值 | 说明 |
|------|-----|------|
| `Skill ID` | `special_offer` | 技能唯一 ID |
| `Display Name` | `特价优惠` | 显示名称 |
| `Description` | `特价商品 50 金币` | 描述 |
| `Cooldown Seconds` | `120` | 冷却时间 120 秒 |
| `Parameters` | `special_price: 50` | 特价价格 50 金币 |

#### 步骤 4：关联技能到 NPC

1. 找到 NPC 的 `NPCDefinition` 资产
2. 将 `DiscountBlessing` 和 `SpecialOffer` 拖拽到 `Skills` 列表
3. 保存资产

#### 步骤 5：构建 NPC Prefab

1. 选中 NPC 的 `NPCDefinition` 资产
2. 点击菜单 → 工具 → NPC → 构建选中 NPC 预制体
3. 等待构建完成

#### 步骤 6：测试技能触发

在游戏中通过 Console 测试：

```csharp
// 获取 NPC
GameObject shopman = GameObject.Find("shopman");
SkillModule skillModule = shopman.GetComponent<SkillModule>();

// 首次施放（预期成功）
bool result1 = skillModule.TryCast("discount_blessing");
Debug.Log($"首次施放结果: {result1}");  // 应该输出: True

// 检查冷却状态
bool isReady1 = skillModule.IsReady("discount_blessing");
Debug.Log($"冷却状态: {isReady1}");  // 应该输出: False

// 等待 10 秒（模拟冷却）
// 在冷却期间尝试施放（预期失败）
bool result2 = skillModule.TryCast("discount_blessing");
Debug.Log($"冷却期间施放结果: {result2}");  // 应该输出: False

// 等待冷却结束（60 秒）
// 冷却结束后施放（预期成功）
bool result3 = skillModule.TryCast("discount_blessing");
Debug.Log($"冷却结束后施放结果: {result3}");  // 应该输出: True
```

---

### 4. 低频调度使用指南

#### 步骤 1：配置低频调度

1. 找到 NPC 的 `NPCDefinition` 资产
2. 配置以下字段：

| 字段 | 值 | 说明 |
|------|-----|------|
| `Enable Low Frequency Tick` | ✅ | 启用低频调度 |
| `Tick Interval` | `1.0` | Tick 周期 1 秒 |
| `Use Unscaled Time` | ❌ | 使用缩放时间（受 Time.timeScale 影响） |

3. 保存资产

**注意**：低频调度需要 NPC 配置了技能才能生效。

#### 步骤 2：构建 NPC Prefab

1. 选中 NPC 的 `NPCDefinition` 资产
2. 点击菜单 → 工具 → NPC → 构建选中 NPC 预制体
3. 等待构建完成

#### 步骤 3：测试低频调度

在游戏中通过 Console 测试：

```csharp
// 获取 NPC
GameObject shopman = GameObject.Find("shopman");
ActorTickModule tickModule = shopman.GetComponent<ActorTickModule>();

// 检查模块状态
Debug.Log($"模块启用: {tickModule.IsEnabled}");  // 应该输出: True
Debug.Log($"正在调度: {tickModule.IsScheduling}");  // 应该输出: True
Debug.Log($"Tick 周期: {tickModule.TickInterval}");  // 应该输出: 1
Debug.Log($"Tick 模式: {tickModule.TickMode}");  // 应该输出: 缩放时间

// 等待几秒，观察日志（应该看到 Tick 触发的日志）
// 禁用模块
tickModule.Disable();

// 检查调度是否停止
Debug.Log($"禁用后正在调度: {tickModule.IsScheduling}");  // 应该输出: False

// 重新启用
tickModule.Enable();

// 检查调度是否恢复
Debug.Log($"重新启用后正在调度: {tickModule.IsScheduling}");  // 应该输出: True
```

---

## 常见问题

### 1. 旧 NPC 会受影响吗？

**答**：不会。v1.1 的所有新特性都是可选的，旧 NPC 不配置新字段时仍可正常运行。

| 旧 NPC 配置 | 运行结果 |
|------------|---------|
| 不配置台词分组 | 使用旧的默认台词列表 |
| 不配置属性 | 不挂载属性模块 |
| 不配置技能 | 不挂载技能模块 |
| 不启用低频调度 | 不挂载低频调度模块 |

---

### 2. 如何启用新特性？

**答**：只需在 NPC 的 `NPCDefinition` 资产中配置相应字段，然后重新构建 Prefab。

**步骤**：
1. 打开 NPC 的 `NPCDefinition` 资产
2. 配置新特性（台词分组、属性、技能、低频调度）
3. 点击菜单 → 工具 → NPC → 构建选中 NPC 预制体
4. 完成

---

### 3. 低频调度会影响性能吗？

**答**：不会。低频调度反而会提升性能。

**性能对比**：

| 方式 | 调用频率 | 性能影响 |
|------|---------|---------|
| Update() | 每秒 60 次 | 高 |
| 低频 Tick（默认 1 秒） | 每秒 1 次 | 低（减少 60 倍） |

---

### 4. 技能冷却会在场景切换后丢失吗？

**答**：会。这是设计决策。

**原因**：
- 技能冷却表不序列化（避免数据污染）
- 场景切换时冷却会重置

**影响**：
- 用户可能会在场景切换后立即施放技能
- 这是简单且安全的设计

**如果需要跨场景保持冷却**：
- 需要额外的开发工作
- 在场景切换时保存和恢复冷却状态

---

### 5. 可以添加自定义技能吗？

**答**：可以。

**步骤**：
1. 在 Project 视图中右键 → Create → Farm → Skill Definition
2. 配置技能参数（技能 ID、冷却时间、自定义参数）
3. 关联到 NPC 的 `NPCDefinition` 资产
4. 重新构建 NPC Prefab

---

### 6. 可以添加自定义属性吗？

**答**：可以。

**步骤**：
1. 找到 NPC 的 `NPCDefinition` 资产
2. 在 `Base Stats` 列表中添加属性（Key + Value）
3. 保存资产
4. 重新构建 NPC Prefab

---

### 7. 台词分组支持复杂条件吗？

**答**：不支持。v1.1 的台词分组只支持简单规则（首次/重复、好感度分级）。

**支持的规则**：
- ✅ 首次会话 → `first_meet`
- ✅ 重复对话 → `daily`
- ✅ 好感度 >= 50 → `friend`
- ✅ 好感度 >= 80 → `close_friend`

**不支持**：
- ❌ 复杂条件树
- ❌ DSL（领域特定语言）
- ❌ 时间相关的条件（如白天/夜晚）

**原因**：保持轻量设计，避免引入重量级框架。

---

### 8. 如何验证配置是否正确？

**答**：使用编辑器校验器。

**步骤**：
1. 点击菜单 → 工具 → NPC → Phase04 → 仅验证 (Validate Only)
2. 查看验证结果
3. 如果有错误，按照错误提示修复

**常见的错误**：
- ❌ 配置了台词分组但预制体缺少 `DialogueResolver` 组件
- ❌ 配置了属性但预制体缺少 `ActorStatsModule` 组件
- ❌ 配置了技能但预制体缺少 `SkillModule` 组件
- ❌ 启用了低频调度但预制体缺少 `ActorTickModule` 组件

**修复方法**：
1. 选中 NPC 的 `NPCDefinition` 资产
2. 点击菜单 → 工具 → NPC → 构建选中 NPC 预制体
3. 重新验证

---

### 9. 如何查看 NPC 的所有信息？

**答**：在游戏中通过 Console 查询。

**示例**：

```csharp
// 获取 NPC
GameObject npc = GameObject.Find("shopman");

// 检查核心组件
Actor actor = npc.GetComponent<Actor>();
ActorIdentity identity = npc.GetComponent<ActorIdentity>();
ActorMemory memory = npc.GetComponent<ActorMemory>();
ActorInteraction interaction = npc.GetComponent<ActorInteraction>();

Debug.Log($"NPC ID: {identity.Id}");
Debug.Log($"NPC Name: {identity.Name}");
Debug.Log($"NPC Function: {identity.Function}");

// 检查台词分组
DialogueResolver resolver = npc.GetComponent<DialogueResolver>();
if (resolver != null && resolver.dialogueSet != null)
{
    Debug.Log($"Dialogue Set: {resolver.dialogueSet.name}");
}

// 检查属性
ActorStatsModule statsModule = npc.GetComponent<ActorStatsModule>();
if (statsModule != null)
{
    float friendshipMultiplier = statsModule.GetStat("friendship_gain_multiplier");
    Debug.Log($"好感度加成倍率: {friendshipMultiplier}");
}

// 检查技能
SkillModule skillModule = npc.GetComponent<SkillModule>();
if (skillModule != null)
{
    int skillCount = skillModule.GetAllSkills().Count;
    Debug.Log($"技能数量: {skillCount}");
}

// 检查低频调度
ActorTickModule tickModule = npc.GetComponent<ActorTickModule>();
if (tickModule != null)
{
    Debug.Log($"低频调度: {tickModule.IsScheduling}");
    Debug.Log($"Tick 周期: {tickModule.TickInterval}");
}
```

---

### 10. 如何调试 NPC 问题？

**答**：按照以下步骤调试：

**步骤 1**：检查 NPC Prefab 组件
- 查看 NPC Prefab 是否包含所有必需组件
- 检查组件配置是否正确

**步骤 2**：使用编辑器校验器
- 点击菜单 → 工具 → NPC → Phase04 → 仅验证
- 查看验证结果，修复错误

**步骤 3**：在游戏中查看日志
- 运行游戏
- 查看 Console 日志
- 查找错误或警告

**步骤 4**：使用 Console 查询 NPC 信息
- 使用上面的代码示例查询 NPC 的所有信息
- 检查配置是否正确

**步骤 5**：重新构建 NPC Prefab
- 选中 NPC 的 `NPCDefinition` 资产
- 点击菜单 → 工具 → NPC → 构建选中 NPC 预制体
- 重新测试

---

## 总结

### 核心特性

| 特性 | 说明 | 是否可选 |
|------|------|---------|
| 台词分组 | 支持首次/重复/好感度分级等分组 | ✅ 可选 |
| 属性系统 | 支持自定义属性（如好感度、生命值） | ✅ 可选 |
| 技能系统 | 支持技能触发和冷却 | ✅ 可选 |
| 低频调度 | 支持低频刷新，优化性能 | ✅ 可选 |

### 使用流程

1. **配置 NPCDefinition**：配置台词分组、属性、技能、低频调度
2. **构建 NPC Prefab**：使用 NPCPrefabBuilder 构建预制体
3. **验证配置**：使用编辑器校验器验证配置
4. **测试功能**：在游戏中测试 NPC 功能
5. **调试问题**：使用 Console 查询和调试

### 关键优势

- ✅ **模块化**：台词、属性、技能等能力可以灵活组合
- ✅ **轻量级**：不引入重量级框架，保持简单高效
- ✅ **可扩展**：新增功能不影响现有 NPC，支持增量迁移
- ✅ **独立调度**：NPC 可以低频刷新，不占用太多性能
- ✅ **向后兼容**：旧 NPC 不配置新字段时仍可正常运行

---

**文档版本**：v1.0
**创建日期**：2026-04-08
**适用版本**：我的农场 v1.1
