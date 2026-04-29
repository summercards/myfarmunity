using System.Collections.Generic;
using System.Text;
using FarmGame.ActorSystem;
using FarmGame.NPCSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ActorSkills = FarmGame.ActorSystem.Skills;

namespace FarmGame.Editor.NPC
{
    /// <summary>
    /// Phase 04 验证器：
    /// 1) 可选重建 NPC 预制体
    /// 2) 校验 NPCDefinition 与 ActorSystem 预制体的一致性
    /// </summary>
    public static class Phase04NpcValidator
    {
        private const string PrefabFolder = "Assets/Prefabs/NPC/";

        [MenuItem("工具/NPC/Phase04/仅验证 (Validate Only)")]
        public static void ValidateOnly()
        {
            RunValidation(rebuildBeforeValidate: false, includeSceneLegacyScan: false);
        }

        [MenuItem("工具/NPC/Phase04/重建并验证 (Rebuild + Validate)")]
        public static void RebuildAndValidate()
        {
            RunValidation(rebuildBeforeValidate: true, includeSceneLegacyScan: false);
        }

        [MenuItem("工具/NPC/Phase04/重建并验证 + Scene Legacy 扫描")]
        public static void RebuildAndValidateWithSceneScan()
        {
            RunValidation(rebuildBeforeValidate: true, includeSceneLegacyScan: true);
        }

        private static void RunValidation(bool rebuildBeforeValidate, bool includeSceneLegacyScan)
        {
            string[] guids = AssetDatabase.FindAssets("t:NPCDefinition");
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            int checkedCount = 0;

            foreach (string guid in guids)
            {
                string defPath = AssetDatabase.GUIDToAssetPath(guid);
                NPCDefinition def = AssetDatabase.LoadAssetAtPath<NPCDefinition>(defPath);
                if (def == null)
                {
                    warnings.Add($"[Load] 无法加载 NPCDefinition: {defPath}");
                    continue;
                }

                if (rebuildBeforeValidate)
                {
                    NPCPrefabBuilder.BuildPrefab(def);
                }

                ValidateOne(def, errors, warnings);
                checkedCount++;
            }

            if (includeSceneLegacyScan)
            {
                ValidateSceneLegacy(errors);
            }

            PrintResult(checkedCount, errors, warnings, rebuildBeforeValidate);
        }

        private static void ValidateOne(NPCDefinition def, List<string> errors, List<string> warnings)
        {
            string prefabPath = PrefabFolder + def.npcId + ".prefab";
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                errors.Add($"[{def.npcId}] 缺少预制体: {prefabPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                ValidateCore(def, root, errors, warnings);
                ValidateFeatureModules(def, root, errors);
                ValidateVisuals(def, root, errors);
                ValidatePhysics(def, root, errors);
                ValidateV1_1DataConsistency(def, root, errors, warnings);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateCore(NPCDefinition def, GameObject root, List<string> errors, List<string> warnings)
        {
            Actor actor = root.GetComponent<Actor>();
            ActorIdentity identity = root.GetComponent<ActorIdentity>();
            ActorMemory memory = root.GetComponent<ActorMemory>();
            ActorBrain brain = root.GetComponent<ActorBrain>();
            ActorDialogue dialogue = root.GetComponent<ActorDialogue>();
            DialogueResolver resolver = root.GetComponent<DialogueResolver>();

            if (actor == null) errors.Add($"[{def.npcId}] 缺少 Actor 组件");
            if (identity == null) errors.Add($"[{def.npcId}] 缺少 ActorIdentity 组件");
            if (memory == null) errors.Add($"[{def.npcId}] 缺少 ActorMemory 组件");
            if (brain == null) errors.Add($"[{def.npcId}] 缺少 ActorBrain 组件");
            if (dialogue == null) errors.Add($"[{def.npcId}] 缺少 ActorDialogue 组件");
            if (resolver == null) errors.Add($"[{def.npcId}] 缺少 DialogueResolver 组件");

#pragma warning disable 0618
            FarmGame.NPCSystem.NPCInteractable[] legacy = root.GetComponentsInChildren<FarmGame.NPCSystem.NPCInteractable>(true);
#pragma warning restore 0618
            if (legacy != null && legacy.Length > 0)
            {
                errors.Add($"[{def.npcId}] 预制体仍包含兼容层 NPCInteractable（数量={legacy.Length}，阶段4要求移除）");
            }

            if (identity != null)
            {
                if (identity.Id != def.npcId)
                {
                    errors.Add($"[{def.npcId}] ActorIdentity.Id 不匹配，当前={identity.Id}");
                }

                if (identity.Name != def.npcName)
                {
                    errors.Add($"[{def.npcId}] ActorIdentity.Name 不匹配，当前={identity.Name}");
                }

                ActorFunction expected = MapFunction(def.function);
                if (identity.Function != expected)
                {
                    errors.Add($"[{def.npcId}] ActorIdentity.Function 不匹配，当前={identity.Function} 期望={expected}");
                }

                if (!identity.IsInitialized)
                {
                    errors.Add($"[{def.npcId}] ActorIdentity 未初始化（IsInitialized=false）");
                }
            }
        }

        private static void ValidateFeatureModules(NPCDefinition def, GameObject root, List<string> errors)
        {
            bool hasInteraction = root.GetComponent<ActorInteraction>() != null;
            if (def.enableInteraction != hasInteraction)
            {
                errors.Add($"[{def.npcId}] ActorInteraction 不匹配，enableInteraction={def.enableInteraction} 实际={hasInteraction}");
            }

            bool hasShop = root.GetComponent<ShopModule>() != null;
            bool hasDialog = root.GetComponent<DialogueModule>() != null;
            bool hasQuest = root.GetComponent<QuestModule>() != null;
            bool hasGift = root.GetComponent<GiftModule>() != null;

            bool expectShop = def.function == NPCFunction.OpenShop && def.enableShop;
            bool expectDialog = def.function == NPCFunction.Talk;
            bool expectQuest = def.function == NPCFunction.Quest;
            bool expectGift = def.function == NPCFunction.Gift;

            if (hasShop != expectShop) errors.Add($"[{def.npcId}] ShopModule 不匹配，实际={hasShop} 期望={expectShop}");
            if (hasDialog != expectDialog) errors.Add($"[{def.npcId}] DialogueModule 不匹配，实际={hasDialog} 期望={expectDialog}");
            if (hasQuest != expectQuest) errors.Add($"[{def.npcId}] QuestModule 不匹配，实际={hasQuest} 期望={expectQuest}");
            if (hasGift != expectGift) errors.Add($"[{def.npcId}] GiftModule 不匹配，实际={hasGift} 期望={expectGift}");

            int enabledModuleCount = (hasShop ? 1 : 0) + (hasDialog ? 1 : 0) + (hasQuest ? 1 : 0) + (hasGift ? 1 : 0);
            int expectedModuleCount = (expectShop ? 1 : 0) + (expectDialog ? 1 : 0) + (expectQuest ? 1 : 0) + (expectGift ? 1 : 0);
            if (enabledModuleCount != expectedModuleCount)
            {
                errors.Add($"[{def.npcId}] 功能模块数量不匹配，实际={enabledModuleCount} 期望={expectedModuleCount}");
            }
        }

        private static void ValidateVisuals(NPCDefinition def, GameObject root, List<string> errors)
        {
            bool hasAnimator = root.GetComponent<Animator>() != null;
            bool hasView = root.GetComponent<ActorView>() != null;
            if (def.enableVisuals)
            {
                if (!hasAnimator) errors.Add($"[{def.npcId}] enableVisuals=true 但缺少 Animator");
                if (!hasView) errors.Add($"[{def.npcId}] enableVisuals=true 但缺少 ActorView");
            }
            else
            {
                if (hasAnimator) errors.Add($"[{def.npcId}] enableVisuals=false 但存在 Animator");
                if (hasView) errors.Add($"[{def.npcId}] enableVisuals=false 但存在 ActorView");
            }
        }

        private static void ValidatePhysics(NPCDefinition def, GameObject root, List<string> errors)
        {
            bool hasRigidbody = root.GetComponent<Rigidbody>() != null;
            bool hasCollider = root.GetComponent<CapsuleCollider>() != null;
            if (def.enableCollider)
            {
                if (!hasRigidbody) errors.Add($"[{def.npcId}] enableCollider=true 但缺少 Rigidbody");
                if (!hasCollider) errors.Add($"[{def.npcId}] enableCollider=true 但缺少 CapsuleCollider");
            }
            else
            {
                if (hasRigidbody) errors.Add($"[{def.npcId}] enableCollider=false 但存在 Rigidbody");
                if (hasCollider) errors.Add($"[{def.npcId}] enableCollider=false 但存在 CapsuleCollider");
            }
        }

        private static ActorFunction MapFunction(NPCFunction legacyFunction)
        {
            switch (legacyFunction)
            {
                case NPCFunction.OpenShop:
                    return ActorFunction.OpenShop;
                case NPCFunction.Talk:
                    return ActorFunction.Talk;
                case NPCFunction.Quest:
                    return ActorFunction.Quest;
                case NPCFunction.Gift:
                    return ActorFunction.Gift;
                case NPCFunction.None:
                default:
                    return ActorFunction.None;
            }
        }

        private static void PrintResult(int checkedCount, List<string> errors, List<string> warnings, bool rebuildBeforeValidate)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Phase04NpcValidator] 验证完成");
            sb.AppendLine($"Mode: {(rebuildBeforeValidate ? "Rebuild + Validate" : "Validate Only")}");
            sb.AppendLine($"Checked NPCDefinitions: {checkedCount}");
            sb.AppendLine($"Errors: {errors.Count}");
            sb.AppendLine($"Warnings: {warnings.Count}");

            if (errors.Count > 0)
            {
                sb.AppendLine("---- Errors ----");
                foreach (string error in errors)
                {
                    sb.AppendLine(error);
                }
            }

            if (warnings.Count > 0)
            {
                sb.AppendLine("---- Warnings ----");
                foreach (string warning in warnings)
                {
                    sb.AppendLine(warning);
                }
            }

            if (errors.Count == 0)
            {
                Debug.Log(sb.ToString());
            }
            else
            {
                Debug.LogError(sb.ToString());
            }
        }

        private static void ValidateSceneLegacy(List<string> errors)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                errors.Add("[SceneScan] 用户取消保存场景，跳过 Scene Legacy 校验。");
                return;
            }

            string originalScenePath = SceneManager.GetActiveScene().path;
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });

            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

#pragma warning disable 0618
                NPCInteractable[] legacy = Object.FindObjectsOfType<NPCInteractable>(true);
#pragma warning restore 0618
                if (legacy != null && legacy.Length > 0)
                {
                    errors.Add($"[Scene:{scene.path}] 仍包含 legacy NPCInteractable（数量={legacy.Length}）");
                }
            }

            if (!string.IsNullOrEmpty(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }
        }

        /// <summary>
        /// v1.1: 数据一致性校验
        /// 验证配置数据与运行时组件的一致性
        /// 重点抓"数据已配置但运行时组件没挂上"的不一致问题
        /// </summary>
        private static void ValidateV1_1DataConsistency(NPCDefinition def, GameObject root, List<string> errors, List<string> warnings)
        {
            // 校验 1: dialogueSet 配置与 DialogueResolver 的 dialogueSet 引用一致性
            ValidateDialogueSetConsistency(def, root, errors);

            // 校验 2: baseStats 配置与 ActorStatsModule 的存在一致性
            ValidateBaseStatsConsistency(def, root, errors);

            // 校验 3: skills 配置与 SkillModule 的存在一致性
            ValidateSkillsConsistency(def, root, errors);

            // 校验 4: enableLowFrequencyTick 与 ActorTickModule 的存在一致性
            ValidateTickModuleConsistency(def, root, errors);
        }

        /// <summary>
        /// 校验 dialogueSet 配置与 DialogueResolver 的 dialogueSet 引用一致性
        /// </summary>
        private static void ValidateDialogueSetConsistency(NPCDefinition def, GameObject root, List<string> errors)
        {
            // 检查是否配置了 dialogueSet
            bool hasConfiguredDialogueSet = def.dialogueSet != null;

            if (!hasConfiguredDialogueSet)
            {
                // dialogueSet 为 null，不进行校验（可选模块为空）
                return;
            }

            // dialogueSet 不为 null，检查是否存在 DialogueResolver 组件
            DialogueResolver resolver = root.GetComponent<DialogueResolver>();
            if (resolver == null)
            {
                errors.Add($"[{def.npcId}] 配置了 dialogueSet ({def.dialogueSet.name}) 但预制体缺少 DialogueResolver 组件");
                errors.Add($"[{def.npcId}] → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）");
                return;
            }

            // 检查 DialogueResolver 的 dialogueSet 引用是否正确
            if (resolver.dialogueSet == null)
            {
                errors.Add($"[{def.npcId}] 配置了 dialogueSet ({def.dialogueSet.name}) 但 DialogueResolver.dialogueSet 为 null");
                errors.Add($"[{def.npcId}] → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）");
                return;
            }

            // 检查引用是否一致
            if (resolver.dialogueSet != def.dialogueSet)
            {
                errors.Add($"[{def.npcId}] DialogueResolver.dialogueSet ({resolver.dialogueSet.name}) 与配置的 dialogueSet ({def.dialogueSet.name}) 不一致");
                errors.Add($"[{def.npcId}] → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）");
                return;
            }
        }

        /// <summary>
        /// 校验 baseStats 配置与 ActorStatsModule 的存在一致性
        /// </summary>
        private static void ValidateBaseStatsConsistency(NPCDefinition def, GameObject root, List<string> errors)
        {
            // 检查是否配置了 baseStats
            bool hasConfiguredBaseStats = def.baseStats != null && def.baseStats.Count > 0;

            if (!hasConfiguredBaseStats)
            {
                // baseStats 为空，不进行校验（可选模块为空）
                return;
            }

            // baseStats 不为空，检查是否存在 ActorStatsModule 组件
            ActorStatsModule statsModule = root.GetComponent<ActorStatsModule>();
            if (statsModule == null)
            {
                errors.Add($"[{def.npcId}] 配置了 baseStats ({def.baseStats.Count} 个属性) 但预制体缺少 ActorStatsModule 组件");
                errors.Add($"[{def.npcId}] → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）");
                return;
            }

            // 检查 ActorStatsModule 的 baseStats 是否正确
            if (statsModule.baseStats == null || statsModule.baseStats.Count == 0)
            {
                errors.Add($"[{def.npcId}] 配置了 baseStats ({def.baseStats.Count} 个属性) 但 ActorStatsModule.baseStats 为空");
                errors.Add($"[{def.npcId}] → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）");
                return;
            }

            // 检查属性数量是否一致
            if (statsModule.baseStats.Count != def.baseStats.Count)
            {
                errors.Add($"[{def.npcId}] ActorStatsModule.baseStats 数量 ({statsModule.baseStats.Count}) 与配置的 baseStats 数量 ({def.baseStats.Count}) 不一致");
                errors.Add($"[{def.npcId}] → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）");
                return;
            }
        }

        /// <summary>
        /// 校验 skills 配置与 SkillModule 的存在一致性
        /// </summary>
        private static void ValidateSkillsConsistency(NPCDefinition def, GameObject root, List<string> errors)
        {
            // 检查是否配置了 skills
            bool hasConfiguredSkills = def.skills != null && def.skills.Count > 0;

            if (!hasConfiguredSkills)
            {
                // skills 为空，不进行校验（可选模块为空）
                return;
            }

            // skills 不为空，检查是否存在 Skills.SkillModule 组件
            ActorSkills.SkillModule skillModule = root.GetComponent<ActorSkills.SkillModule>();
            if (skillModule == null)
            {
                errors.Add($"[{def.npcId}] 配置了 skills ({def.skills.Count} 个技能) 但预制体缺少 Skills.SkillModule 组件");
                errors.Add($"[{def.npcId}] → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）");
                return;
            }

            // 检查 Skills.SkillModule 的 skills 是否正确
            if (skillModule.skills == null || skillModule.skills.Count == 0)
            {
                errors.Add($"[{def.npcId}] 配置了 skills ({def.skills.Count} 个技能) 但 Skills.SkillModule.skills 为空");
                errors.Add($"[{def.npcId}] → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）");
                return;
            }

            // 检查技能数量是否一致
            if (skillModule.skills.Count != def.skills.Count)
            {
                errors.Add($"[{def.npcId}] Skills.SkillModule.skills 数量 ({skillModule.skills.Count}) 与配置的 skills 数量 ({def.skills.Count}) 不一致");
                errors.Add($"[{def.npcId}] → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）");
                return;
            }
        }

        /// <summary>
        /// 校验 enableLowFrequencyTick 与 ActorTickModule 的存在一致性
        /// </summary>
        private static void ValidateTickModuleConsistency(NPCDefinition def, GameObject root, List<string> errors)
        {
            // 检查是否启用了低频调度
            bool enableLowFrequencyTick = def.enableLowFrequencyTick;

            if (!enableLowFrequencyTick)
            {
                // enableLowFrequencyTick 为 false，不进行校验（可选模块为空）
                return;
            }

            // enableLowFrequencyTick 为 true，检查是否配置了 skills（因为 ActorTickModule 主要驱动技能冷却）
            bool hasConfiguredSkills = def.skills != null && def.skills.Count > 0;

            if (!hasConfiguredSkills)
            {
                // 没有配置技能，但启用了低频调度
                // 警告（不是错误，因为未来可能有其他用途）
                errors.Add($"[{def.npcId}] 启用了低频调度 (enableLowFrequencyTick=true) 但没有配置任何技能");
                errors.Add($"[{def.npcId}] → 建议：配置技能后再启用低频调度，或者取消启用低频调度");
                return;
            }

            // enableLowFrequencyTick 为 true 且有技能，检查是否存在 ActorTickModule 组件
            ActorTickModule tickModule = root.GetComponent<ActorTickModule>();
            if (tickModule == null)
            {
                errors.Add($"[{def.npcId}] 启用了低频调度 (enableLowFrequencyTick=true) 且配置了技能 ({def.skills.Count} 个) 但预制体缺少 ActorTickModule 组件");
                errors.Add($"[{def.npcId}] → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）");
                return;
            }

            // 检查 ActorTickModule 的配置是否正确
            if (Mathf.Abs(tickModule.tickInterval - def.tickInterval) > 0.0001f)
            {
                errors.Add($"[{def.npcId}] ActorTickModule.tickInterval ({tickModule.tickInterval}) 与配置的 tickInterval ({def.tickInterval}) 不一致");
                errors.Add($"[{def.npcId}] → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）");
                return;
            }

            if (tickModule.useUnscaledTime != def.useUnscaledTime)
            {
                errors.Add($"[{def.npcId}] ActorTickModule.useUnscaledTime ({tickModule.useUnscaledTime}) 与配置的 useUnscaledTime ({def.useUnscaledTime}) 不一致");
                errors.Add($"[{def.npcId}] → 建议操作：重新构建预制体（工具/NPC/构建选中 NPC 预制体）");
                return;
            }
        }
    }
}
