using UnityEngine;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// ActorBrain: 负责"正在做什么"
    /// Phase 6: 实现状态机（Idle, Talking, Busy, Cutscene）。
    /// - 状态转换规则
    /// - 与 ActorMemory 协同记录状态变化
    /// </summary>
    public class ActorBrain : MonoBehaviour
    {
        /// <summary>
        /// Actor 状态枚举
        /// </summary>
        public enum State
        {
            Idle,           // 闲散：默认状态，可以交互
            Talking,        // 对话中：正在与玩家交流
            Busy,           // 忙碌：执行某项任务（购物、任务等）
            Cutscene        // 剧情：过场动画中，不可交互
        }

        /// <summary> 当前状态 </summary>
        public State CurrentState { get; private set; } = State.Idle;

        /// <summary> 状态变化事件 </summary>
        public System.Action<State> OnStateChanged;

        private Actor _actor;
        private ActorMemory _memory;

        // 状态计时
        private float _stateStartTime;

        void Awake()
        {
            _actor = GetComponent<Actor>();
            _memory = GetComponent<ActorMemory>();
            _stateStartTime = Time.time;
        }

        /// <summary>
        /// Phase 6: 切换到指定状态
        /// </summary>
        public void ChangeState(State newState)
        {
            if (CurrentState == newState) return;

            State oldState = CurrentState;
            float oldStateDuration = Time.time - _stateStartTime;

            CurrentState = newState;
            _stateStartTime = Time.time;

            Debug.Log($"[ActorBrain] {_actor?.Identity?.Name ?? "Unknown"} 状态变化：{oldState} → {newState}");

            // 记录到 Memory
            if (_memory != null)
            {
                switch (newState)
                {
                    case State.Talking:
                        // 进入对话：此处无需逐帧计时，离开时用 stateStartTime 差值计算即可
                        break;

                    case State.Idle:
                    case State.Busy:
                    case State.Cutscene:
                        // 记录状态结束时间
                        if (oldState == State.Talking)
                        {
                            Debug.Log($"[ActorBrain] 对话持续时间：{oldStateDuration:F2}秒");
                        }
                        break;
                }
            }

            // 触发事件
            OnStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Phase 6: 检查当前状态持续时间
        /// </summary>
        public float GetStateDuration()
        {
            return Time.time - _stateStartTime;
        }

        /// <summary>
        /// Phase 6: 切换到对话状态
        /// </summary>
        public void StartTalking()
        {
            ChangeState(State.Talking);
        }

        /// <summary>
        /// Phase 6: 结束对话，返回空闲
        /// </summary>
        public void EndTalking()
        {
            if (CurrentState == State.Talking)
            {
                ChangeState(State.Idle);
            }
        }

        /// <summary>
        /// Phase 6: 切换到忙碌状态
        /// </summary>
        public void SetBusy(string reason = "")
        {
            ChangeState(State.Busy);
            if (!string.IsNullOrEmpty(reason))
            {
                Debug.Log($"[ActorBrain] {_actor?.Identity?.Name ?? "Unknown"} 进入忙碌状态：{reason}");
            }
        }

        /// <summary>
        /// Phase 6: 从忙碌状态恢复
        /// </summary>
        public void ClearBusy()
        {
            if (CurrentState == State.Busy)
            {
                ChangeState(State.Idle);
                Debug.Log($"[ActorBrain] {_actor?.Identity?.Name ?? "Unknown"} 忙碌状态结束");
            }
        }

        /// <summary>
        /// Phase 6: 进入过场状态
        /// </summary>
        public void EnterCutscene()
        {
            ChangeState(State.Cutscene);
            Debug.Log($"[ActorBrain] {_actor?.Identity?.Name ?? "Unknown"} 进入过场");
        }

        /// <summary>
        /// Phase 6: 退出过场状态
        /// </summary>
        public void ExitCutscene()
        {
            if (CurrentState == State.Cutscene)
            {
                ChangeState(State.Idle);
                Debug.Log($"[ActorBrain] {_actor?.Identity?.Name ?? "Unknown"} 退出过场");
            }
        }

        /// <summary>
        /// Phase 6: 检查是否可以交互
        /// </summary>
        public bool CanInteract()
        {
            return CurrentState == State.Idle;
        }
    }
}
