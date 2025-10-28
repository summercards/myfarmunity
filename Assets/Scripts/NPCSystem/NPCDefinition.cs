using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.NPCSystem
{
    public enum NPCFunctionType
    {
        None = 0,
        OpenShop = 1,
        // 以后你要扩展：接任务、传送、打开锻造等，都可继续加
    }

    [CreateAssetMenu(fileName = "NPC_", menuName = "Farm/NPC Definition", order = 10)]
    public class NPCDefinition : ScriptableObject
    {
        [Header("基础信息")]
        public string npcId = "npc_001";
        public string npcName = "新村民";

        [Tooltip("对话台词（逐行显示）")]
        public List<string> dialogLines = new List<string>()
        {
            "你好，欢迎来到我们的农场。",
            "需要我做点什么吗？"
        };

        [Header("功能按钮（对话框里的“功能”）")]
        public NPCFunctionType function = NPCFunctionType.None;

        [Tooltip("当功能是 OpenShop 时使用；指向商店的默认货架 Catalog（可选）")]
        public ScriptableObject defaultShopCatalog; // 你的 ShopCatalogSO 类型（不强制引用，留空也行）

        [Tooltip("功能按钮显示用的文字；为空则使用默认：比如“打开商店”")]
        public string functionButtonText = "";

        [Header("生成选项（用于一键放置时的默认值）")]
        public Vector3 defaultColliderCenter = new Vector3(0, 1, 0);
        public Vector3 defaultColliderSize = new Vector3(1, 2, 1);
        public bool addCapsuleColliderInstead = false;

        [Header("Gizmos（可选）")]
        public Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.35f);
    }
}
