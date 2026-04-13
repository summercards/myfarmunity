# 我的农场 v1.1 回归验收脚本

## 验收目标

基于试点 NPC 执行完整回归，验证 v1.1 的五个核心目标全部达成。

---

## 核心目标验证

### G1. NPC 成为"独立个体"（Actor）

**验收标准**：
- 每个 NPC 至少具备 Identity/Memory/Brain/Interaction 这条主线组件结构
- 可被识别和调度

**验证步骤**：
1. 选择试点 NPC：`shopman`（商店老板）
2. 加载场景并找到 shopman 预制体
3. 检查预制体组件

**预期结果**：
- ✅ Actor 组件存在
- ✅ ActorIdentity 组件存在
- ✅ ActorMemory 组件存在
- ✅ ActorBrain 组件存在
- ✅ ActorInteraction 组件存在

**验证命令**：
```csharp
// 在游戏运行时，通过 Console 执行
GameObject shopman = GameObject.Find("shopman");
Debug.Log($"Actor: {shopman.GetComponent<Actor>() != null}");
Debug.Log($"ActorIdentity: {shopman.GetComponent<ActorIdentity>() != null}");
Debug.Log($"ActorMemory: {shopman.GetComponent<ActorMemory>() != null}");
Debug.Log($"ActorBrain: {shopman.GetComponent<ActorBrain>() != null}");
Debug.Log($"ActorInteraction: {shopman.GetComponent<ActorInteraction>() != null}");
```

**验收结果**：[待验证]
- 通过：所有组件都存在
- 失败：缺少任何一个核心组件
- 风险待观察：组件存在但功能异常

---

### G2. 模块化（台词/属性/技能），保持轻量

#### G2.1 台词模块化

**验收标准**：
- 至少 2 组台词能按简单条件切换
- 保留默认台词列表作为 fallback

**验证步骤**：
1. 配置 shopman 的台词分组（`dialogueSet`）
   - 首次会话：`first_meet`
   - 重复对话：`daily`
   - 好感度分级：`friend`（好感度 >= 50）
   - 亲密朋友：`close_friend`（好感度 >= 80）
2. 在游戏中进行对话测试：
   - 首次对话：触发 `first_meet`
   - 重复对话：触发 `daily`
   - 提升好感度到 50：触发 `friend`
   - 提升好感度到 80：触发 `close_friend`

**预期结果**：
- ✅ 首次对话触发 `first_meet` 分组
- ✅ 重复对话触发 `daily` 分组
- ✅ 好感度 >= 50 时触发 `friend` 分组
- ✅ 好感度 >= 80 时触发 `close_friend` 分组
- ✅ 缺少 `dialogueSet` 时回退到默认台词列表

**验证命令**：
```csharp
// 在游戏运行时，通过 Console 执行
GameObject shopman = GameObject.Find("shopman");
ActorIdentity identity = shopman.GetComponent<ActorIdentity>();
ActorMemory memory = shopman.GetComponent<ActorMemory>();
DialogueResolver resolver = shopman.GetComponent<DialogueResolver>();

// 检查好感度
int friendship = memory.GetStat("friendship");
Debug.Log($"当前好感度: {friendship}");

// 检查当前对话分组
string currentGroup = resolver.GetCurrentDialogueGroup();
Debug.Log($"当前对话分组: {currentGroup}");

// 模拟好感度变化
memory.SetStat("friendship", 50);
Debug.Log($"好感度提升到 50，预期分组: friend，实际分组: {resolver.GetCurrentDialogueGroup()}");

memory.SetStat("friendship", 80);
Debug.Log($"好感度提升到 80，预期分组: close_friend，实际分组: {resolver.GetCurrentDialogueGroup()}");
```

**验收结果**：[待验证]
- 通过：所有分组切换正常
- 失败：分组切换异常或回退失败
- 风险待观察：分组切换正常但显示异常

---

#### G2.2 属性模块化

**验收标准**：
- 至少 1 个 NPC 可读写属性条目
- 缺省 NPC 不报错

**验证步骤**：
1. 配置 shopman 的属性（`baseStats`）：
   - `friendship_gain_multiplier: 1.5`（好感度加成倍率）
   - `max_hp: 100`（最大生命值）
2. 在游戏中进行属性读写测试：
   - 读取属性值
   - 修改属性值
   - 添加新属性
   - 删除属性

**预期结果**：
- ✅ 可以读取已配置的属性
- ✅ 可以修改已配置的属性
- ✅ 可以添加新属性
- ✅ 可以删除属性
- ✅ 缺少 `baseStats` 配置时不会报错

**验证命令**：
```csharp
// 在游戏运行时，通过 Console 执行
GameObject shopman = GameObject.Find("shopman");
ActorStatsModule statsModule = shopman.GetComponent<ActorStatsModule>();

if (statsModule != null)
{
    // 读取属性
    float friendshipMultiplier = statsModule.GetStat("friendship_gain_multiplier");
    Debug.Log($"好感度加成倍率: {friendshipMultiplier}");

    float maxHp = statsModule.GetStat("max_hp");
    Debug.Log($"最大生命值: {maxHp}");

    // 修改属性
    statsModule.SetStat("friendship_gain_multiplier", 2.0f);
    Debug.Log($"修改后好感度加成倍率: {statsModule.GetStat("friendship_gain_multiplier")}");

    // 添加新属性
    statsModule.AddStat("luck", 50);
    Debug.Log($"添加运气属性: {statsModule.GetStat("luck")}");

    // 删除属性
    statsModule.RemoveStat("luck");
    Debug.Log($"删除运气属性: {statsModule.GetStat("luck", 0)}");
}
else
{
    Debug.Log("ActorStatsModule 不存在");
}
```

**验收结果**：[待验证]
- 通过：所有属性操作正常
- 失败：属性读写失败或报错
- 风险待观察：属性操作正常但逻辑异常

---

#### G2.3 技能模块化

**验收标准**：
- 至少 1 个 NPC 可触发技能且有冷却
- 技能定义使用 ScriptableObject
- 技能支持自定义参数

**验证步骤**：
1. 配置 shopman 的技能（`skills`）：
   - `discount_blessing`（折扣祝福）
     - 技能 ID: `discount_blessing`
     - 显示名称: 折扣祝福
     - 冷却时间: 60 秒
     - 参数: `discount_rate: 0.2`（折扣率 20%）
   - `special_offer`（特价优惠）
     - 技能 ID: `special_offer`
     - 显示名称: 特价优惠
     - 冷却时间: 120 秒
     - 参数: `special_price: 50`（特价 50 金币）
2. 在游戏中进行技能测试：
   - 尝试施放技能（首次）
   - 等待冷却结束
   - 尝试施放技能（再次）
   - 在冷却期间尝试施放

**预期结果**：
- ✅ 首次施放技能成功
- ✅ 冷却期间无法施放技能
- ✅ 冷却结束后可以再次施放技能
- ✅ 技能参数正确传递

**验证命令**：
```csharp
// 在游戏运行时，通过 Console 执行
GameObject shopman = GameObject.Find("shopman");
SkillModule skillModule = shopman.GetComponent<SkillModule>();

if (skillModule != null)
{
    // 查找技能
    SkillDefinitionSO skill = skillModule.GetSkillDefinition("discount_blessing");

    if (skill != null)
    {
        // 首次施放（预期成功）
        bool result1 = skillModule.TryCast("discount_blessing");
        Debug.Log($"首次施放结果: {result1}");

        // 检查冷却状态
        bool isReady1 = skillModule.IsReady("discount_blessing");
        Debug.Log($"冷却状态: {isReady1}");

        // 等待 10 秒（模拟冷却）
        yield return new WaitForSeconds(10);

        // 在冷却期间尝试施放（预期失败）
        bool result2 = skillModule.TryCast("discount_blessing");
        Debug.Log($"冷却期间施放结果: {result2}");

        // 等待冷却结束（60 秒）
        yield return new WaitForSeconds(60);

        // 冷却结束后施放（预期成功）
        bool result3 = skillModule.TryCast("discount_blessing");
        Debug.Log($"冷却结束后施放结果: {result3}");

        // 获取技能参数
        float discountRate = skill.GetParameter<float>("discount_rate");
        Debug.Log($"折扣率: {discountRate}");
    }
    else
    {
        Debug.Log("技能 discount_blessing 不存在");
    }
}
else
{
    Debug.Log("SkillModule 不存在");
}
```

**验收结果**：[待验证]
- 通过：所有技能操作正常
- 失败：技能施放或冷却异常
- 风险待观察：技能操作正常但效果异常

---

### G3. NPC 可独立调度（低频 tick）

**验收标准**：
- NPC 可以以低频 tick（例如 0.2s 或 0.5s）刷新技能冷却、日常作息、状态等逻辑
- 不在每个模块里新增 `Update()`
- 优先使用统一调度器 `FarmGame.Core.TaskManager` 注册可取消任务

**验证步骤**：
1. 配置 shopman 的低频调度：
   - `enableLowFrequencyTick: true`
   - `tickInterval: 1.0`（1 秒）
   - `useUnscaledTime: false`
2. 在游戏中进行调度测试：
   - 检查模块是否启用
   - 检查调度是否注册
   - 观察技能冷却是否正常递减
   - 禁用 NPC，验证调度是否停止
   - 切换场景，验证无悬挂回调

**预期结果**：
- ✅ 模块启用后开始调度
- ✅ 技能冷却正常递减
- ✅ 禁用 NPC 后调度停止
- ✅ 切换场景后无悬挂回调
- ✅ 没有新增大量 `Update()` 方法

**验证命令**：
```csharp
// 在游戏运行时，通过 Console 执行
GameObject shopman = GameObject.Find("shopman");
ActorTickModule tickModule = shopman.GetComponent<ActorTickModule>();

if (tickModule != null)
{
    // 检查模块状态
    Debug.Log($"模块启用: {tickModule.IsEnabled}");
    Debug.Log($"正在调度: {tickModule.IsScheduling}");
    Debug.Log($"Tick 周期: {tickModule.TickInterval}");
    Debug.Log($"Tick 模式: {tickModule.TickMode}");

    // 等待几秒，观察日志
    yield return new WaitForSeconds(5);

    // 禁用模块
    tickModule.Disable();
    yield return new WaitForSeconds(1);

    // 检查调度是否停止
    Debug.Log($"禁用后正在调度: {tickModule.IsScheduling}");

    // 重新启用
    tickModule.Enable();
    yield return new WaitForSeconds(1);

    // 检查调度是否恢复
    Debug.Log($"重新启用后正在调度: {tickModule.IsScheduling}");
}
else
{
    Debug.Log("ActorTickModule 不存在");
}
```

**性能验证**：
- 检查是否有大量新增的 `Update()` 方法
- 使用 Profiler 监控性能

**验收结果**：[待验证]
- 通过：所有调度操作正常，无性能问题
- 失败：调度异常或性能问题
- 风险待观察：调度正常但性能有轻微影响

---

### G4. 扩展性与迁移友好

**验收标准**：
- 新增模块不需要改动对话/交互/商店主链路
- 允许增量迁移：旧 NPC 不配置新字段时仍可运行
- 新能力按"可选字段/可选模块"启用

**验证步骤**：
1. 创建一个新的 NPC（`farmer`），不配置任何 v1.1 特性：
   - `dialogueSet: null`
   - `baseStats: []`
   - `skills: []`
   - `enableLowFrequencyTick: false`
2. 构建 farmer 预制体
3. 在游戏中测试：
   - 对话功能是否正常
   - 交互功能是否正常
   - 是否有报错

**预期结果**：
- ✅ 旧 NPC（不配置 v1.1 特性）仍然可以正常运行
- ✅ 对话功能正常
- ✅ 交互功能正常
- ✅ 没有报错

**验收结果**：[待验证]
- 通过：旧 NPC 正常运行
- 失败：旧 NPC 报错或功能异常
- 风险待观察：旧 NPC 运行正常但有警告

---

## 主链路验证（验收口径 1）

### 主链路不回退

**验收标准**：
- 对话打开/切换/关闭、商店等功能仍可用
- 不出现主流程退化

**验证步骤**：
1. 测试对话打开：
   - 靠近 NPC
   - 按下 E 键
   - 预期：对话框打开，显示对话内容

2. 测试对话切换：
   - 在对话框中切换到另一个 NPC
   - 预期：对话框内容更新为新 NPC 的对话

3. 测试对话关闭：
   - 点击关闭按钮或按下 ESC
   - 预期：对话框关闭

4. 测试商店功能（shopman）：
   - 打开商店对话框
   - 预期：商店按钮可用，可以打开商店界面

5. 测试交互功能：
   - 与 NPC 交互
   - 预期：交互功能正常

**验收结果**：[待验证]
- 通过：所有主链路功能正常
- 失败：主链路出现退化
- 风险待观察：主链路功能正常但有轻微问题

**关键规则**：
- 如果主链路出现任何回退，应直接阻断版本完成判定，不能用"模块功能已完成"掩盖主流程退化。

---

## 验收结果分类

### 通过（Pass）
- 所有验证步骤都符合预期结果
- 没有报错或异常
- 主链路功能正常

### 失败（Fail）
- 验证步骤不符合预期结果
- 出现报错或异常
- 主链路出现退化

### 风险待观察（Risk）
- 验证步骤基本符合预期结果
- 没有报错或异常
- 但有轻微问题或不稳定因素

---

## 最终输出

### v1.1 是否满足发布条件？

**发布条件**：
1. ✅ G1 通过：NPC 成为"独立个体"（Actor）
2. ✅ G2 通过：模块化（台词/属性/技能），保持轻量
3. ✅ G3 通过：NPC 可独立调度（低频 tick）
4. ✅ G4 通过：扩展性与迁移友好
5. ✅ 主链路不回退：对话打开/切换/关闭、商店等功能仍可用

**判定结果**：[待判定]

### 如果不满足，剩余阻塞项是什么？

**阻塞项列表**：
- [待补充]

### 建议修复方向

**失败项修复建议**：
- [待补充]

**风险项缓解建议**：
- [待补充]

---

## 验收脚本使用说明

### 执行前准备

1. **创建试点 NPC**：
   - 使用 `NPCDefinition` 创建 shopman
   - 配置所有 v1.1 特性（`dialogueSet`、`baseStats`、`skills`、`enableLowFrequencyTick`）
   - 使用 `NPCPrefabBuilder` 构建预制体

2. **创建对比 NPC**：
   - 使用 `NPCDefinition` 创建 farmer
   - 不配置任何 v1.1 特性
   - 使用 `NPCPrefabBuilder` 构建预制体

3. **场景准备**：
   - 在场景中摆放 shopman 和 farmer
   - 保存场景

### 执行步骤

1. **运行游戏**：
   - 启动游戏
   - 进入包含 shopman 和 farmer 的场景

2. **执行验证**：
   - 按照每个验收标准的验证步骤执行
   - 记录验证结果

3. **记录问题**：
   - 如果遇到失败或风险，记录：
     - 复现步骤
     - 影响范围
     - 建议修复方向

4. **生成报告**：
   - 填写验收结果
   - 填写最终输出
   - 生成验收报告

### 性能监控

1. **开启 Profiler**：
   - Window → Analysis → Profiler
   - 开始录制

2. **观察性能指标**：
   - CPU 使用率
   - 内存使用量
   - 调度任务数量

3. **对比基准**：
   - 与 v1.0 性能对比
   - 确认没有性能退化

---

**文档版本**：v1.0
**创建日期**：2026-04-08
**最后更新**：2026-04-08
