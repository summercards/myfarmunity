using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// DialogueGroup: 台词分组条目
    /// 包含分组标识（key）和该分组的台词列表（lines）
    /// </summary>
    [System.Serializable]
    public class DialogueGroup
    {
        [Header("分组标识（用于选择规则）")]
        [Tooltip("预定义分组：first_meet, daily, stranger, acquaintance, friend, close_friend")]
        public string key = "daily";

        [Header("台词列表")]
        [Tooltip("该分组的所有台词，运行时随机选择一条")]
        public List<string> lines = new List<string>();
    }

    /// <summary>
    /// DialogueSetSO: 台词分组资产（ScriptableObject）
    /// 支持首次会面、好感度分级等分组规则
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueSet_", menuName = "Farm/Dialogue Set", order = 20)]
    public class DialogueSetSO : ScriptableObject
    {
        [Header("台词分组列表")]
        [Tooltip("包含多个分组的台词集合，每个分组有独立的选择规则")]
        public List<DialogueGroup> groups = new List<DialogueGroup>();
    }
}
