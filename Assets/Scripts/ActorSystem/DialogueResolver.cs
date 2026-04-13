using UnityEngine;
using System.Collections.Generic;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// DialogueResolver: 负责"说什么"的核心逻辑
    /// Phase 5: 根据记忆、时间、条件返回最合适的对话。
    /// - v1.1 扩展：支持台词分组（first_meet, daily, stranger, friend 等）
    /// - 优先使用特定标记的对话
    /// - 其次根据好感度选择对话
    /// - 最后回退到默认对话（NPCDefinition.dialogLines）
    /// </summary>
    public class DialogueResolver : MonoBehaviour
    {
        [Header("台词分组（v1.1 扩展）")]
        [Tooltip("可选的台词分组资产，支持首次会面、好感度分级等分组规则")]
        public DialogueSetSO dialogueSet;

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
        /// Phase 5 + v1.1: 获取当前最合适的对话
        /// v1.1 扩展：支持台词分组选择规则
        /// </summary>
        public string GetCurrentDialogue()
        {
            if (_identity == null)
            {
                Debug.LogWarning("[DialogueResolver] ActorIdentity 缺失");
                return "(……)";
            }

            // v1.1 步骤 1: 首次会面优先
            if (dialogueSet != null && _memory != null && !_memory.HasCompletedFirstDialogue)
            {
                string dialogue = GetDialogueFromGroup("first_meet");
                if (!string.IsNullOrEmpty(dialogue))
                {
                    return dialogue;
                }
            }

            // v1.1 步骤 2: 按好感度分级
            if (dialogueSet != null && _memory != null)
            {
                string friendshipKey = GetFriendshipKey(_memory.Friendship);
                string dialogue = GetDialogueFromGroup(friendshipKey);
                if (!string.IsNullOrEmpty(dialogue))
                {
                    return dialogue;
                }
            }

            // v1.1 步骤 3: 日常回退
            if (dialogueSet != null)
            {
                string dialogue = GetDialogueFromGroup("daily");
                if (!string.IsNullOrEmpty(dialogue))
                {
                    return dialogue;
                }
            }

            // v1.1 步骤 4: 旧逻辑回退
            // 1. 优先检查标记系统（Phase 4 实现）
            if (_memory != null && CheckFlaggedDialogue())
            {
                return _flaggedDialogue;
            }

            // 2. 根据好感度选择对话
            if (_memory != null)
            {
                string friendshipDialogue = GetDialogueByFriendship();
                if (!string.IsNullOrEmpty(friendshipDialogue))
                {
                    return friendshipDialogue;
                }
            }

            // 3. 回退到默认对话
            List<string> defaultLines = _identity.GetDefaultDialog();
            if (defaultLines != null && defaultLines.Count > 0)
            {
                // 简单循环返回（Phase 5 可扩展为随机）
                int index = Random.Range(0, defaultLines.Count);
                return defaultLines[index];
            }

            return "(……)";
        }

        /// <summary>
        /// Phase 5: 检查是否有标记的对话
        /// 未来可在 NPCDefinition 中支持多组对话（如 first_greeting, repeat_dialog）
        /// </summary>
        private string _flaggedDialogue;
        private bool _hasCheckedFlaggedDialogue;

        private bool CheckFlaggedDialogue()
        {
            // Phase 5: 简化实现，未来可扩展
            // 示例：如果首次见面，使用特殊对话
            if (!_hasCheckedFlaggedDialogue && _memory != null)
            {
                if (_memory.HasFlag("first_meeting_dialog"))
                {
                    _flaggedDialogue = "你好！很高兴认识你。";
                    return true;
                }

                // 标记已检查，避免重复查找
                _hasCheckedFlaggedDialogue = true;
            }

            return false;
        }

        /// <summary>
        /// Phase 5: 根据好感度选择对话
        /// 未来可在 NPCDefinition 中支持多组对话（按好感度分级）
        /// </summary>
        private string GetDialogueByFriendship()
        {
            if (_memory == null)
            {
                return null;
            }

            var level = _memory.GetFriendshipLevel();
            var defaultLines = _identity.GetDefaultDialog();

            if (defaultLines == null || defaultLines.Count == 0)
            {
                return null;
            }

            // Phase 5: 简化实现 - 检查对话行是否包含标记
            // 未来可在 NPCDefinition 中支持多组对话数据结构
            // 例如：dialogLinesStranger = [...], dialogLinesFriend = [...]

            // 临时实现：返回第一条不同台词
            if (defaultLines.Count >= 2)
            {
                if (level >= ActorMemory.FriendshipLevel.Friend)
                {
                    return defaultLines[0]; // 朋友说第一句
                }
                else if (level >= ActorMemory.FriendshipLevel.Acquaintance)
                {
                    return defaultLines[1]; // 认识人说第二句
                }
            }

            return defaultLines[0]; // 默认返回第一句
        }

        /// <summary>
        /// Phase 5: 添加标记检查（供外部调用）
        /// </summary>
        public void SetFlaggedDialogue(string dialogue)
        {
            _flaggedDialogue = dialogue;
        }

        /// <summary>
        /// v1.1: 将好感度映射到分组 key
        /// </summary>
        private string GetFriendshipKey(float friendship)
        {
            if (friendship >= 80) return "close_friend";
            if (friendship >= 50) return "friend";
            if (friendship >= 20) return "acquaintance";
            return "stranger";
        }

        /// <summary>
        /// v1.1: 从分组中随机选择一句台词
        /// </summary>
        private string GetDialogueFromGroup(string groupKey)
        {
            if (dialogueSet == null || dialogueSet.groups == null)
            {
                return null;
            }

            var group = dialogueSet.groups.Find(g => g.key == groupKey);
            if (group == null || group.lines == null || group.lines.Count == 0)
            {
                return null;
            }

            // 边界情况：防止索引越界（lines 被外部清空）
            if (group.lines.Count == 0)
            {
                return null;
            }

            // 随机选择一句台词
            int index = Random.Range(0, group.lines.Count);
            return group.lines[index];
        }
    }
}
