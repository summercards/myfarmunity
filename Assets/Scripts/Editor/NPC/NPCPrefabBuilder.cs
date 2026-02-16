using UnityEngine;
using UnityEditor;
using System.IO;
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
                var rb = Ensure<Rigidbody>(root);
                rb.isKinematic = true; 
                rb.useGravity = false;

                var col = Ensure<CapsuleCollider>(root);
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
            // 2. 架构分流：新 Actor 系统
            // ==========================================

            if (def.useActorSystem)
            {
                // --- 新架构 Actor ---
                
                // Phase 8 修复：不调用 RemoveLegacyComponents()，因为旧系统文件已删除
                // 直接添加新系统组件

                Ensure<Actor>(root);

                // Phase 3: 使用 Initialize 方法
                var identity = Ensure<ActorIdentity>(root);
                identity.Initialize(def);

                Ensure<ActorMemory>(root);
                Ensure<ActorBrain>(root);
                Ensure<ActorDialogue>(root);
                Ensure<DialogueResolver>(root);

                // 交互组件
                if (def.enableInteraction)
                {
                    Ensure<ActorInteraction>(root);
                }
                else
                {
                    RemoveIfExists<ActorInteraction>(root);
                }

                // 模块化系统
                switch (def.function)
                {
                    case NPCFunction.OpenShop:
                        Ensure<ShopModule>(root);
                        break;

                    case NPCFunction.Talk:
                        Ensure<DialogueModule>(root);
                        break;

                    case NPCFunction.Quest:
                        Ensure<QuestModule>(root);
                        break;

                    case NPCFunction.Gift:
                        Ensure<GiftModule>(root);
                        break;

                    case NPCFunction.None:
                    default:
                        // 不添加任何模块
                        break;
                }

                // 視觉组件
                if (def.enableVisuals)
                {
                    var animator = Ensure<Animator>(root);
                    animator.runtimeAnimatorController = def.animatorController;

                    var view = Ensure<ActorView>(root);
                    view.Animator = animator;
                    view.ModelRoot = visual;
                }
                else
                {
                    RemoveIfExists<Animator>(root);
                    RemoveIfExists<ActorView>(root);
                }
            }
            else
            {
                // Phase 8: 保留向后兼容，但不添加旧系统组件
                // 只添加必要的标记，让 Editor 知道这是旧版 NPC
                
                // 不添加任何 NPC 前缀组件
                // 如果用户需要旧版功能，可以从版本历史恢复
            }
        }

        private static T Ensure<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        private static void RemoveIfExists<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c != null)
                GameObject.DestroyImmediate(c);
        }

        // Phase 8: 清理方法（仅保留必要的）
        
        /// <summary>
        /// 移除旧系统组件（仅包含仍存在的类型）
        /// Phase 8 修复：移除对已删除类型的引用
        /// </summary>
        private static void RemoveLegacyComponents(GameObject go)
        {
            // Phase 8: 不添加任何删除调用，因为旧系统文件已被删除
            // 如果预制体中仍有旧组件引用，会在重新生成时自动清除
        }
    }
}
