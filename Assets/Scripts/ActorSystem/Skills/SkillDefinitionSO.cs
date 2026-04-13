using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.ActorSystem.Skills
{
    /// <summary>
    /// SkillDefinitionSO: 技能定义资产
    /// v1.1: 技能的数据驱动定义，支持冷却时间和自定义参数
    /// </summary>
    [CreateAssetMenu(fileName = "SkillDefinition", menuName = "Farm/Skill Definition")]
    public class SkillDefinitionSO : ScriptableObject
    {
        [Header("基本信息")]
        [Tooltip("技能唯一标识符（用于运行时查找和冷却管理）")]
        public string skillId;

        [Tooltip("技能显示名称")]
        public string displayName;

        [Tooltip("技能描述")]
        [TextArea(3, 10)]
        public string description;

        [Header("冷却设置")]
        [Tooltip("技能冷却时间（秒）")]
        [Range(0f, 3600f)]
        public float cooldownSeconds = 10f;

        [Header("技能参数")]
        [Tooltip("技能自定义参数（键值对）")]
        public List<SkillParameter> parameters = new List<SkillParameter>();

        /// <summary>
        /// SkillParameter: 技能参数（键值对）
        /// 用于支持不同技能的自定义参数（如伤害值、范围、持续时间等）
        /// </summary>
        [System.Serializable]
        public class SkillParameter
        {
            [Tooltip("参数键")]
            public string key;

            [Tooltip("参数值（浮点数）")]
            public float value;
        }

        /// <summary>
        /// 获取技能参数
        /// </summary>
        /// <param name="key">参数键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>参数值</returns>
        public float GetParameter(string key, float defaultValue = 0f)
        {
            if (parameters == null) return defaultValue;

            foreach (var param in parameters)
            {
                if (param.key == key)
                {
                    return param.value;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// 检查技能定义是否有效
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(skillId) && cooldownSeconds >= 0f;
        }
    }
}
