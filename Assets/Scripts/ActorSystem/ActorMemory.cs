using UnityEngine;
using System;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// ActorMemory: 负责"记得什么”
    /// Phase 4: 实现运行时状态和事件记录。
    /// - 玩家关系（是否见过、好感度、今日交互次数）
    /// - 标记系统（已完成对话、任务状态等）
    /// - 事件记录（时间戳日志）
    /// </summary>
    public class ActorMemory : MonoBehaviour
    {
        // ========== 玩家关系 ==========
        /// <summary> 是否见过玩家 </summary>
        public bool HasMetPlayer { get; private set; } = false;

        /// <summary> 好感度 (0-100) </summary>
        public int Friendship { get; private set; } = 0;

        /// <summary> 今日交互次数 </summary>
        public int InteractionsToday { get; private set; } = 0;

        /// <summary> 上次交互日期（用于重置每日计数） </summary>
        private int _lastInteractionDate = -1;

        // ========== 标记系统 ==========
        /// <summary> 是否已完成首次对话 </summary>
        public bool HasCompletedFirstDialogue { get; set; } = false;

        /// <summary> 是否已解锁商店（对商店型 NPC） </summary>
        public bool HasUnlockedShop { get; set; } = false;

        /// <summary> 通用标记系统 </summary>
        public string Flags { get; private set; } = "";

        // ========== 事件记录 ==========
        /// <summary> 事件类型 </summary>
        public enum EventType
        {
            MetPlayer,
            FirstDialogue,
            ShopOpened,
            QuestStarted,
            QuestCompleted
        }

        /// <summary> 记录事件 </summary>
        public void RecordEvent(EventType type)
        {
            // Phase 4: 简单实现，未来可扩展为完整日志
            switch (type)
            {
                case EventType.MetPlayer:
                    HasMetPlayer = true;
                    IncrementInteractions();
                    break;

                case EventType.FirstDialogue:
                    HasCompletedFirstDialogue = true;
                    break;

                case EventType.ShopOpened:
                    HasUnlockedShop = true;
                    break;
            }
        }

        /// <summary> 记录玩家相遇 </summary>
        public void MeetPlayer()
        {
            if (!HasMetPlayer)
            {
                HasMetPlayer = true;
                IncrementInteractions();
                Debug.Log($"[ActorMemory] 玩家首次相遇");
            }
        }

        /// <summary> 增加好感度 </summary>
        public void AddFriendship(int amount)
        {
            Friendship = Mathf.Clamp(Friendship + amount, 0, 100);
            Debug.Log($"[ActorMemory] 好感度变化：{Friendship - amount} → {Friendship}");
        }

        /// <summary> 检查是否认识玩家 </summary>
        public bool KnowsPlayer()
        {
            return HasMetPlayer;
        }

        /// <summary> 检查好感度等级 </summary>
        public FriendshipLevel GetFriendshipLevel()
        {
            if (Friendship >= 80) return FriendshipLevel.BestFriend;
            if (Friendship >= 60) return FriendshipLevel.CloseFriend;
            if (Friendship >= 40) return FriendshipLevel.Friend;
            if (Friendship >= 20) return FriendshipLevel.Acquaintance;
            return FriendshipLevel.Stranger;
        }

        /// <summary> 设置标记 </summary>
        public void SetFlag(string flag, bool value = true)
        {
            if (string.IsNullOrEmpty(flag)) return;

            if (value)
            {
                if (string.IsNullOrEmpty(Flags)) Flags = flag;
                else Flags += "," + flag;
            }
            else
            {
                var flagList = new System.Collections.Generic.List<string>(Flags.Split(','));
                flagList.Remove(flag);
                Flags = string.Join(",", flagList.ToArray());
            }
        }

        /// <summary> 检查标记 </summary>
        public bool HasFlag(string flag)
        {
            if (string.IsNullOrEmpty(Flags)) return false;
            return Flags.Contains(flag);
        }

        /// <summary> 增加交互次数（带日期检查） </summary>
        private void IncrementInteractions()
        {
            int currentDate = DateTime.Now.DayOfYear;
            if (currentDate != _lastInteractionDate)
            {
                // 新的一天，重置计数
                InteractionsToday = 1;
                _lastInteractionDate = currentDate;
            }
            else
            {
                InteractionsToday++;
            }
        }

        /// <summary> 获取今日交互次数（用于限制每日对话） </summary>
        public int GetDailyInteractions()
        {
            // 检查是否是新的一天
            int currentDate = DateTime.Now.DayOfYear;
            if (currentDate != _lastInteractionDate)
            {
                return 0;
            }
            return InteractionsToday;
        }

        /// <summary> 好感度等级 </summary>
        public enum FriendshipLevel
        {
            Stranger,      // 0-19
            Acquaintance, // 20-39
            Friend,        // 40-59
            CloseFriend,    // 60-79
            BestFriend     // 80-100
        }
    }
}
