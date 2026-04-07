using UnityEngine;
using System.Collections.Generic;

namespace FarmGame.ActorSystem
{
    [System.Serializable]
    public sealed class ActorIdentitySeedData
    {
        public string id;
        public string displayName;
        public List<string> dialogLines;
        public ActorFunction function;
        public string functionButtonText;
        public ShopCatalogSO defaultShopCatalog;
        public RuntimeAnimatorController animatorController;
        public GameObject modelPrefab;
        public Vector3 modelLocalPosition;
        public Vector3 modelLocalEuler;
        public Vector3 modelLocalScale = Vector3.one;
    }

    /// <summary>
    /// ActorIdentity: 负责"我是谁"，存储角色的静态身份数据。
    /// Phase 4: 通过 ActorIdentitySeedData 初始化，避免运行时直接依赖旧 NPCSystem。
    /// </summary>
    [DisallowMultipleComponent]
    public class ActorIdentity : MonoBehaviour
    {
        // 内部存储的身份数据（可序列化）
        [SerializeField] private string _id;
        [SerializeField] private string _name;
        [SerializeField] private List<string> _dialogLines;
        [SerializeField] private ActorFunction _function;
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
        public ActorFunction Function => _function;
        public string FunctionButtonText => _functionButtonText;
        public ShopCatalogSO DefaultShopCatalog => _defaultShopCatalog;
        public RuntimeAnimatorController AnimatorController => _animatorController;
        public GameObject ModelPrefab => _modelPrefab;
        public Vector3 ModelLocalPosition => _modelLocalPosition;
        public Vector3 ModelLocalEuler => _modelLocalEuler;
        public Vector3 ModelLocalScale => _modelLocalScale;
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 在构建或加载时调用，使用 ActorSystem 自有 seed 初始化身份数据。
        /// </summary>
        public void Initialize(ActorIdentitySeedData seed)
        {
            if (seed == null)
            {
                Debug.LogError("[ActorIdentity] 初始化失败：ActorIdentitySeedData 为空。");
                return;
            }

            _id = string.IsNullOrWhiteSpace(seed.id) ? gameObject.name : seed.id;
            _name = string.IsNullOrWhiteSpace(seed.displayName) ? _id : seed.displayName;
            _dialogLines = seed.dialogLines != null
                ? new List<string>(seed.dialogLines)
                : new List<string>();
            _function = seed.function;
            _functionButtonText = seed.functionButtonText;
            _defaultShopCatalog = seed.defaultShopCatalog;

            _animatorController = seed.animatorController;
            _modelPrefab = seed.modelPrefab;
            _modelLocalPosition = seed.modelLocalPosition;
            _modelLocalEuler = seed.modelLocalEuler;
            _modelLocalScale = seed.modelLocalScale == Vector3.zero ? Vector3.one : seed.modelLocalScale;

            _isInitialized = true;

            Debug.Log($"[ActorIdentity] {_name} 初始化完成（seed: {_id}）。");
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
