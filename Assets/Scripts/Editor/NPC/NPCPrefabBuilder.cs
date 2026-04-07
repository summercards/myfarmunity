using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Reflection;
using FarmGame.NPCSystem;
using FarmGame.ActorSystem; // 引用新系统

namespace FarmGame.Editor.NPC
{
    public static class NPCPrefabBuilder
    {
        private const string PREFAB_FOLDER = "Assets/Prefabs/NPC/";

        [MenuItem("工具/NPC/构建选中 NPC 预制体")]
        public static void BuildSelected()
        {
            var def = Selection.activeObject as NPCDefinition;
            if (def == null)
            {
                EditorUtility.DisplayDialog("NPC Builder", "请选择一个 NPCDefinition 资产。", "OK");
                return;
            }

            BuildPrefab(def);
        }

        [MenuItem("工具/NPC/重建所有 NPC 预制体")]
        public static void RebuildAll()
        {
            var guids = AssetDatabase.FindAssets("t:NPCDefinition");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<NPCDefinition>(path);
                if (def != null)
                {
                    BuildPrefab(def);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"所有 NPC 预制体已重建（找到 {guids.Length} 个）");
        }

        public static GameObject BuildPrefab(NPCDefinition def)
        {
            if (!Directory.Exists(PREFAB_FOLDER))
                Directory.CreateDirectory(PREFAB_FOLDER);

            string prefabPath = PREFAB_FOLDER + def.npcId + ".prefab";

            GameObject root = CreateOrLoadRoot(prefabPath, def);
            ApplyStructure(root, def);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            GameObject.DestroyImmediate(root);

            Debug.Log($"已构建 NPC 预制体：{def.npcId} 位于 {prefabPath}");
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        private static GameObject CreateOrLoadRoot(string path, NPCDefinition def)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return (GameObject)PrefabUtility.InstantiatePrefab(existing);

            return new GameObject(def.npcId);
        }

        private static void ApplyStructure(GameObject root, NPCDefinition def)
        {
            root.name = def.npcId;

            // ==========================================
            // 1. 公共基础：物理与模型生成
            // ==========================================
            
            // 物理/碰撞模块
            if (def.enableCollider)
            {
                var rb = EnsureOrReplace<Rigidbody>(root);
                rb.isKinematic = true; 
                rb.useGravity = false;

                var col = EnsureOrReplace<CapsuleCollider>(root);
                col.center = def.colliderCenter;
                col.radius = def.colliderRadius;
                col.height = def.colliderHeight;

                // 自动设置 Layer 为 Interactable
                int layer = LayerMask.NameToLayer("Interactable");
                if (layer != -1) 
                {
                    root.layer = layer;
                }
                else 
                {
                    Debug.LogWarning("[NPCPrefabBuilder] Layer 'Interactable' not found! NPC interaction might fail. Please add this Layer in Project Settings.");
                }
            }
            else
            {
                RemoveIfExists<Rigidbody>(root);
                RemoveIfExists<CapsuleCollider>(root);
                root.layer = 0;
            }

            // 视觉子物体生成
            Transform visual = root.transform.Find("Visual");
            if (def.enableVisuals)
            {
                if (visual == null)
                {
                    GameObject v = new GameObject("Visual");
                    v.transform.SetParent(root.transform);
                    v.transform.localPosition = Vector3.zero;
                    v.transform.localRotation = Quaternion.identity;
                    v.transform.localScale = Vector3.one;
                    visual = v.transform;
                }
                
                // 强制刷新模型：清空 Visual 下的所有子物体，确保与 Definition 一致
                while (visual.childCount > 0) {
                    GameObject.DestroyImmediate(visual.GetChild(0).gameObject);
                }

                // 实例化模型
                if (def.modelPrefab != null)
                {
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(def.modelPrefab);
                    model.transform.SetParent(visual);
                    model.transform.localPosition = def.modelLocalPosition;
                    model.transform.localEulerAngles = def.modelLocalEuler;
                    model.transform.localScale = def.modelLocalScale;
                }
            }
            else
            {
                if (visual != null) GameObject.DestroyImmediate(visual.gameObject);
                visual = null;
            }

            // ==========================================
            // 2. 架构收口：统一走 Actor 系统
            // ==========================================
            if (!def.useActorSystem)
            {
                Debug.LogWarning($"[NPCPrefabBuilder] {def.npcId} 的 useActorSystem=false 已过时，构建器将强制使用 ActorSystem。");
            }

            int removedLegacyCount = RemoveLegacyNpcInteractables(root);
            if (removedLegacyCount > 0)
            {
                Debug.Log($"[NPCPrefabBuilder] {def.npcId} 已移除 {removedLegacyCount} 个旧版 NPCInteractable 组件。");
            }

            // --- 新架构 Actor ---
            
            // Phase 8 修复：确保所有 Actor 组件都正确添加
            EnsureOrReplace<Actor>(root);

            // Phase 3: 使用 Initialize 方法
            var identity = EnsureOrReplace<ActorIdentity>(root);
            identity.Initialize(BuildIdentitySeed(def));

            EnsureOrReplace<ActorMemory>(root);
            EnsureOrReplace<ActorBrain>(root);
            EnsureOrReplace<ActorDialogue>(root);
            EnsureOrReplace<DialogueResolver>(root);

            // 交互组件
            if (def.enableInteraction)
            {
                EnsureOrReplace<ActorInteraction>(root);
            }
            else
            {
                RemoveIfExists<ActorInteraction>(root);
            }

            // 模块化系统
            // 先清理历史残留模块，避免功能切换后同一 NPC 残留多套模块。
            RemoveIfExists<ShopModule>(root);
            RemoveIfExists<DialogueModule>(root);
            RemoveIfExists<QuestModule>(root);
            RemoveIfExists<GiftModule>(root);

            switch (def.function)
            {
                case NPCFunction.OpenShop:
                    {
                        if (!def.enableShop)
                        {
                            Debug.LogWarning($"[NPCPrefabBuilder] {def.npcId} 功能为 OpenShop，但 enableShop=false，已跳过添加 ShopModule。");
                            break;
                        }

                        var shopModule = EnsureOrReplace<ShopModule>(root);
                        // Phase 7 修复：配置商店目录
                        var catalogField = shopModule.GetType().GetField("shopCatalog", BindingFlags.Public | BindingFlags.Instance);
                        if (catalogField != null)
                        {
                            catalogField.SetValue(shopModule, def.defaultShopCatalog);
                            Debug.Log($"[NPCPrefabBuilder] 已配置商店目录到 ShopModule");
                        }
                        else
                        {
                            Debug.LogWarning("[NPCPrefabBuilder] ShopModule 没有 shopCatalog 字段");
                        }
                        break;
                    }

                case NPCFunction.Talk:
                    EnsureOrReplace<DialogueModule>(root);
                    break;

                case NPCFunction.Quest:
                    EnsureOrReplace<QuestModule>(root);
                    break;

                case NPCFunction.Gift:
                    EnsureOrReplace<GiftModule>(root);
                    break;

                case NPCFunction.None:
                default:
                    // 不添加任何模块
                    break;
            }

            // 視觉组件
            if (def.enableVisuals)
            {
                var animator = EnsureOrReplace<Animator>(root);
                animator.runtimeAnimatorController = def.animatorController;

                var view = EnsureOrReplace<ActorView>(root);
                view.Animator = animator;
                view.ModelRoot = visual;
            }
            else
            {
                RemoveIfExists<Animator>(root);
                RemoveIfExists<ActorView>(root);
            }
        }

        /// <summary>
        /// 确保组件存在且有效，如果无效则替换
        /// Phase 8 修复：检测并替换 Missing Script 组件
        /// </summary>
        private static T EnsureOrReplace<T>(GameObject go) where T : Component
        {
            // 获取所有 T 类型的组件
            var components = go.GetComponents<T>();
            
            // 查找有效的组件（不是 null 且脚本是正确的）
            T validComponent = null;
            foreach (var comp in components)
            {
                if (comp != null && comp.GetType() == typeof(T))
                {
                    validComponent = comp;
                    break;
                }
            }
            
            // 如果有有效组件，返回
            if (validComponent != null)
            {
                return validComponent;
            }
            
            // 如果没有有效组件，删除所有 T 类型的组件（包括 Missing Script）
            RemoveAllComponentsOfType<T>(go);
            
            // 添加新组件
            return go.AddComponent<T>();
        }

        /// <summary>
        /// 删除所有 T 类型的组件（包括 Missing Script）
        /// </summary>
        private static void RemoveAllComponentsOfType<T>(GameObject go) where T : Component
        {
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                // 检查组件类型是否为 T 或其派生类
                // 使用字符串比较，因为 Missing Script 的类型为 null
                if (comp == null)
                {
                    // Missing Script 组件，删除
                    GameObject.DestroyImmediate(comp);
                }
                else if (typeof(T).IsAssignableFrom(comp.GetType()))
                {
                    // 正确的 T 类型组件，删除
                    GameObject.DestroyImmediate(comp);
                }
            }
        }

        private static void RemoveIfExists<T>(GameObject go) where T : Component
        {
            RemoveAllComponentsOfType<T>(go);
        }

        private static int RemoveLegacyNpcInteractables(GameObject root)
        {
#pragma warning disable 0618
            NPCInteractable[] legacyComponents = root.GetComponentsInChildren<NPCInteractable>(true);
#pragma warning restore 0618

            int removedCount = 0;
            for (int i = 0; i < legacyComponents.Length; i++)
            {
                var legacy = legacyComponents[i];
                if (legacy == null)
                {
                    continue;
                }

                GameObject.DestroyImmediate(legacy);
                removedCount++;
            }

            return removedCount;
        }

        private static ActorIdentitySeedData BuildIdentitySeed(NPCDefinition def)
        {
            return new ActorIdentitySeedData
            {
                id = def.npcId,
                displayName = def.npcName,
                dialogLines = def.dialogLines,
                function = MapFunction(def.function),
                functionButtonText = def.functionButtonText,
                defaultShopCatalog = def.defaultShopCatalog,
                animatorController = def.animatorController,
                modelPrefab = def.modelPrefab,
                modelLocalPosition = def.modelLocalPosition,
                modelLocalEuler = def.modelLocalEuler,
                modelLocalScale = def.modelLocalScale
            };
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
    }
}
