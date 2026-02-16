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
        // 内部存储的身份数据（可序列化）
        [SerializeField] private string _id;
        [SerializeField] private string _name;
        [SerializeField] private List<string> _dialogLines;
        [SerializeField] private NPCFunction _function;
        [SerializeField] private string _functionButtonText;
        [SerializeField] private ShopCatalogSO _defaultShopCatalog;
        
        // 动画控制器和模型相关的设置 (用于 ActorView 初始化)
        [SerializeField] private RuntimeAnimatorController _animatorController;
        [SerializeField] private GameObject _modelPrefab;
        [SerializeField] private Vector3 _modelLocalPosition;
        [SerializeField] private Vector3 _modelLocalEuler;
        [SerializeField] private Vector3 _modelLocalScale;

        // 初始化标记（可序列化）
        [SerializeField] private bool _isInitialized = false;

        // 公开属性（只读）
        public string Id => _id;
        public string Name => _name;
        public List<string> DialogLines => _dialogLines ?? new List<string>();
        public NPCFunction Function => _function;
        public string FunctionButtonText => _functionButtonText;
        public ShopCatalogSO DefaultShopCatalog => _defaultShopCatalog;
        public RuntimeAnimatorController AnimatorController => _animatorController;
        public GameObject ModelPrefab => _modelPrefab;
        public Vector3 ModelLocalPosition => _modelLocalPosition;
        public Vector3 ModelLocalEuler => _modelLocalEuler;
        public Vector3 ModelLocalScale => _modelLocalScale;
        public bool IsInitialized => _isInitialized;

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

            _id = def.npcId;
            _name = def.npcName;
            _dialogLines = new List<string>(def.dialogLines); // 复制一份，避免直接引用 Definition
            _function = def.function;
            _functionButtonText = def.functionButtonText;
            _defaultShopCatalog = def.defaultShopCatalog;
            
            _animatorController = def.animatorController;
            _modelPrefab = def.modelPrefab;
            _modelLocalPosition = def.modelLocalPosition;
            _modelLocalEuler = def.modelLocalEuler;
            _modelLocalScale = def.modelLocalScale;

            _isInitialized = true;

            Debug.Log($"[ActorIdentity] {_name} 初始化完成（从 {def.npcId}）。");
        }

        /// <summary>
        /// Phase 3: 获取默认对话（用于 ActorDialogue 的 fallback）
        /// </summary>
        public List<string> GetDefaultDialog()
        {
            return DialogLines;
        }
    }
}
