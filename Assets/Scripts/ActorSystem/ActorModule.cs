using UnityEngine;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// ActorModule: Actor 能力模块的基类
    /// Phase 7: 所有模块继承此基类，提供统一的接口。
    /// - 提供启用/禁用方法
    /// - 提供与 Actor 和 Memory 的通信接口
    /// </summary>
    public abstract class ActorModule : MonoBehaviour
    {
        protected Actor _actor;
        protected ActorIdentity _identity;
        protected ActorMemory _memory;

        /// <summary>
        /// 模块是否已启用
        /// </summary>
        public bool IsEnabled { get; protected set; } = false;

        /// <summary>
        /// Phase 7: 初始化模块，缓存必要的组件引用
        /// </summary>
        protected virtual void Awake()
        {
            _actor = GetComponent<Actor>();
            _identity = GetComponent<ActorIdentity>();
            _memory = GetComponent<ActorMemory>();
        }

        /// <summary>
        /// Phase 7: 启用模块
        /// </summary>
        public virtual void Enable()
        {
            IsEnabled = true;
            OnEnabled();
            Debug.Log($"[{GetType().Name}] 已启用");
        }

        /// <summary>
        /// Phase 7: 禁用模块
        /// </summary>
        public virtual void Disable()
        {
            IsEnabled = false;
            OnDisabled();
            Debug.Log($"[{GetType().Name}] 已禁用");
        }

        /// <summary>
        /// Phase 7: 启用时的回调（子类可重写）
        /// </summary>
        protected virtual void OnEnabled()
        {
        }

        /// <summary>
        /// Phase 7: 禁用时的回调（子类可重写）
        /// </summary>
        protected virtual void OnDisabled()
        {
        }
    }
}
