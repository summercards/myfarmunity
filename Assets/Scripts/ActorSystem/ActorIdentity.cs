using UnityEngine;
using System.Collections.Generic;
using FarmGame.NPCSystem;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// ActorIdentity: 负责"我是谁"，存储角色的静态身份数据。
    /// Phase 3: 从 NPCDefinition 初始化，之后独立运行。
    /// </summary>
    [DisallowMultipleComponent]
    public class ActorIdentity : MonoBehaviour
    {
        // 内部存储的身份数据
        public string Id { get; private set; }
        public string Name { get; private set; }
        public List<string> DialogLines { get; private set; }
        public NPCFunction Function { get; private set; }
        public string FunctionButtonText { get; private set; }
        public ShopCatalogSO DefaultShopCatalog { get; private set; }
        
        // 动画控制器和模型相关的设置 (用于 ActorView 初始化)
        public RuntimeAnimatorController AnimatorController { get; private set; }
        public GameObject ModelPrefab { get; private set; }
        public Vector3 ModelLocalPosition { get; private set; }
        public Vector3 ModelLocalEuler { get; private set; }
        public Vector3 ModelLocalScale { get; private set; }

        /// <summary>
        /// Phase 3: 标记是否已从 Definition 初始化
        /// </summary>
        public bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Phase 3: 在构建或加载时调用，从 NPCDefinition 初始化身份数据。
        /// 初始化后，ActorIdentity 完全独立运行，不再依赖 NPCDefinition。
        /// </summary>
        public void Initialize(NPCDefinition def)
        {
            if (def == null)
            {
                Debug.LogError("[ActorIdentity] 初始化失败：NPCDefinition 为空。");
                return;
            }

            Id = def.npcId;
            Name = def.npcName;
            DialogLines = new List<string>(def.dialogLines); // 复制一份，避免直接引用 Definition
            Function = def.function;
            FunctionButtonText = def.functionButtonText;
            DefaultShopCatalog = def.defaultShopCatalog;
            
            AnimatorController = def.animatorController;
            ModelPrefab = def.modelPrefab;
            ModelLocalPosition = def.modelLocalPosition;
            ModelLocalEuler = def.modelLocalEuler;
            ModelLocalScale = def.modelLocalScale;

            IsInitialized = true;

            Debug.Log($"[ActorIdentity] {Name} 初始化完成（从 {def.npcId}）。");
        }

        /// <summary>
        /// Phase 3: 获取默认对话（用于 ActorDialogue 的 fallback）
        /// </summary>
        public List<string> GetDefaultDialog()
        {
            return DialogLines ?? new List<string>();
        }
    }
}
