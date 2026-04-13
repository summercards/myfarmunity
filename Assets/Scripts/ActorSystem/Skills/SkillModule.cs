using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.ActorSystem.Skills
{
    /// <summary>
    /// SkillModule: 技能模块
    /// v1.1: 提供技能列表、冷却管理和技能触发能力
    /// - 支持多个技能定义
    /// - 支持技能冷却状态管理
    /// - 支持技能触发和冷却检查
    /// </summary>
    public class SkillModule : ActorModule
    {
        [Header("技能配置")]
        [Tooltip("技能定义列表（从 NPCDefinition 写入）")]
        public List<SkillDefinitionSO> skills = new List<SkillDefinitionSO>();

        /// <summary>
        /// CooldownEntry: 冷却条目
        /// 记录每个技能的冷却结束时间
        /// </summary>
        [System.Serializable]
        private class CooldownEntry
        {
            public string skillId;
            public float cooldownEndTime;
        }

        /// <summary>
        /// 运行时冷却表（不序列化）
        /// </summary>
        private Dictionary<string, float> _cooldownTable;

        /// <summary>
        /// v1.1: 启用模块
        /// </summary>
        protected override void OnEnabled()
        {
            base.OnEnabled();
            InitializeCooldownTable();
            Debug.Log($"[SkillModule] 已启用，包含 {skills?.Count ?? 0} 个技能");
        }

        /// <summary>
        /// v1.1: 禁用模块
        /// </summary>
        protected override void OnDisabled()
        {
            base.OnDisabled();
            ClearCooldownTable();
            Debug.Log($"[SkillModule] 已禁用，已清理冷却表");
        }

        /// <summary>
        /// 初始化冷却表
        /// </summary>
        private void InitializeCooldownTable()
        {
            _cooldownTable = new Dictionary<string, float>();

            if (skills == null) return;

            // 初始化所有技能的冷却时间为 0（表示未进入冷却）
            foreach (var skill in skills)
            {
                if (skill == null || string.IsNullOrEmpty(skill.skillId))
                {
                    continue;
                }

                _cooldownTable[skill.skillId] = 0f;
            }
        }

        /// <summary>
        /// 清理冷却表
        /// </summary>
        private void ClearCooldownTable()
        {
            if (_cooldownTable != null)
            {
                _cooldownTable.Clear();
            }
        }

        /// <summary>
        /// 尝试施放技能
        /// </summary>
        /// <param name="skillId">技能 ID</param>
        /// <returns>是否施放成功</returns>
        public bool TryCast(string skillId)
        {
            if (!IsEnabled)
            {
                Debug.LogWarning($"[SkillModule] 模块未启用，无法施放技能 {skillId}");
                return false;
            }

            // 检查技能是否存在
            var skill = GetSkillDefinition(skillId);
            if (skill == null)
            {
                Debug.LogWarning($"[SkillModule] 技能 {skillId} 不存在");
                return false;
            }

            // 检查技能是否就绪
            if (!IsReady(skillId))
            {
                Debug.LogWarning($"[SkillModule] 技能 {skillId} 正在冷却中");
                return false;
            }

            // 施放技能成功，进入冷却
            EnterCooldown(skillId, skill.cooldownSeconds);

            Debug.Log($"[SkillModule] 技能 {skillId} 施放成功，冷却时间 {skill.cooldownSeconds} 秒");
            return true;
        }

        /// <summary>
        /// 检查技能是否就绪（不在冷却中）
        /// </summary>
        /// <param name="skillId">技能 ID</param>
        /// <returns>是否就绪</returns>
        public bool IsReady(string skillId)
        {
            if (!IsEnabled)
            {
                return false;
            }

            // 检查技能定义是否存在
            var skill = GetSkillDefinition(skillId);
            if (skill == null)
            {
                return false;
            }

            // 检查冷却表
            if (_cooldownTable == null || !_cooldownTable.ContainsKey(skillId))
            {
                return true;
            }

            // 检查冷却是否结束
            float currentTime = Time.time;
            float cooldownEndTime = _cooldownTable[skillId];
            return currentTime >= cooldownEndTime;
        }

        /// <summary>
        /// 更新冷却时间（低频 tick 调用）
        /// </summary>
        /// <param name="dt">时间增量（秒）</param>
        public void Tick(float dt)
        {
            // 冷却时间由 Time.time 自动计算，不需要显式递减
            // 此方法为接口保留，未来可用于处理其他周期性逻辑
        }

        /// <summary>
        /// 进入冷却
        /// </summary>
        /// <param name="skillId">技能 ID</param>
        /// <param name="cooldownSeconds">冷却时间（秒）</param>
        private void EnterCooldown(string skillId, float cooldownSeconds)
        {
            if (_cooldownTable == null)
            {
                return;
            }

            float currentTime = Time.time;
            float cooldownEndTime = currentTime + cooldownSeconds;

            _cooldownTable[skillId] = cooldownEndTime;
        }

        /// <summary>
        /// 获取技能定义
        /// </summary>
        /// <param name="skillId">技能 ID</param>
        /// <returns>技能定义</returns>
        public SkillDefinitionSO GetSkillDefinition(string skillId)
        {
            if (skills == null)
            {
                return null;
            }

            foreach (var skill in skills)
            {
                if (skill != null && skill.skillId == skillId)
                {
                    return skill;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取所有技能定义
        /// </summary>
        /// <returns>技能定义列表</returns>
        public List<SkillDefinitionSO> GetAllSkills()
        {
            return skills ?? new List<SkillDefinitionSO>();
        }

        /// <summary>
        /// 获取技能冷却剩余时间
        /// </summary>
        /// <param name="skillId">技能 ID</param>
        /// <returns>剩余冷却时间（秒），0 表示已就绪</returns>
        public float GetCooldownRemaining(string skillId)
        {
            if (_cooldownTable == null || !_cooldownTable.ContainsKey(skillId))
            {
                return 0f;
            }

            float currentTime = Time.time;
            float cooldownEndTime = _cooldownTable[skillId];
            float remaining = cooldownEndTime - currentTime;

            return Mathf.Max(0f, remaining);
        }

        /// <summary>
        /// 检查模块是否包含指定技能
        /// </summary>
        /// <param name="skillId">技能 ID</param>
        /// <returns>是否包含</returns>
        public bool HasSkill(string skillId)
        {
            return GetSkillDefinition(skillId) != null;
        }

        /// <summary>
        /// 获取技能数量
        /// </summary>
        /// <returns>技能数量</returns>
        public int GetSkillCount()
        {
            return skills?.Count ?? 0;
        }
    }
}
