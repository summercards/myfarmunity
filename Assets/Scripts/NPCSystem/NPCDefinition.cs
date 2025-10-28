using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.NPCSystem
{
    public enum NPCFunctionType { None = 0, OpenShop = 1 }

    [System.Serializable]
    public class DailyAnimEntry
    {
        [Header("时间段（含起不含止，支持跨夜：如 22→4）")]
        [Range(0, 23)] public int startHour = 8;
        [Range(0, 24)] public int endHour = 18;

        [Header("Animator 状态名（需存在于 AnimatorController）")]
        public string stateName = "Idle";

        [Range(0.1f, 3f)] public float speed = 1f;

        [Header("条件（预留，不启用）")]
        public string conditionTag = ""; // 先占位，暂不使用

        public bool ContainsHour(float hour)
        {
            if (startHour <= endHour) return hour >= startHour && hour < endHour;
            else return hour >= startHour || hour < endHour; // 跨夜
        }
    }

    [CreateAssetMenu(fileName = "NPC_", menuName = "Farm/NPC Definition", order = 10)]
    public class NPCDefinition : ScriptableObject
    {
        [Header("基础信息")]
        public string npcId = "npc_001";
        public string npcName = "新村民";
        [Tooltip("对话台词（逐行显示）")]
        public List<string> dialogLines = new List<string> { "你好，欢迎来到我们的农场。", "需要我做点什么吗？" };

        [Header("功能按钮（对话框里的“功能”）")]
        public NPCFunctionType function = NPCFunctionType.None;
        [Tooltip("当功能是 OpenShop 时使用；指向商店的默认货架 Catalog（可选）")]
        public ScriptableObject defaultShopCatalog;
        [Tooltip("功能按钮显示用的文字；为空则使用默认")]
        public string functionButtonText = "";

        [Header("模型与动作")]
        public GameObject modelPrefab;
        public RuntimeAnimatorController animatorController;
        public Vector3 modelLocalPosition = Vector3.zero;
        public Vector3 modelLocalEuler = Vector3.zero;
        public Vector3 modelLocalScale = Vector3.one;

        [Tooltip("一天 24h 的动作表（按顺序检查，先匹配先用）")]
        public List<DailyAnimEntry> dailyAnimations = new List<DailyAnimEntry>()
        {
            new DailyAnimEntry(){ startHour=0, endHour=6, stateName="Sleep", speed=1 },
            new DailyAnimEntry(){ startHour=6, endHour=22, stateName="Idle",  speed=1 },
            new DailyAnimEntry(){ startHour=22,endHour=24,stateName="Sleep", speed=1 },
        };

        [Header("对话时动作")]
        public string talkStateName = "Talk";       // 对话期间播放的状态
        [Range(0.1f, 3f)] public float talkSpeed = 1f;
        [Range(0f, 1f)] public float talkCrossFade = 0.2f;

        [Header("生成选项（用于一键放置时的默认值）")]
        public Vector3 defaultColliderCenter = new Vector3(0, 1, 0);
        public Vector3 defaultColliderSize = new Vector3(1, 2, 1);
        public bool addCapsuleColliderInstead = false;

        [Header("Gizmos（可选）")]
        public Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.35f);
    }
}
