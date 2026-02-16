using UnityEngine;
using System.Collections.Generic;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// ActorDialogue: 负责"说什么"
    /// Phase 5: 根据记忆状态返回当前台词（使用 DialogueResolver）。
    /// - 从 ActorIdentity 获取默认对话（作为 fallback）
    /// - 根据好感度、标记返回不同对话
    /// - 提供给 UI 的接口
    /// </summary>
    public class ActorDialogue : MonoBehaviour
    {
        private Actor _actor;
        private ActorIdentity _identity;
        private ActorMemory _memory;
        private DialogueResolver _resolver;

        private void Awake()
        {
            _actor = GetComponent<Actor>();
            _identity = GetComponent<ActorIdentity>();
            _memory = GetComponent<ActorMemory>();
            _resolver = GetComponent<DialogueResolver>();
        }

        /// <summary>
        /// Phase 5: 根据记忆状态返回当前台词（通过 DialogueResolver）
        /// </summary>
        public List<string> GetCurrentLines()
        {
            if (_identity == null)
            {
                Debug.LogWarning("[ActorDialogue] ActorIdentity 缺失，无法获取对话");
                return new List<string> { "(……)" };
            }

            // Phase 5: 使用 DialogueResolver 获取当前对话
            string current = _resolver?.GetCurrentDialogue();
            if (!string.IsNullOrEmpty(current))
            {
                return new List<string> { current };
            }

            // 回退到默认对话
            List<string> defaultLines = _identity.GetDefaultDialog();
            return defaultLines ?? new List<string> { "(……)" };
        }

        /// <summary>
        /// Phase 5: 记录对话事件到 Memory
        /// </summary>
        public void OnDialogueStarted()
        {
            if (_memory != null)
            {
                _memory.RecordEvent(ActorMemory.EventType.MetPlayer);

                if (!_memory.HasCompletedFirstDialogue)
                {
                    _memory.RecordEvent(ActorMemory.EventType.FirstDialogue);
                }
            }
        }
    }
}
