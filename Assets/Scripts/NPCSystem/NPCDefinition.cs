using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.NPCSystem
{
    [System.Serializable]
    public class DailyAnimEntry
    {
        [Header("时间段 (开始时间包含，结束时间不包含，支持跨夜如 22-4)")]
        [Range(0, 23)] public int startHour = 8;
        [Range(0, 24)] public int endHour = 18;

        [Header("动画状态名")]
        public string stateName = "Idle";

        [Range(0.1f, 3f)] public float speed = 1f;

        [Header("条件标签 (预留)")]
        public string conditionTag = ""; 

        public bool ContainsHour(float hour)
        {
            if (startHour <= endHour) return hour >= startHour && hour < endHour;
            else return hour >= startHour || hour < endHour; // Overnight
        }
    }

    [CreateAssetMenu(fileName = "NPC_", menuName = "Farm/NPC Definition", order = 10)]
    public class NPCDefinition : ScriptableObject
    {
        [Header("架构切换 (重构)")]
        [Tooltip("是否使用新的 Actor 系统架构")]
        public bool useActorSystem = false;

        [Header("模块开关 (用于单独测试)")]
        [Tooltip("是否生成 Visual 子物体（模型与动画）")]
        public bool enableVisuals = true;
        [Tooltip("是否添加碰撞体和刚体")]
        public bool enableCollider = true;
        [Tooltip("是否添加交互组件 (NPCInteractable)")]
        public bool enableInteraction = true;
        [Tooltip("是否启用商店组件 (SimpleShopOpener)")]
        public bool enableShop = true;

        [Header("基本信息")]
        public string npcId = "npc_001";
        public string npcName = "新角色";
        [Tooltip("简单的对话列表")]
        public List<string> dialogLines = new List<string> { "你好，欢迎来到农场。", "有什么需要帮忙的吗？" };

        [Header("功能配置")]
        public NPCFunction function = NPCFunction.None;
        [Tooltip("如果是 OpenShop 类型，请在此配置商店目录")]
        public ShopCatalogSO defaultShopCatalog;
        [Tooltip("功能按钮上的文本（如“打开商店”）")]
        public string functionButtonText = "";

        [Header("模型与动画")]
        public GameObject modelPrefab;
        public RuntimeAnimatorController animatorController;
        public Vector3 modelLocalPosition = Vector3.zero;
        public Vector3 modelLocalEuler = Vector3.zero;
        public Vector3 modelLocalScale = Vector3.one;

        [Header("日常作息 (24小时制)")]
        [Tooltip("设定 NPC 在不同时间段的动画状态")]
        public List<DailyAnimEntry> dailyAnimations = new List<DailyAnimEntry>()
        {
            new DailyAnimEntry(){ startHour=0, endHour=6, stateName="Sleep", speed=1 },
            new DailyAnimEntry(){ startHour=6, endHour=22, stateName="Idle",  speed=1 },
            new DailyAnimEntry(){ startHour=22,endHour=24,stateName="Sleep", speed=1 },
        };

        [Header("对话表现")]
        public string talkStateName = "Talk";
        [Range(0.1f, 3f)] public float talkSpeed = 1f;
        [Range(0f, 1f)] public float talkCrossFade = 0.2f;

        [Header("碰撞体设置 (预制体构建)")]
        public Vector3 colliderCenter = new Vector3(0, 1, 0);
        public float colliderRadius = 0.5f;
        public float colliderHeight = 2.0f;
        
        [Header("调试绘制 (Gizmos)")]
        public Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.35f);
    }
}
