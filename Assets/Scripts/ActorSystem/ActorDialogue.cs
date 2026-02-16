using UnityEngine;
using System.Collections.Generic;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// ActorDialogue: 负责"说什么”
    /// Phase 4: 根据 Memory 决定当前台词。
    /// - 从 ActorIdentity 获取默认对话（作为 fallback）
    /// - 根据好感度、标记返回不同对话
    /// - 提供给 UI 的接口
    /// </summary>
    public class ActorDialogue : MonoBehaviour
    {
        private Actor _actor;
        private ActorIdentity _identity;
        private ActorMemory _memory;

        private void Awake()
        {
            _actor = GetComponent<Actor>();
            _identity = GetComponent<ActorIdentity>();
            _memory = GetComponent<ActorMemory>();
        }

        /// <summary>
        /// Phase 4: 根据记忆状态返回当前台词
        /// </summary>
        public List<string> GetCurrentLines()
        {
            if (_identity == null)
            {
                Debug.LogWarning("[ActorDialogue] ActorIdentity 缺失，无法获取对话");
                return new List<string> { "(……)" };
            }

            // Phase 4: 根据好感度选择对话
            if (_memory != null)
            {
                var friendshipLevel = _memory.GetFriendshipLevel();

                switch (friendshipLevel)
                {
                    case ActorMemory.FriendshipLevel.Stranger:
                        return GetLinesWithTag("stranger") ?? _identity.GetDefaultDialog();

                    case ActorMemory.FriendshipLevel.Acquaintance:
                        return GetLinesWithTag("acquaintance") ?? GetLinesWithTag("stranger") ?? _identity.GetDefaultDialog();

                    case ActorMemory.FriendshipLevel.Friend:
                        return GetLinesWithTag("friend") ?? _identity.GetDefaultDialog();

                    case ActorMemory.FriendshipLevel.CloseFriend:
                        return GetLinesWithTag("close_friend") ?? GetLinesWithTag("friend") ?? _identity.GetDefaultDialog();

                    case ActorMemory.FriendshipLevel.BestFriend:
                        return GetLinesWithTag("best_friend") ?? GetLinesWithTag("close_friend") ?? _identity.GetDefaultDialog();
                }
            }

            // Fallback: 返回默认对话
            return _identity.GetDefaultDialog();
        }

        /// <summary>
        /// Phase 4: 根据标签获取对话行（从默认对话列表中筛选）
        /// 未来可在 NPCDefinition 中支持多组对话（按好感度分级）
        /// </summary>
        private List<string> GetLinesWithTag(string tag)
        {
            // Phase 4: 简化实现，未来可从 Definition 的结构化对话中筛选
            // 当前只返回 null，让系统回退到默认对话
            return null;
        }

        /// <summary>
        /// Phase 4: 记录对话事件到 Memory
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
