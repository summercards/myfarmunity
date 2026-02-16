using UnityEngine;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// ActorView: 负责“表现层”
    /// - 动画控制 (Animator)
    /// - 模型挂载
    /// - 视觉特效
    /// </summary>
    public class ActorView : MonoBehaviour
    {
        public Animator Animator { get; set; }
        public Transform ModelRoot { get; set; }

        // Phase 1: 暂时只作引用持有
    }
}
