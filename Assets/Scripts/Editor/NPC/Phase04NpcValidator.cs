using System.Collections.Generic;
using System.Text;
using FarmGame.ActorSystem;
using FarmGame.NPCSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    }
}
