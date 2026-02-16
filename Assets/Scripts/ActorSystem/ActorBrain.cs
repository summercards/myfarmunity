using UnityEngine;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// ActorBrain: 负责“正在做什么”
    /// - 状态机（Idle, Talking, Working, Sleep）
    /// - AI 决策
    /// </summary>
    public class ActorBrain : MonoBehaviour
    {
        // Phase 6 将实现状态机
        public enum State { Idle, Talking, Busy, Cutscene }
        public State CurrentState { get; private set; } = State.Idle;
    }
}
