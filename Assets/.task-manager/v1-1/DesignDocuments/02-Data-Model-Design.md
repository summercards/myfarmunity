# 我的农场 v1.1 数据模型设计文档

> 文档版本：v1.0
> 创建日期：2026-04-08
> 对应任务：02 - 设计台词分组与 NPC 扩展数据结构
> 设计者：杰西卡 🎀

---

## 一、设计原则

### 1.1 核心原则

- **轻量化**：不引入复杂框架，使用 `List<>` + ScriptableObject 引用
- **可序列化**：所有数据结构必须支持 Inspector 编辑和 YAML 序列化
- **可扩展**：优先使用 `List<>` 而非 Dictionary（避免序列化陷阱）
- **向后兼容**：旧 NPC 不配置新字段时仍可运行，不报错、不丢失原有对话和交互能力
- **可选挂载**：新增模块必须可选，缺省时不影响对话/交互主链路

### 1.2 数据结构选择依据

| 结构类型 | 适用场景 | 序列化 | 扩展性 | 性能 |
|---------|---------|--------|--------|------|
| `List<T>` | 固定数量的小列表、需要序列化的数据 | ✅ 优秀 | ✅ 良好 | ✅ 良好 |
| `Dictionary<K,V>` | 大量查找、需要快速访问 | ❌ 不支持直接序列化 | ✅ 优秀 | ✅ 优秀 |
| ScriptableObject | 共享数据、配置资产 | ✅ 优秀 | ✅ 优秀 | ✅ 优秀 |

**结论**：v1.1 所有新增数据结构统一使用 `List<>` + ScriptableObject 引用。

---

## 二、台词分组数据结构

### 2.1 DialogueGroup（台词分组条目）

**文件路径**：`Assets/Scripts/ActorSystem/Dialogue/DialogueSetSO.cs`

**代码定义**：
```csharp
[System.Serializable]
public class DialogueGroup
{
    [Header("分组标识（用于选择规则）")]
    [Tooltip("预定义分组：first_meet, daily, stranger, acquaintance, friend, close_friend")]
    public string key = "daily";

    [Header("台词列表")]
    [Tooltip("该分组的所有台词，运行时随机选择一条")]
    public List<string> lines = new List<string>();
}
```

**字段说明**：

| 字段 | 类型 | 默认值 | 用途 | 约束 |
|-----|------|--------|------|------|
| `key` | string | "daily" | 分组标识符 | 必须是预定义值之一 |
| `lines` | List<string> | 空列表 | 该分组的台词 | 空列表时被跳过 |

**预定义分组类型**：

| 分组 key | 触发条件 | 优先级 | 示例 |
|----------|---------|--------|------|
| `first_meet` | 首次会面（`ActorMemory.HasCompletedFirstDialogue == false`） | 1 | "初次见面，欢迎来到我的农场。" |
| `daily` | 日常对话（默认回退） | 4 | "今天天气不错。" |
| `stranger` | 好感度 < 20 | 2 | "你好，陌生人。" |
| `acquaintance` | 20 ≤ 好感度 < 50 | 2 | "你好，我们又见面了。" |
| `friend` | 50 ≤ 好感度 < 80 | 2 | "你好呀，老朋友！" |
| `close_friend` | 好感度 ≥ 80 | 2 | "亲爱的，很高兴见到你！" |

**边界情况处理**：
- ✅ 空 `lines` 列表 → 被跳过，继续查找下一个优先级分组
- ✅ 缺失 `key` 字段 → 默认为 "daily"
- ✅ 重复触发首次会面 → `ActorMemory.HasCompletedFirstDialogue` 状态控制
- ✅ 非预定义 key → 被跳过，继续查找下一个优先级分组

---

### 2.2 DialogueSetSO（台词分组资产）

**文件路径**：`Assets/Scripts/ActorSystem/Dialogue/DialogueSetSO.cs`

**代码定义**：
```csharp
[CreateAssetMenu(fileName = "DialogueSet_", menuName = "Farm/Dialogue Set", order = 20)]
public class DialogueSetSO : ScriptableObject
{
    [Header("台词分组列表")]
    [Tooltip("包含多个分组的台词集合，每个分组有独立的选择规则")]
    public List<DialogueGroup> groups = new List<DialogueGroup>();
}
```

**字段说明**：

| 字段 | 类型 | 默认值 | 用途 | 约束 |
|-----|------|--------|------|------|
| `groups` | List<DialogueGroup> | 空列表 | 所有的台词分组 | 支持任意数量分组 |

**使用示例**：

**场景 1**：商店 NPC 的台词配置
```yaml
groups:
  - key: "first_meet"
    lines:
      - "欢迎来到商店！这里有什么可以帮您的吗？"
      - "您好！我是这里的店主。"
  - key: "daily"
    lines:
      - "欢迎光临！今天需要点什么？"
      - "来点新货了吗？"
  - key: "friend"
    lines:
      - "老朋友来了！今天给你打八折。"
      - "好久不见，来看看新商品吗？"
```

**场景 2**：村民 NPC 的简单配置
```yaml
groups:
  - key: "daily"
    lines:
      - "你好呀。"
      - "今天天气不错。"
```

**缺省行为**：
- ❌ 不配置 `dialogueSet` → 回退到 `ActorIdentity.GetDefaultDialog()`（旧逻辑）
- ✅ 配置了 `dialogueSet` 但所有分组为空 → 回退到 `ActorIdentity.GetDefaultDialog()`
- ✅ 配置了 `dialogueSet` 且有有效分组 → 按优先级选择分组台词

**编辑器创建路径**：
1. 在 Project 窗口右键 → `Create` → `Farm` → `Dialogue Set`
2. 命名规则：`DialogueSet_<NPC ID>`（例如 `DialogueSet_shopman`）
3. 填写 `groups` 列表，每个分组定义 `key` 和 `lines`

---

### 2.3 DialogueResolver 分组选择逻辑

**文件路径**：`Assets/Scripts/ActorSystem/DialogueResolver.cs`

**新增字段**：
```csharp
[Header("台词分组（v1.1 扩展）")]
[Tooltip("可选的台词分组资产，支持首次会面、好感度分级等分组规则")]
public DialogueSetSO dialogueSet;
```

**选择规则（优先级从高到低）**：

```
1. 若 dialogueSet 不为空且 ActorMemory.HasCompletedFirstDialogue == false
   → 查找 key="first_meet" 的分组
   → 若存在且 lines 非空，返回随机台词
   → 否则继续下一步

2. 若 dialogueSet 不为空
   → 按好感度查找对应分组（stranger/acquaintance/friend/close_friend）
   → 若存在且 lines 非空，返回随机台词
   → 否则继续下一步

3. 若 dialogueSet 不为空
   → 查找 key="daily" 的分组
   → 若存在且 lines 非空，返回随机台词
   → 否则继续下一步

4. 回退到旧逻辑
   → 调用 ActorIdentity.GetDefaultDialog()
```

**好感度分组映射**（基于 `ActorMemory.Friendship`）：

| 好感度范围 | 分组 key | 说明 |
|-----------|---------|------|
| < 20 | "stranger" | 陌生人 |
| 20 ≤ X < 50 | "acquaintance" | 熟人 |
| 50 ≤ X < 80 | "friend" | 朋友 |
| ≥ 80 | "close_friend" | 亲密朋友 |

**代码实现伪码**：
```csharp
public string GetCurrentDialogue()
{
    // 1. 首次会面优先
    if (dialogueSet != null && !actorMemory.HasCompletedFirstDialogue)
    {
        var group = dialogueSet.groups.Find(g => g.key == "first_meet");
        if (group != null && group.lines.Count > 0)
        {
            return group.lines[Random.Range(0, group.lines.Count)];
        }
    }

    // 2. 好感度分级
    if (dialogueSet != null)
    {
        string friendshipKey = GetFriendshipKey(actorMemory.Friendship);
        var group = dialogueSet.groups.Find(g => g.key == friendshipKey);
        if (group != null && group.lines.Count > 0)
        {
            return group.lines[Random.Range(0, group.lines.Count)];
        }
    }

    // 3. 日常回退
    if (dialogueSet != null)
    {
        var group = dialogueSet.groups.Find(g => g.key == "daily");
        if (group != null && group.lines.Count > 0)
        {
            return group.lines[Random.Range(0, group.lines.Count)];
        }
    }

    // 4. 旧逻辑回退
    return actorIdentity.GetDefaultDialog();
}

private string GetFriendshipKey(float friendship)
{
    if (friendship >= 80) return "close_friend";
    if (friendship >= 50) return "friend";
    if (friendship >= 20) return "acquaintance";
    return "stranger";
}
```

**边界情况处理**：
- ✅ `dialogueSet` 为 null → 直接跳到步骤 4（旧逻辑）
- ✅ `dialogueSet` 存在但 `groups` 为空列表 → 跳到步骤 4（旧逻辑）
- ✅ 找不到匹配的 key → 继续下一步骤
- ✅ 找到 key 但 `lines` 为空列表 → 继续下一步骤
- ✅ 随机索引越界（lines 被外部清空） → 跳到下一步骤

**兼容旧数据**：
- ❌ 不配置 `dialogueSet` → 完全走旧逻辑，无任何影响
- ✅ 配置 `dialogueSet` → 优先使用新逻辑，失败则回退旧逻辑

---

## 三、属性模块数据结构

### 3.1 StatEntry（属性条目）

**文件路径**：`Assets/Scripts/ActorSystem/ActorStatsModule.cs`

**代码定义**：
```csharp
[System.Serializable]
public class StatEntry
{
    [Header("属性名（键）")]
    [Tooltip("属性的唯一标识符，用于 Get/Set/Add 接口")]
    public string key = "";

    [Header("属性值")]
    [Tooltip("属性的数值，支持整数、浮点数等")]
    public float value = 0f;
}
```

**字段说明**：

| 字段 | 类型 | 默认值 | 用途 | 约束 |
|-----|------|--------|------|------|
| `key` | string | "" | 属性名 | 不能为空字符串 |
| `value` | float | 0f | 属性值 | 任意浮点数 |

**常用属性预定义**：

| 属性 key | 用途 | 示例值 |
|----------|------|--------|
| `friendship_gain_multiplier` | 好感度增益倍率 | 1.5f（50% 增益） |
| `shop_discount_rate` | 商店折扣率 | 0.8f（20% 折扣） |
| `interaction_range` | 交互范围 | 2.5f |
| `move_speed` | 移动速度 | 1.0f |
| `dialogue_speed` | 对话速度 | 1.2f |

**边界情况处理**：
- ✅ `key` 为空字符串 → 该条目被忽略，不参与逻辑
- ✅ 重复的 `key` → 后面的条目覆盖前面的（List 顺序）

---

### 3.2 ActorStatsModule（属性模块）

**文件路径**：`Assets/Scripts/ActorSystem/ActorStatsModule.cs`

**代码定义**：
```csharp
public class ActorStatsModule : ActorModule
{
    [Header("基础属性列表（可序列化）")]
    [Tooltip("NPC 的基础属性配置，运行时可动态修改")]
    public List<StatEntry> baseStats = new List<StatEntry>();

    /// <summary>
    /// 读取属性值，不存在时返回默认值
    /// </summary>
    public float Get(string key, float defaultValue = 0f)
    {
        // 实现：从 baseStats 中查找 key，返回 value
        // 找不到返回 defaultValue
    }

    /// <summary>
    /// 设置属性值，不存在则新增
    /// </summary>
    public void Set(string key, float value)
    {
        // 实现：从 baseStats 中查找 key，更新 value
        // 找不到则新增 StatEntry
    }

    /// <summary>
    /// 累加属性值，不存在则从默认值开始累加
    /// </summary>
    public void Add(string key, float delta)
    {
        // 实现：调用 Get(key)，加上 delta，再调用 Set
    }
}
```

**核心 API 说明**：

| API | 参数 | 返回值 | 说明 |
|-----|------|--------|------|
| `Get(string key, float defaultValue)` | key: 属性名<br>defaultValue: 默认值 | float | 读取属性，不存在返回默认值 |
| `Set(string key, float value)` | key: 属性名<br>value: 新值 | void | 设置属性，不存在则新增 |
| `Add(string key, float delta)` | key: 属性名<br>delta: 增量 | void | 累加属性，不存在则从 0 开始 |

**使用示例**：

**场景 1**：读取商店折扣率
```csharp
// 假设 shopman 配置了 friendship_gain_multiplier = 1.5f
ActorStatsModule stats = GetComponent<ActorStatsModule>();
if (stats != null)
{
    float discount = stats.Get("shop_discount_rate", 1.0f);
    Debug.Log($"当前折扣率：{discount * 100}%");
}
```

**场景 2**：修改好感度增益
```csharp
// 累加好感度增益倍率
ActorStatsModule stats = GetComponent<ActorStatsModule>();
if (stats != null)
{
    stats.Add("friendship_gain_multiplier", 0.2f); // 从 1.5f 增加到 1.7f
}
```

**场景 3**：缺省 NPC 安全访问
```csharp
// 旧 NPC 没有 ActorStatsModule，也不会报错
ActorStatsModule stats = GetComponent<ActorStatsModule>();
if (stats != null)
{
    float speed = stats.Get("move_speed", 1.0f);
}
else
{
    // 使用默认值
    float speed = 1.0f;
}
```

**缺省行为**：
- ❌ NPC 不挂 `ActorStatsModule` → `GetComponent` 返回 null，逻辑需要判空
- ✅ NPC 挂了 `ActorStatsModule` 但 `baseStats` 为空 → `Get` 返回默认值
- ✅ NPC 挂了 `ActorStatsModule` 且 `baseStats` 非空 → 正常读写

---

### 3.3 NPCDefinition 扩展字段

**文件路径**：`Assets/Scripts/NPCSystem/NPCDefinition.cs`

**新增字段**：
```csharp
[Header("属性配置（v1.1 扩展）")]
[Tooltip("可选的属性列表，用于技能、商店等功能")]
[ConditionalField(nameof(enableStatsModule))]
public List<StatEntry> baseStats = new List<StatEntry>();
```

**字段说明**：

| 字段 | 类型 | 默认值 | 用途 | 约束 |
|-----|------|--------|------|------|
| `baseStats` | List<StatEntry> | 空列表 | 基础属性列表 | 可选，为空则不挂模块 |

**编辑器写入路径**：
- **配置位置**：`Assets/Resources/NPCDefinitions/` 目录下的 NPCDefinition 资产
- **写入目标**：`NPCPrefabBuilder` 在构建 prefab 时，如果 `baseStats` 非空，则挂 `ActorStatsModule` 并写入数据
- **运行时读取**：`ActorStatsModule.baseStats` 字段

---

## 四、技能模块数据结构

### 4.1 SkillDefinitionSO（技能定义资产）

**文件路径**：`Assets/Scripts/ActorSystem/Skills/SkillDefinitionSO.cs`

**代码定义**：
```csharp
[CreateAssetMenu(fileName = "Skill_", menuName = "Farm/Skill Definition", order = 21)]
public class SkillDefinitionSO : ScriptableObject
{
    [Header("技能标识")]
    [Tooltip("技能的唯一标识符，用于 TryCast/IsReady 接口")]
    public string skillId = "";

    [Header("技能名称")]
    [Tooltip("技能的显示名称")]
    public string displayName = "";

    [Header("冷却时间（秒）")]
    [Tooltip("技能触发后的冷却时间，冷却期间不能重复触发")]
    public float cooldownSeconds = 5f;

    [Header("技能参数（扩展预留）")]
    [Tooltip("技能的额外参数，如数值、范围、效果持续时间等")]
    public List<SkillParameter> parameters = new List<SkillParameter>();
}

[System.Serializable]
public class SkillParameter
{
    public string key = "";
    public float value = 0f;
}
```

**字段说明**：

| 字段 | 类型 | 默认值 | 用途 | 约束 |
|-----|------|--------|------|------|
| `skillId` | string | "" | 技能唯一标识符 | 不能为空 |
| `displayName` | string | "" | 技能显示名称 | 可为空 |
| `cooldownSeconds` | float | 5f | 冷却时间（秒） | 必须 > 0 |
| `parameters` | List<SkillParameter> | 空列表 | 技能参数 | 可选 |

**常用技能预定义**：

| 技能 ID | 用途 | 冷却时间 | 参数示例 |
|---------|------|----------|----------|
| `shop_discount` | 商店折扣 | 600s（10分钟） | discount_rate=0.8f |
| `farming_boost` | 农业增益 | 300s（5分钟） | growth_speed=1.5f |
| `weather_control` | 天气控制 | 1800s（30分钟） | weather_type="sunny" |
| `gift_double` | 礼物双倍 | 86400s（24小时） | multiplier=2.0f |

**边界情况处理**：
- ✅ `skillId` 为空字符串 → 技能无法被引用
- ✅ `cooldownSeconds` ≤ 0 → 技能可以无限触发
- ✅ `parameters` 为空列表 → 技能无额外参数

**编辑器创建路径**：
1. 在 Project 窗口右键 → `Create` → `Farm` → `Skill Definition`
2. 命名规则：`Skill_<Skill ID>`（例如 `Skill_shop_discount`）
3. 填写 `skillId`、`displayName`、`cooldownSeconds` 和可选的 `parameters`

---

### 4.2 SkillModule（技能模块）

**文件路径**：`Assets/Scripts/ActorSystem/Skills/SkillModule.cs`

**代码定义**：
```csharp
public class SkillModule : ActorModule
{
    [Header("技能列表（可序列化）")]
    [Tooltip("NPC 配置的技能，运行时管理冷却状态")]
    public List<SkillDefinitionSO> skills = new List<SkillDefinitionSO>();

    [System.NonSerialized]
    private Dictionary<string, float> cooldownTable = new Dictionary<string, float>();

    /// <summary>
    /// 尝试施放技能，成功则进入冷却
    /// </summary>
    public bool TryCast(string skillId)
    {
        // 实现：
        // 1. 检查技能是否在技能列表中
        // 2. 检查技能是否冷却中
        // 3. 如果未冷却，标记为冷却中，返回 true
        // 4. 否则返回 false
    }

    /// <summary>
    /// 检查技能是否就绪（未冷却）
    /// </summary>
    public bool IsReady(string skillId)
    {
        // 实现：检查 cooldownTable 中该技能的冷却时间是否 ≤ 0
    }

    /// <summary>
    /// 更新冷却时间（每帧调用或 tick 调用）
    /// </summary>
    public void Tick(float dt)
    {
        // 实现：遍历 cooldownTable，每个冷却时间 -= dt
        // 移除冷却时间 ≤ 0 的条目
    }
}
```

**核心 API 说明**：

| API | 参数 | 返回值 | 说明 |
|-----|------|--------|------|
| `TryCast(string skillId)` | skillId: 技能标识符 | bool | 尝试施放，成功进入冷却 |
| `IsReady(string skillId)` | skillId: 技能标识符 | bool | 检查是否就绪 |
| `Tick(float dt)` | dt: 时间增量（秒） | void | 更新所有冷却时间 |

**冷却逻辑流程**：

```
1. 首次触发技能
   → 调用 TryCast("shop_discount")
   → 检查技能列表中存在该技能
   → 检查 cooldownTable 中该技能冷却时间 ≤ 0
   → 如果通过，设置冷却时间 = SkillDefinitionSO.cooldownSeconds
   → 返回 true

2. 冷却期间再次触发
   → 调用 TryCast("shop_discount")
   → 检查 cooldownTable 中该技能冷却时间 > 0
   → 返回 false

3. Tick 刷新冷却
   → 每帧或每 tick 调用 Tick(dt)
   → 遍历 cooldownTable，每个冷却时间 -= dt
   → 冷却时间 ≤ 0 时，从 cooldownTable 移除

4. 冷却结束后再次触发
   → 调用 TryCast("shop_discount")
   → cooldownTable 中该技能不存在（已被移除）
   → 重新进入步骤 1
```

**使用示例**：

**场景 1**：商店折扣技能
```csharp
SkillModule skillModule = GetComponent<SkillModule>();
if (skillModule != null && skillModule.TryCast("shop_discount"))
{
    // 技能触发成功，进入冷却
    ApplyShopDiscount(0.8f); // 8 折
}
else
{
    // 技能冷却中或不存在
    Debug.Log("折扣技能不可用");
}
```

**场景 2**：检查技能是否就绪
```csharp
SkillModule skillModule = GetComponent<SkillModule>();
if (skillModule != null && skillModule.IsReady("shop_discount"))
{
    // 技能就绪，可以显示按钮
    shopDiscountButton.SetActive(true);
}
```

**场景 3**：Tick 刷新冷却
```csharp
// 在 ActorTickModule 中每 tick 调用
public void OnTick(float dt)
{
    SkillModule skillModule = GetComponent<SkillModule>();
    if (skillModule != null)
    {
        skillModule.Tick(dt);
    }
}
```

**缺省行为**：
- ❌ NPC 不挂 `SkillModule` → 无法使用技能，不影响其他功能
- ✅ NPC 挂了 `SkillModule` 但 `skills` 为空列表 → 所有 API 调用返回 false
- ✅ NPC 挂了 `SkillModule` 且 `skills` 非空 → 正常管理技能冷却

**边界情况处理**：
- ✅ `skillId` 不存在于技能列表 → `TryCast` 和 `IsReady` 返回 false
- ✅ `cooldownSeconds` = 0 → 技能可以无限触发（`IsReady` 永远返回 true）
- ✅ Tick 传入负数 `dt` → 冷却时间增加（不应该发生，但不会崩溃）

---

### 4.3 NPCDefinition 扩展字段

**文件路径**：`Assets/Scripts/NPCSystem/NPCDefinition.cs`

**新增字段**：
```csharp
[Header("技能配置（v1.1 扩展）")]
[Tooltip("可选的技能列表，用于 NPC 技能系统")]
[ConditionalField(nameof(enableSkillModule))]
public List<SkillDefinitionSO> skills = new List<SkillDefinitionSO>();
```

**字段说明**：

| 字段 | 类型 | 默认值 | 用途 | 约束 |
|-----|------|--------|------|------|
| `skills` | List<SkillDefinitionSO> | 空列表 | 技能列表 | 可选，为空则不挂模块 |

**编辑器写入路径**：
- **配置位置**：`Assets/Resources/NPCDefinitions/` 目录下的 NPCDefinition 资产
- **写入目标**：`NPCPrefabBuilder` 在构建 prefab 时，如果 `skills` 非空，则挂 `SkillModule` 并写入引用
- **运行时读取**：`SkillModule.skills` 字段

---

## 五、NPCDefinition 扩展字段汇总

### 5.1 完整扩展字段清单

**文件路径**：`Assets/Scripts/NPCSystem/NPCDefinition.cs`

**新增字段**（按顺序添加到文件末尾）：

```csharp
[Header("台词分组配置（v1.1 扩展）")]
[Tooltip("可选的台词分组资产，支持首次会面、好感度分级等分组规则")]
public DialogueSetSO dialogueSet;

[Header("属性配置（v1.1 扩展）")]
[Tooltip("可选的属性列表，用于技能、商店等功能")]
[ConditionalField(nameof(enableStatsModule))]
public List<StatEntry> baseStats = new List<StatEntry>();

[Header("技能配置（v1.1 扩展）")]
[Tooltip("可选的技能列表，用于 NPC 技能系统")]
[ConditionalField(nameof(enableSkillModule))]
public List<SkillDefinitionSO> skills = new List<SkillDefinitionSO>();
```

### 5.2 字段说明汇总表

| 字段 | 类型 | 默认值 | 用途 | 缺省行为 | 可选性 |
|-----|------|--------|------|----------|--------|
| `dialogueSet` | DialogueSetSO | null | 台词分组资产 | 回退到旧逻辑 | ✅ 可选 |
| `baseStats` | List<StatEntry> | 空列表 | 基础属性列表 | 不挂 ActorStatsModule | ✅ 可选 |
| `skills` | List<SkillDefinitionSO> | 空列表 | 技能列表 | 不挂 SkillModule | ✅ 可选 |

### 5.3 旧 NPC 兼容性

**v1.0 NPC 配置**（不配置新字段）：
```yaml
npcId: "shopman"
npcName: "商店店主"
dialogLines: ["你好，欢迎来到商店。", "有什么需要帮忙的吗？"]
# 不配置 dialogueSet、baseStats、skills
```

**v1.1 运行时行为**：
- ✅ `dialogueSet` 为 null → 使用 `dialogLines` 旧逻辑
- ✅ `baseStats` 为空列表 → 不挂 `ActorStatsModule`
- ✅ `skills` 为空列表 → 不挂 `SkillModule`
- ✅ 对话/交互/商店功能正常，与 v1.0 一致

**v1.1 NPC 配置**（配置新字段）：
```yaml
npcId: "shopman"
npcName: "商店店主"
dialogLines: ["你好，欢迎来到商店。", "有什么需要帮忙的吗？"]
dialogueSet: DialogueSet_shopman
baseStats:
  - key: "shop_discount_rate"
    value: 0.8f
skills:
  - Skill_shop_discount
```

**v1.1 运行时行为**：
- ✅ `dialogueSet` 不为 null → 使用新分组逻辑，失败则回退旧逻辑
- ✅ `baseStats` 非空 → 挂 `ActorStatsModule`，可读写属性
- ✅ `skills` 非空 → 挂 `SkillModule`，可触发技能
- ✅ 对话/交互/商店功能增强，同时保持向后兼容

---

## 六、编辑器写入路径与数据流

### 6.1 数据流示意图

```
内容资产（ScriptableObject）
       ↓
NPCDefinition（定义文件）
       ↓
NPCPrefabBuilder（编辑器构建）
       ↓
NPC Prefab（运行时组件）
       ↓
运行时逻辑（DialogueResolver、ActorStatsModule、SkillModule）
```

### 6.2 具体写入路径

| 数据类型 | 配置位置 | 构建器写入 | 运行时读取 | 回退逻辑 |
|---------|---------|-----------|-----------|---------|
| `dialogueSet` | `NPCDefinition.dialogueSet` | `NPCPrefabBuilder` 写入 `DialogueResolver.dialogueSet` | `DialogueResolver.GetCurrentDialogue()` | `ActorIdentity.GetDefaultDialog()` |
| `baseStats` | `NPCDefinition.baseStats` | `NPCPrefabBuilder` 挂 `ActorStatsModule` 并写入 `baseStats` | `ActorStatsModule.Get/Set/Add` | 返回默认值 |
| `skills` | `NPCDefinition.skills` | `NPCPrefabBuilder` 挂 `SkillModule` 并写入 `skills` | `SkillModule.TryCast/IsReady/Tick` | 返回 false |

### 6.3 NPCPrefabBuilder 构建逻辑

**文件路径**：`Assets/Scripts/Editor/NPC/NPCPrefabBuilder.cs`

**台词分组构建**：
```csharp
// 写入 DialogueResolver.dialogueSet
var dialogueResolver = npcActor.GetComponent<DialogueResolver>();
if (dialogueResolver != null && definition.dialogueSet != null)
{
    dialogueResolver.dialogueSet = definition.dialogueSet;
}
// 如果 definition.dialogueSet 为 null，不修改 dialogueResolver，保持旧逻辑
```

**属性模块构建**：
```csharp
// 如果 baseStats 非空，挂 ActorStatsModule 并写入数据
if (definition.baseStats != null && definition.baseStats.Count > 0)
{
    var statsModule = npcActor.GetOrAddComponent<ActorStatsModule>();
    statsModule.baseStats = definition.baseStats;
}
// 如果 baseStats 为空，不挂 ActorStatsModule
```

**技能模块构建**：
```csharp
// 如果 skills 非空，挂 SkillModule 并写入引用
if (definition.skills != null && definition.skills.Count > 0)
{
    var skillModule = npcActor.GetOrAddComponent<SkillModule>();
    skillModule.skills = definition.skills;
}
// 如果 skills 为空，不挂 SkillModule
```

---

## 七、数据结构验收清单

### 7.1 台词分组验收

- ✅ `DialogueGroup` 包含 `key` 和 `lines` 字段
- ✅ `DialogueSetSO` 包含 `List<DialogueGroup> groups` 字段
- ✅ `NPCDefinition` 扩展 `dialogueSet` 字段
- ✅ `DialogueResolver` 扩展 `dialogueSet` 字段
- ✅ `DialogueResolver.GetCurrentDialogue()` 实现分组选择规则
- ✅ 不配置 `dialogueSet` 时回退到 `ActorIdentity.GetDefaultDialog()`
- ✅ 配置 `dialogueSet` 但所有分组为空时回退到旧逻辑
- ✅ 首次会面优先 `first_meet` 分组
- ✅ 好感度分级映射 `stranger/acquaintance/friend/close_friend`
- ✅ 空组、空文本、缺失 key 不报错

### 7.2 属性模块验收

- ✅ `StatEntry` 包含 `key` 和 `value` 字段
- ✅ `ActorStatsModule` 包含 `List<StatEntry> baseStats` 字段
- ✅ `ActorStatsModule` 实现 `Get/Set/Add` 三个核心 API
- ✅ `NPCDefinition` 扩展 `baseStats` 字段
- ✅ `NPCPrefabBuilder` 在 `baseStats` 非空时挂 `ActorStatsModule`
- ✅ 不配置 `baseStats` 时不挂 `ActorStatsModule`
- ✅ 缺省 NPC 访问属性时返回默认值，不报错
- ✅ 至少有一个实际属性示例（如 `friendship_gain_multiplier`）

### 7.3 技能模块验收

- ✅ `SkillDefinitionSO` 包含 `skillId`、`displayName`、`cooldownSeconds` 字段
- ✅ `SkillModule` 包含 `List<SkillDefinitionSO> skills` 字段
- ✅ `SkillModule` 实现 `TryCast/IsReady/Tick` 三个核心 API
- ✅ `SkillModule` 运行时冷却表不序列化
- ✅ `NPCDefinition` 扩展 `skills` 字段
- ✅ `NPCPrefabBuilder` 在 `skills` 非空时挂 `SkillModule`
- ✅ 不配置 `skills` 时不挂 `SkillModule`
- ✅ 技能首次触发成功，冷却期间不能重复触发
- ✅ 冷却结束后可再次成功触发
- ✅ 至少有一个试点技能验证完整流程

### 7.4 旧 NPC 兼容性验收

- ✅ 不配置 `dialogueSet/baseStats/skills` 时不报错
- ✅ 旧 NPC 对话功能与 v1.0 一致
- ✅ 旧 NPC 交互功能与 v1.0 一致
- ✅ 旧 NPC 商店功能与 v1.0 一致
- ✅ 旧 NPC 不挂任何新模块
- ✅ 旧 NPC 不产生任何额外性能开销

---

## 八、设计决策记录

### 8.1 为什么使用 List<> 而非 Dictionary？

**原因 1**：序列化问题
- `Dictionary<K,V>` 不支持 Unity 的 YAML 序列化
- Inspector 中无法直接编辑 Dictionary
- 需要手动编写序列化/反序列化逻辑

**原因 2**：运行时性能
- `List<>` 的线性查找对于小列表（2-4 个分组、5-10 个属性）性能足够
- `Dictionary<K,V>` 的哈希查找对于小列表反而有额外开销

**原因 3**：编辑器友好性
- `List<>` 在 Inspector 中显示为可展开列表，直观易用
- `Dictionary<K,V>` 需要自定义编辑器才能显示

**结论**：v1.1 的数据规模小，`List<>` 是更优选择。

### 8.2 为什么设计可选模块而非强制字段？

**原因 1**：向后兼容性
- 强制字段会破坏现有 NPC 配置
- 旧 NPC 需要逐一修改才能运行

**原因 2**：增量迁移
- 允许部分 NPC 先启用新功能
- 其他 NPC 保持不变，逐步迁移

**原因 3**：性能优化
- 缺省 NPC 不挂不需要的模块
- 减少内存占用和 CPU 开销

**结论**：v1.1 的"可选挂载"设计是正确的。

### 8.3 为什么设计回退逻辑而非强制升级？

**原因 1**：风险控制
- 新逻辑可能存在 Bug
- 回退逻辑保证旧功能始终可用

**原因 2**：渐进式增强
- 新功能作为增强选项
- 旧功能作为兜底保证

**原因 3**：调试友好性
- 可以单独测试新逻辑
- 出问题时可以临时关闭新功能

**结论**：v1.1 的"回退逻辑"设计是必要的。

---

## 九、后续任务依赖

### 9.1 任务 03 依赖

本设计文档为任务 03（实现 DialogueSetSO 与 DialogueResolver 分组选择逻辑）提供了：
- ✅ `DialogueGroup` 和 `DialogueSetSO` 的完整字段定义
- ✅ 分组选择规则的优先级顺序
- ✅ 好感度分组的映射规则
- ✅ 边界情况的处理方案

### 9.2 任务 04 依赖

本设计文档为任务 04（扩展 NPCPrefabBuilder 写入台词分组内容管线）提供了：
- ✅ `NPCDefinition.dialogueSet` 的字段定义
- ✅ 构建器写入 `DialogueResolver.dialogueSet` 的逻辑
- ✅ 旧 NPC 的兼容性保证

### 9.3 任务 05 依赖

本设计文档为任务 05（实现 ActorStatsModule 与基础属性读写能力）提供了：
- ✅ `StatEntry` 的完整字段定义
- ✅ `ActorStatsModule` 的核心 API 定义
- ✅ `NPCDefinition.baseStats` 的字段定义
- ✅ 构建器挂载 `ActorStatsModule` 的条件

### 9.4 任务 06 依赖

本设计文档为任务 06（实现 SkillDefinitionSO 与 SkillModule 冷却闭环）提供了：
- ✅ `SkillDefinitionSO` 的完整字段定义
- ✅ `SkillModule` 的核心 API 定义
- ✅ 冷却逻辑的详细流程说明
- ✅ `NPCDefinition.skills` 的字段定义
- ✅ 构建器挂载 `SkillModule` 的条件

---

## 十、附录：代码模板

### 10.1 DialogueSetSO 完整代码模板

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.ActorSystem
{
    [System.Serializable]
    public class DialogueGroup
    {
        [Header("分组标识（用于选择规则）")]
        [Tooltip("预定义分组：first_meet, daily, stranger, acquaintance, friend, close_friend")]
        public string key = "daily";

        [Header("台词列表")]
        [Tooltip("该分组的所有台词，运行时随机选择一条")]
        public List<string> lines = new List<string>();
    }

    [CreateAssetMenu(fileName = "DialogueSet_", menuName = "Farm/Dialogue Set", order = 20)]
    public class DialogueSetSO : ScriptableObject
    {
        [Header("台词分组列表")]
        [Tooltip("包含多个分组的台词集合，每个分组有独立的选择规则")]
        public List<DialogueGroup> groups = new List<DialogueGroup>();
    }
}
```

### 10.2 StatEntry 完整代码模板

```csharp
using System;

namespace FarmGame.ActorSystem
{
    [Serializable]
    public class StatEntry
    {
        [Header("属性名（键）")]
        [Tooltip("属性的唯一标识符，用于 Get/Set/Add 接口")]
        public string key = "";

        [Header("属性值")]
        [Tooltip("属性的数值，支持整数、浮点数等")]
        public float value = 0f;
    }
}
```

### 10.3 ActorStatsModule 核心代码模板

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.ActorSystem
{
    public class ActorStatsModule : ActorModule
    {
        [Header("基础属性列表（可序列化）")]
        [Tooltip("NPC 的基础属性配置，运行时可动态修改")]
        public List<StatEntry> baseStats = new List<StatEntry>();

        /// <summary>
        /// 读取属性值，不存在时返回默认值
        /// </summary>
        public float Get(string key, float defaultValue = 0f)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("ActorStatsModule.Get: key is null or empty");
                return defaultValue;
            }

            var entry = baseStats.Find(e => e.key == key);
            return entry != null ? entry.value : defaultValue;
        }

        /// <summary>
        /// 设置属性值，不存在则新增
        /// </summary>
        public void Set(string key, float value)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("ActorStatsModule.Set: key is null or empty");
                return;
            }

            var entry = baseStats.Find(e => e.key == key);
            if (entry != null)
            {
                entry.value = value;
            }
            else
            {
                baseStats.Add(new StatEntry { key = key, value = value });
            }
        }

        /// <summary>
        /// 累加属性值，不存在则从默认值开始累加
        /// </summary>
        public void Add(string key, float delta)
        {
            float currentValue = Get(key, 0f);
            Set(key, currentValue + delta);
        }
    }
}
```

### 10.4 SkillDefinitionSO 完整代码模板

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.ActorSystem
{
    [CreateAssetMenu(fileName = "Skill_", menuName = "Farm/Skill Definition", order = 21)]
    public class SkillDefinitionSO : ScriptableObject
    {
        [Header("技能标识")]
        [Tooltip("技能的唯一标识符，用于 TryCast/IsReady 接口")]
        public string skillId = "";

        [Header("技能名称")]
        [Tooltip("技能的显示名称")]
        public string displayName = "";

        [Header("冷却时间（秒）")]
        [Tooltip("技能触发后的冷却时间，冷却期间不能重复触发")]
        public float cooldownSeconds = 5f;

        [Header("技能参数（扩展预留）")]
        [Tooltip("技能的额外参数，如数值、范围、效果持续时间等")]
        public List<SkillParameter> parameters = new List<SkillParameter>();
    }

    [System.Serializable]
    public class SkillParameter
    {
        public string key = "";
        public float value = 0f;
    }
}
```

### 10.5 SkillModule 核心代码模板

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.ActorSystem
{
    public class SkillModule : ActorModule
    {
        [Header("技能列表（可序列化）")]
        [Tooltip("NPC 配置的技能，运行时管理冷却状态")]
        public List<SkillDefinitionSO> skills = new List<SkillDefinitionSO>();

        [System.NonSerialized]
        private Dictionary<string, float> cooldownTable = new Dictionary<string, float>();

        private SkillDefinitionSO FindSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return null;
            }

            return skills.Find(s => s.skillId == skillId);
        }

        /// <summary>
        /// 尝试施放技能，成功则进入冷却
        /// </summary>
        public bool TryCast(string skillId)
        {
            var skill = FindSkill(skillId);
            if (skill == null)
            {
                Debug.LogWarning($"SkillModule.TryCast: Skill '{skillId}' not found in skills list");
                return false;
            }

            if (IsCooling(skillId))
            {
                return false;
            }

            cooldownTable[skillId] = skill.cooldownSeconds;
            return true;
        }

        /// <summary>
        /// 检查技能是否就绪（未冷却）
        /// </summary>
        public bool IsReady(string skillId)
        {
            return !IsCooling(skillId);
        }

        private bool IsCooling(string skillId)
        {
            if (cooldownTable.TryGetValue(skillId, out float cooldown))
            {
                return cooldown > 0;
            }
            return false;
        }

        /// <summary>
        /// 更新冷却时间（每帧调用或 tick 调用）
        /// </summary>
        public void Tick(float dt)
        {
            var keysToRemove = new List<string>();

            foreach (var kvp in cooldownTable)
            {
                cooldownTable[kvp.Key] -= dt;
                if (cooldownTable[kvp.Key] <= 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                cooldownTable.Remove(key);
            }
        }
    }
}
```

---

**文档状态：✅ 已完成，可供后续编码任务直接使用**
