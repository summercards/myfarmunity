using UnityEngine;
using System.Collections.Generic;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// QuestModule: 负责"任务"能力模块
    /// Phase 7: 段理任务系统，与 ActorMemory 交互。
    /// - 任务状态管理
    /// - 任务完成通知
    /// - 与 ActorMemory 交互（记录任务事件）
    /// - 修复编译错误：移除所有对 UI 的直接调用，改为动态查找或移除
    /// </summary>
    public class QuestModule : ActorModule
    {
        [System.Serializable]
        public class QuestData
        {
            public string questId;
            public string questName;
            public string description;
            public List<string> objectives;
            public int experienceReward;
            public List<string> itemRewards;
        }

        [Header("Quest Configuration")]
        public List<QuestData> availableQuests = new List<QuestData>();

        [Header("Debug")]
        [Tooltip("显示调试日志")]
        public bool showDebugLogs = true;

        // 内部状态
        private QuestData _currentQuest;
        private int _currentObjectiveIndex = 0;
        private bool _isQuestActive;

        /// <summary>
        /// Phase 7: 初始化任务系统
        /// </summary>
        public void InitializeQuests()
        {
            if (availableQuests == null || availableQuests.Count == 0)
            {
                Debug.LogWarning("[QuestModule] 没有可用的任务");
                return;
            }

            if (showDebugLogs) Debug.Log($"[QuestModule] 已初始化 {availableQuests.Count} 个任务");
        }

        /// <summary>
        /// Phase 7: 开始任务
        /// </summary>
        public void StartQuest(string questId)
        {
            var quest = availableQuests.Find(q => q.questId == questId);
            if (quest == null)
            {
                if (showDebugLogs) Debug.LogWarning($"[QuestModule] 找不到任务 ID: {questId}");
                return;
            }

            _currentQuest = quest;
            _currentObjectiveIndex = 0;
            _isQuestActive = true;

            // Phase 7: 记录任务开始到 Memory
            if (_memory != null)
            {
                _memory.RecordEvent(ActorMemory.EventType.QuestStarted);
            }

            if (showDebugLogs) Debug.Log($"[QuestModule] 任务已开始：{quest.questName}");
        }

        /// <summary>
        /// Phase 7: 完成当前任务目标
        /// </summary>
        public void CompleteObjective()
        {
            if (!_isQuestActive)
            {
                if (showDebugLogs) Debug.LogWarning("[QuestModule] 当前没有激活的任务");
                return;
            }

            _currentObjectiveIndex++;

            // Phase 7: 检查是否所有目标都完成
            if (_currentObjectiveIndex >= _currentQuest.objectives.Count)
            {
                CompleteQuest();
            }
            else
            {
                // Phase 7: 修复：移除 UI 通知调用
                if (showDebugLogs)
                {
                    Debug.Log($"[QuestModule] 目标进度：{_currentObjectiveIndex}/{_currentQuest.objectives.Count}");
                }
            }
        }

        /// <summary>
        /// Phase 7: 完成整个任务
        /// </summary>
        public void CompleteQuest()
        {
            if (!_isQuestActive)
            {
                if (showDebugLogs) Debug.LogWarning("[QuestModule] 当前没有激活的任务");
                return;
            }

            // Phase 7: 给予奖励
            if (_memory != null)
            {
                _memory.AddFriendship(_currentQuest.experienceReward);

                // Phase 7: 记录奖励物品
                if (_currentQuest.itemRewards != null && _currentQuest.itemRewards.Count > 0)
                {
                    if (showDebugLogs) Debug.Log($"[QuestModule] 奖励物品：{_currentQuest.itemRewards.Count} 个");
                }
            }

            _isQuestActive = false;

            // Phase 7: 记录任务完成到 Memory
            if (_memory != null)
            {
                _memory.RecordEvent(ActorMemory.EventType.QuestCompleted);
            }

            if (showDebugLogs) Debug.Log($"[QuestModule] 任务已完成：{_currentQuest.questName}");
        }

        /// <summary>
        /// Phase 7: 取消任务
        /// </summary>
        public void CancelQuest()
        {
            if (!_isQuestActive)
            {
                return;
            }

            _isQuestActive = false;

            if (showDebugLogs) Debug.Log($"[QuestModule] 任务已取消：{_currentQuest.questName}");
        }

        /// <summary>
        /// Phase 7: 获取当前任务信息
        /// </summary>
        public QuestData GetCurrentQuest()
        {
            return _currentQuest;
        }

        /// <summary>
        /// Phase 7: 检查是否有激活的任务
        /// </summary>
        public bool IsQuestActive()
        {
            return _isQuestActive;
        }

        /// <summary>
        /// Phase 7: 启用模块
        /// </summary>
        public override void Enable()
        {
            base.Enable();
            InitializeQuests();
        }

        /// <summary>
        /// Phase 7: 禁用模块
        /// </summary>
        public override void Disable()
        {
            CancelQuest();
            base.Disable();
            if (showDebugLogs) Debug.Log("[QuestModule] 已禁用");
        }

        /// <summary>
        /// Phase 7: 启用模块时的初始化
        /// </summary>
        protected override void OnEnabled()
        {
            base.OnEnabled();
        }

        /// <summary>
        /// Phase 7: 禁用模块时的清理
        /// </summary>
        protected override void OnDisabled()
        {
            _currentQuest = null;
            _currentObjectiveIndex = 0;
            _isQuestActive = false;
        }
    }
}
