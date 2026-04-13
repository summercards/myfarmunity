/// <summary>
/// Phase04NpcValidator v1.1 验证脚本
/// 用于测试编辑器校验器的 2 个正例和 2 个反例
/// </summary>

namespace FarmGame.ValidatorTest
{
    /// <summary>
    /// Phase04NpcValidator 验证文档
    /// 包含 2 个正例和 2 个反例的详细说明
    /// </summary>
    public class Phase04NpcValidatorV1_1_Verification
    {
        #region 正例（Positive Cases - 应该通过校验）

        /// <summary>
        /// 正例 1: 完整配置的 NPC
        /// 配置了所有 v1.1 特性，且运行时组件都正确挂载
        /// </summary>
        public class PositiveCase1_FullConfiguration
        {
            /// <summary>
            /// NPCDefinition 配置
            /// </summary>
            public static readonly string NPCDefinitionConfig = @"
NPCDefinition: shopman

配置:
- npcId: shopman
- npcName: 商店老板
- function: OpenShop
- enableInteraction: true
- enableShop: true
- enableVisuals: true
- enableCollider: true

v1.1 扩展:
- dialogueSet: ShopmanDialogueSet (非 null)
  └─ 包含首次会话、重复对话、好感度分级等分组

- baseStats:
  └─ friendship_gain_multiplier: 1.5
  └─ max_hp: 100

- skills:
  └─ discount_blessing (SkillDefinitionSO)
  └─ special_offer (SkillDefinitionSO)

- enableLowFrequencyTick: true
- tickInterval: 1.0
- useUnscaledTime: false
";

            /// <summary>
            /// 预制体组件（应存在）
            /// </summary>
            public static readonly string PrefabComponents = @"
预制体: Assets/Prefabs/NPC/shopman.prefab

核心组件:
- Actor ✓
- ActorIdentity ✓
- ActorIdentity.Id = shopman ✓
- ActorIdentity.Name = 商店老板 ✓
- ActorIdentity.Function = OpenShop ✓
- ActorIdentity.IsInitialized = true ✓
- ActorMemory ✓
- ActorBrain ✓
- ActorDialogue ✓
- DialogueResolver ✓
  └─ DialogueResolver.dialogueSet = ShopmanDialogueSet ✓

功能模块:
- ActorInteraction ✓
- ShopModule ✓

v1.1 模块:
- ActorStatsModule ✓
  └─ ActorStatsModule.baseStats.Count = 2 ✓
- Skills.SkillModule ✓
  └─ Skills.SkillModule.skills.Count = 2 ✓
- ActorTickModule ✓
  └─ ActorTickModule.tickInterval = 1.0 ✓
  └─ ActorTickModule.useUnscaledTime = false ✓

视觉组件:
- Animator ✓
- ActorView ✓

物理组件:
- Rigidbody ✓
- CapsuleCollider ✓
";

            /// <summary>
            /// 预期结果
            /// </summary>
            public static readonly string ExpectedResult = @"
验证结果: 通过 (0 errors, 0 warnings)

原因:
1. ✓ dialogueSet 配置与 DialogueResolver.dialogueSet 引用一致
2. ✓ baseStats 配置与 ActorStatsModule 一致
3. ✓ skills 配置与 Skills.SkillModule 一致
4. ✓ enableLowFrequencyTick 与 ActorTickModule 配置一致
";
        }

        /// <summary>
        /// 正例 2: 最小配置的 NPC（未配置 v1.1 特性）
        /// 未配置 v1.1 特性，不应触发 v1.1 校验错误
        /// </summary>
        public class PositiveCase2_MinimalConfiguration
        {
            /// <summary>
            /// NPCDefinition 配置
            /// </summary>
            public static readonly string NPCDefinitionConfig = @"
NPCDefinition: farmer

配置:
- npcId: farmer
- npcName: 农夫
- function: Talk
- enableInteraction: true
- enableShop: false
- enableVisuals: true
- enableCollider: true

v1.1 扩展:
- dialogueSet: null (未配置)
- baseStats: [] (空列表)
- skills: [] (空列表)
- enableLowFrequencyTick: false
";

            /// <summary>
            /// 预制体组件（应存在）
            /// </summary>
            public static readonly string PrefabComponents = @"
预制体: Assets/Prefabs/NPC/farmer.prefab

核心组件:
- Actor ✓
- ActorIdentity ✓
- ActorIdentity.Id = farmer ✓
- ActorIdentity.Name = 农夫 ✓
- ActorIdentity.Function = Talk ✓
- ActorIdentity.IsInitialized = true ✓
- ActorMemory ✓
- ActorBrain ✓
- ActorDialogue ✓
- DialogueResolver ✓
  └─ DialogueResolver.dialogueSet = null ✓

功能模块:
- ActorInteraction ✓
- DialogueModule ✓

v1.1 模块:
- ActorStatsModule ✗ (无，符合预期)
- Skills.SkillModule ✗ (无，符合预期)
- ActorTickModule ✗ (无，符合预期)

视觉组件:
- Animator ✓
- ActorView ✓

物理组件:
- Rigidbody ✓
- CapsuleCollider ✓
";

            /// <summary>
            /// 预期结果
            /// </summary>
            public static readonly string ExpectedResult = @"
验证结果: 通过 (0 errors, 0 warnings)

原因:
1. ✓ dialogueSet 为 null，未触发校验（可选模块为空）
2. ✓ baseStats 为空，未触发校验（可选模块为空）
3. ✓ skills 为空，未触发校验（可选模块为空）
4. ✓ enableLowFrequencyTick 为 false，未触发校验（可选模块为空）
";
        }

        #endregion

        #region 反例（Negative Cases - 应该报告错误）

        /// <summary>
        /// 反例 1: 配置了数据但未挂载模块
        /// dialogueSet、baseStats、skills 都配置了，但预制体缺少对应的组件
        /// </summary>
        public class NegativeCase1_DataConfiguredButMissingComponents
        {
            /// <summary>
            /// NPCDefinition 配置
            /// </summary>
            public static readonly string NPCDefinitionConfig = @"
NPCDefinition: blacksmith

配置:
- npcId: blacksmith
- npcName: 铁匠
- function: OpenShop
- enableInteraction: true
- enableShop: true
- enableVisuals: true
- enableCollider: true

v1.1 扩展:
- dialogueSet: BlacksmithDialogueSet (非 null)
  └─ 包含首次会话、重复对话、好感度分级等分组

- baseStats:
  └─ smithing_skill: 80
  └─ max_hp: 120

- skills:
  └─ repair_tool (SkillDefinitionSO)
  └─ craft_weapon (SkillDefinitionSO)

- enableLowFrequencyTick: true
- tickInterval: 1.0
- useUnscaledTime: false
";

            /// <summary>
            /// 预制体组件（缺少 v1.1 模块）
            /// </summary>
            public static readonly string PrefabComponents = @"
预制体: Assets/Prefabs/NPC/blacksmith.prefab

核心组件:
- Actor ✓
- ActorIdentity ✓
- ActorIdentity.Id = blacksmith ✓
- ActorIdentity.Name = 铁匠 ✓
- ActorIdentity.Function = OpenShop ✓
- ActorIdentity.IsInitialized = true ✓
- ActorMemory ✓
- ActorBrain ✓
- ActorDialogue ✓
- DialogueResolver ✓
  └─ DialogueResolver.dialogueSet = null ✗ (错误)

功能模块:
- ActorInteraction ✓
- ShopModule ✓

v1.1 模块:
- ActorStatsModule ✗ (缺少)
- Skills.SkillModule ✗ (缺少)
- ActorTickModule ✗ (缺少)

视觉组件:
- Animator ✓
- ActorView ✓

物理组件:
- Rigidbody ✓
- CapsuleCollider ✓
";

            /// <summary>
            /// 预期结果
            /// </summary>
            public static readonly string ExpectedResult = @"
验证结果: 失败 (6 errors)

错误列表:
1. [blacksmith] 配置了 dialogueSet (BlacksmithDialogueSet) 但预制体缺少 DialogueResolver 组件
   → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）

2. [blacksmith] 配置了 dialogueSet (BlacksmithDialogueSet) 但 DialogueResolver.dialogueSet 为 null
   → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）

3. [blacksmith] 配置了 baseStats (2 个属性) 但预制体缺少 ActorStatsModule 组件
   → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）

4. [blacksmith] 配置了 skills (2 个技能) 但预制体缺少 Skills.SkillModule 组件
   → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）

5. [blacksmith] 启用了低频调度 (enableLowFrequencyTick=true) 且配置了技能 (2 个) 但预制体缺少 ActorTickModule 组件
   → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）

6. [blacksmith] 启用了低频调度 (enableLowFrequencyTick=true) 但没有配置任何技能
   → 建议：配置技能后再启用低频调度，或者取消启用低频调度

修复步骤:
1. 在编辑器中选择 blacksmith 的 NPCDefinition
2. 点击 菜单 → 工具 → NPC → 构建选中 NPC 预制体
3. 重新运行验证，应该通过
";
        }

        /// <summary>
        /// 反例 2: 配置与预制体不一致
        /// 配置和预制体都有数据，但数据不一致（数量、引用不匹配）
        /// </summary>
        public class NegativeCase2_DataInconsistency
        {
            /// <summary>
            /// NPCDefinition 配置
            /// </summary>
            public static readonly string NPCDefinitionConfig = @"
NPCDefinition: alchemist

配置:
- npcId: alchemist
- npcName: 炼金术士
- function: Talk
- enableInteraction: true
- enableShop: false
- enableVisuals: true
- enableCollider: true

v1.1 扩展:
- dialogueSet: AlchemistDialogueSet (非 null)
  └─ 包含首次会话、重复对话、好感度分级等分组

- baseStats:
  └─ alchemy_skill: 90
  └─ max_hp: 80
  └─ mana: 100

- skills:
  └─ brew_potion (SkillDefinitionSO)
  └─ transmute (SkillDefinitionSO)
  └─ enchant (SkillDefinitionSO)

- enableLowFrequencyTick: true
- tickInterval: 0.5
- useUnscaledTime: true
";

            /// <summary>
            /// 预制体组件（数据不一致）
            /// </summary>
            public static readonly string PrefabComponents = @"
预制体: Assets/Prefabs/NPC/alchemist.prefab

核心组件:
- Actor ✓
- ActorIdentity ✓
- ActorIdentity.Id = alchemist ✓
- ActorIdentity.Name = 炼金术士 ✓
- ActorIdentity.Function = Talk ✓
- ActorIdentity.IsInitialized = true ✓
- ActorMemory ✓
- ActorBrain ✓
- ActorDialogue ✓
- DialogueResolver ✓
  └─ DialogueResolver.dialogueSet = WrongDialogueSet (错误，应该是 AlchemistDialogueSet) ✗

功能模块:
- ActorInteraction ✓
- DialogueModule ✓

v1.1 模块:
- ActorStatsModule ✓
  └─ ActorStatsModule.baseStats.Count = 2 (错误，应该是 3) ✗

- Skills.SkillModule ✓
  └─ Skills.SkillModule.skills.Count = 2 (错误，应该是 3) ✗

- ActorTickModule ✓
  └─ ActorTickModule.tickInterval = 1.0 (错误，应该是 0.5) ✗
  └─ ActorTickModule.useUnscaledTime = false (错误，应该是 true) ✗

视觉组件:
- Animator ✓
- ActorView ✓

物理组件:
- Rigidbody ✓
- CapsuleCollider ✓
";

            /// <summary>
            /// 预期结果
            /// </summary>
            public static readonly string ExpectedResult = @"
验证结果: 失败 (5 errors)

错误列表:
1. [alchemist] DialogueResolver.dialogueSet (WrongDialogueSet) 与配置的 dialogueSet (AlchemistDialogueSet) 不一致
   → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）

2. [alchemist] ActorStatsModule.baseStats 数量 (2) 与配置的 baseStats 数量 (3) 不一致
   → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）

3. [alchemist] Skills.SkillModule.skills 数量 (2) 与配置的 skills 数量 (3) 不一致
   → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）

4. [alchemist] ActorTickModule.tickInterval (1) 与配置的 tickInterval (0.5) 不一致
   → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）

5. [alchemist] ActorTickModule.useUnscaledTime (False) 与配置的 useUnscaledTime (True) 不一致
   → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）

修复步骤:
1. 在编辑器中选择 alchemist 的 NPCDefinition
2. 点击 菜单 → 工具 → NPC → 构建选中 NPC 预制体
3. 重新运行验证，应该通过
";
        }

        #endregion

        #region 测试场景

        /// <summary>
        /// 测试场景说明
        /// </summary>
        public static readonly string TestScenarios = @"
测试场景 1: 完整配置的 NPC（shopman）
目标: 验证所有 v1.1 特性正确配置和挂载
操作:
1. 创建 shopman NPCDefinition，配置所有 v1.1 特性
2. 构建预制体（工具/NPC/构建选中 NPC 预制体）
3. 运行验证（工具/NPC/Phase04/仅验证 (Validate Only)）
预期: 0 errors, 0 warnings

测试场景 2: 最小配置的 NPC（farmer）
目标: 验证未配置 v1.1 特性时不触发错误
操作:
1. 创建 farmer NPCDefinition，不配置 v1.1 特性
2. 构建预制体
3. 运行验证
预期: 0 errors, 0 warnings

测试场景 3: 配置了数据但未挂载模块（blacksmith）
目标: 验证数据已配置但模块缺失时正确报错
操作:
1. 创建 blacksmith NPCDefinition，配置所有 v1.1 特性
2. 手动修改预制体，删除 v1.1 模块（ActorStatsModule、SkillModule、ActorTickModule）
3. 运行验证
预期: 报告 6 个错误，提供修复建议

测试场景 4: 配置与预制体不一致（alchemist）
目标: 验证数据和预制体不一致时正确报错
操作:
1. 创建 alchemist NPCDefinition，配置所有 v1.1 特性
2. 手动修改预制体，修改 v1.1 模块的数据（数量、引用、参数）
3. 运行验证
预期: 报告 5 个错误，提供修复建议
";

        #endregion

        #region 校验器使用说明

        /// <summary>
        /// 校验器使用说明
        /// </summary>
        public static readonly string UsageGuide = @"
使用方法:
1. 仅验证模式（不重建预制体）:
   - 菜单 → 工具 → NPC → Phase04 → 仅验证 (Validate Only)
   - 适用于快速检查现有预制体

2. 重建并验证模式:
   - 菜单 → 工具 → NPC → Phase04 → 重建并验证 (Rebuild + Validate)
   - 适用于批量修复问题

3. 重建并验证 + Scene Legacy 扫描:
   - 菜单 → 工具 → NPC → Phase04 → 重建并验证 + Scene Legacy 扫描
   - 适用于全面检查（包括场景中的旧 NPC）

常见错误:
1. 配置了 dialogueSet 但预制体缺少 DialogueResolver 组件
   → 修复：重新构建预制体

2. 配置了 baseStats 但预制体缺少 ActorStatsModule 组件
   → 修复：重新构建预制体

3. 配置了 skills 但预制体缺少 Skills.SkillModule 组件
   → 修复：重新构建预制体

4. 启用了低频调度但预制体缺少 ActorTickModule 组件
   → 修复：重新构建预制体

5. 数据引用不一致（DialogueResolver.dialogueSet != def.dialogueSet）
   → 修复：重新构建预制体

6. 数据数量不一致（ActorStatsModule.baseStats.Count != def.baseStats.Count）
   → 修复：重新构建预制体

注意事项:
1. 校验器只检查 NPCDefinition 和预制体的一致性
2. 不检查 NPCDefinition 本身的配置是否合理（如技能冷却时间是否合理）
3. 可选模块为空时不会触发错误（如 dialogueSet = null）
4. 数据已配置但模块缺失时会触发错误（如 dialogueSet != null 但无 DialogueResolver）
";
    }
    #endregion
}
