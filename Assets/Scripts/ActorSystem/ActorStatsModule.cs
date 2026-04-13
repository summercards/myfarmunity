using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// StatEntry: 属性条目，使用轻量的 key/value 结构
    /// </summary>
    [System.Serializable]
    public class StatEntry
    {
        [Tooltip("属性键名（如 friendship_gain_multiplier, max_hp 等）")]
        public string key;

        [Tooltip("属性数值")]
        public float value;

        public StatEntry(string key, float value)
        {
            this.key = key;
            this.value = value;
        }
    }

    /// <summary>
    /// ActorStatsModule: NPC 属性模块
    /// Phase v1.1: 提供统一的轻量属性入口，支持按 key 查询、设置、累加属性
    /// - 不引入战斗框架或复杂数值系统
    /// - 支持缺省 NPC 安全无报错
    /// - 属性不存在时返回默认值
    /// </summary>
    public class ActorStatsModule : ActorModule
    {
        /// <summary>
        /// 属性列表（key/value 结构，方便序列化与扩展）
        /// </summary>
        [Tooltip("基础属性列表（从 NPCDefinition.baseStats 写入）")]
        public List<StatEntry> baseStats = new List<StatEntry>();

        /// <summary>
        /// 运行时属性缓存（key -> value）
        /// </summary>
        private Dictionary<string, float> _runtimeStats;

        /// <summary>
        /// 默认属性值（当属性不存在时返回）
        /// </summary>
        private static readonly Dictionary<string, float> DefaultValues = new Dictionary<string, float>
        {
            { "friendship_gain_multiplier", 1.0f },
            { "max_hp", 100.0f },
            { "move_speed", 1.0f },
            { "attack_power", 10.0f },
            { "defense", 0.0f }
        };

        #region 模块生命周期

        protected override void OnEnabled()
        {
            // 初始化运行时属性缓存
            _runtimeStats = new Dictionary<string, float>();
            
            // 复制基础属性到运行时缓存
            foreach (var stat in baseStats)
            {
                if (!string.IsNullOrEmpty(stat.key))
                {
                    _runtimeStats[stat.key] = stat.value;
                }
            }

            Debug.Log($"[{GetType().Name}] 已启用，初始化 {_runtimeStats.Count} 个属性");
        }

        protected override void OnDisabled()
        {
            // 清理运行时缓存
            _runtimeStats?.Clear();
            _runtimeStats = null;
            
            Debug.Log($"[{GetType().Name}] 已禁用");
        }

        #endregion

        #region 核心接口

        /// <summary>
        /// 获取属性值（不存在时返回默认值）
        /// </summary>
        public float Get(string key, float defaultValue = 0f)
        {
            if (!IsEnabled || _runtimeStats == null)
            {
                // 返回预定义的默认值或传入的默认值
                if (DefaultValues.ContainsKey(key))
                {
                    return DefaultValues[key];
                }
                return defaultValue;
            }

            if (_runtimeStats.TryGetValue(key, out float value))
            {
                return value;
            }

            // 返回预定义的默认值或传入的默认值
            if (DefaultValues.ContainsKey(key))
            {
                return DefaultValues[key];
            }
            return defaultValue;
        }

        /// <summary>
        /// 设置属性值（覆盖）
        /// </summary>
        public void Set(string key, float value)
        {
            if (!IsEnabled || _runtimeStats == null)
            {
                Debug.LogWarning($"[{GetType().Name}] 模块未启用，无法设置属性：{key}");
                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"[{GetType().Name}] 属性键名不能为空");
                return;
            }

            _runtimeStats[key] = value;
        }

        /// <summary>
        /// 累加属性值（不存在时创建）
        /// </summary>
        public void Add(string key, float delta)
        {
            if (!IsEnabled || _runtimeStats == null)
            {
                Debug.LogWarning($"[{GetType().Name}] 模块未启用，无法累加属性：{key}");
                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"[{GetType().Name}] 属性键名不能为空");
                return;
            }

            if (_runtimeStats.TryGetValue(key, out float currentValue))
            {
                _runtimeStats[key] = currentValue + delta;
            }
            else
            {
                // 不存在时创建新属性
                _runtimeStats[key] = delta;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查属性是否存在
        /// </summary>
        public bool HasStat(string key)
        {
            if (!IsEnabled || _runtimeStats == null)
            {
                return false;
            }

            return _runtimeStats.ContainsKey(key);
        }

        /// <summary>
        /// 获取所有属性键名
        /// </summary>
        public string[] GetAllKeys()
        {
            if (!IsEnabled || _runtimeStats == null)
            {
                return new string[0];
            }

            var keys = new string[_runtimeStats.Count];
            _runtimeStats.Keys.CopyTo(keys, 0);
            return keys;
        }

        /// <summary>
        /// 重置所有属性到基础值（清除运行时修改）
        /// </summary>
        public void ResetToBaseStats()
        {
            if (!IsEnabled || _runtimeStats == null)
            {
                Debug.LogWarning($"[{GetType().Name}] 模块未启用，无法重置属性");
                return;
            }

            _runtimeStats.Clear();
            
            foreach (var stat in baseStats)
            {
                if (!string.IsNullOrEmpty(stat.key))
                {
                    _runtimeStats[stat.key] = stat.value;
                }
            }

            Debug.Log($"[{GetType().Name}] 已重置 {_runtimeStats.Count} 个属性到基础值");
        }

        /// <summary>
        /// 获取所有属性的快照（用于调试或保存）
        /// </summary>
        public Dictionary<string, float> GetStatsSnapshot()
        {
            if (!IsEnabled || _runtimeStats == null)
            {
                return new Dictionary<string, float>();
            }

            return new Dictionary<string, float>(_runtimeStats);
        }

        #endregion

        #region 调试辅助

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器中显示当前属性列表（用于调试）
        /// </summary>
        public void DebugPrintStats()
        {
            if (!IsEnabled || _runtimeStats == null)
            {
                Debug.Log($"[{GetType().Name}] 模块未启用，无属性数据");
                return;
            }

            var keys = GetAllKeys();
            if (keys.Length == 0)
            {
                Debug.Log($"[{GetType().Name}] 当前无属性");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[{GetType().Name}] 当前属性列表（{keys.Length} 个）：");
            
            foreach (var key in keys)
            {
                float value = Get(key);
                sb.AppendLine($"  {key}: {value}");
            }

            Debug.Log(sb.ToString());
        }
#endif

        #endregion
    }
}
