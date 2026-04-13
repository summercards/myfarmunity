using System;
using UnityEngine;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// ActorTickModule: 低频 tick 调度模块
    /// v1.1: 建立独立的调度能力，让周期逻辑统一走低频 tick，而不是新增大量 Update
    /// - 支持 TickInterval 配置（默认 1 秒）
    /// - 支持 UseUnscaledTime 配置（默认 false）
    /// - 启用时注册重复调度，禁用或销毁时可靠取消
    /// - 避免 Update 的频繁调用，降低性能开销
    /// </summary>
    public class ActorTickModule : ActorModule
    {
        [Header("调度配置")]
        [Tooltip("Tick 周期（秒），默认 1 秒")]
        [Range(0.01f, 60f)]
        public float tickInterval = 1f;

        [Tooltip("是否使用未缩放时间（不受 Time.timeScale 影响）")]
        public bool useUnscaledTime = false;

        /// <summary>
        /// TaskManager 的任务句柄，用于取消调度
        /// </summary>
        private FarmGame.Core.TaskManager.ITaskHandle _taskHandle;

        /// <summary>
        /// SkillModule 引用
        /// </summary>
        private Skills.SkillModule _skillModule;

        /// <summary>
        /// v1.1: 启用模块
        /// </summary>
        protected override void OnEnabled()
        {
            base.OnEnabled();

            // 获取 SkillModule 引用
            _skillModule = GetComponent<Skills.SkillModule>();

            // 注册重复调度
            _taskHandle = FarmGame.Core.TaskManager.Instance?.ScheduleRepeating(
                intervalSeconds: tickInterval,
                callback: OnTick,
                useUnscaledTime: useUnscaledTime
            );

            Debug.Log($"[ActorTickModule] 已启用，Tick 周期：{tickInterval} 秒，未缩放时间：{useUnscaledTime}");
        }

        /// <summary>
        /// v1.1: 禁用模块
        /// </summary>
        protected override void OnDisabled()
        {
            base.OnDisabled();

            // 取消调度
            CancelSchedule();

            Debug.Log($"[ActorTickModule] 已禁用，已取消调度");
        }

        /// <summary>
        /// 销毁时确保调度已取消
        /// </summary>
        private void OnDestroy()
        {
            CancelSchedule();
        }

        /// <summary>
        /// 取消调度
        /// </summary>
        private void CancelSchedule()
        {
            if (_taskHandle != null && !_taskHandle.IsCancelled)
            {
                _taskHandle.Cancel();
                _taskHandle = null;
                Debug.Log($"[ActorTickModule] 已取消调度");
            }
        }

        /// <summary>
        /// Tick 回调（由 TaskManager 调用）
        /// </summary>
        private void OnTick()
        {
            if (!IsEnabled)
            {
                return;
            }

            // 调用 SkillModule.Tick，让技能冷却通过低频调度刷新
            if (_skillModule != null && _skillModule.IsEnabled)
            {
                _skillModule.Tick(tickInterval);
            }
        }

        /// <summary>
        /// 检查是否正在调度
        /// </summary>
        public bool IsScheduling
        {
            get { return _taskHandle != null && !_taskHandle.IsCancelled; }
        }

        /// <summary>
        /// 获取当前 Tick 周期
        /// </summary>
        public float TickInterval => tickInterval;

        /// <summary>
        /// 获取当前 Tick 模式
        /// </summary>
        public string TickMode => useUnscaledTime ? "未缩放时间" : "缩放时间";
    }
}
