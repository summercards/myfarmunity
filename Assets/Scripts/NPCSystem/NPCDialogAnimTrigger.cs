using UnityEngine;

namespace FarmGame.NPCSystem
{
    /// <summary>
    /// 监听 NPCDialogUI：当对话开启且当前 NPC = 自己时，触发 NPCVisualController 的 BeginTalk/EndTalk。
    /// 非侵入式：自动在场景里 Find 一个 NPCDialogUI。
    /// </summary>
    [DisallowMultipleComponent]
    public class NPCDialogAnimTrigger : MonoBehaviour
    {
        public NPCDefinition definition;
        public MonoBehaviour npcInteractable;     // 你的 NPCInteractable（自动找）
        public Component dialogUI;                // 场景里的 NPCDialogUI（自动找）
        public NPCVisualController visual;        // 同节点上的视觉控制器（自动找）

        private bool _lastTalking = false;

        private void Reset() => TryAutoFill();

        private void Awake()
        {
            TryAutoFill();
        }

        private void Update()
        {
            if (definition == null) return;
            if (npcInteractable == null || dialogUI == null || visual == null) return;

            // 读取 UI 状态
            var uiType = dialogUI.GetType();
            var isOpenProp = uiType.GetProperty("IsOpen");
            var currentNpcProp = uiType.GetProperty("CurrentNPC");

            if (isOpenProp == null || currentNpcProp == null) return;

            bool isOpen = false;
            object currentNpc = null;
            try
            {
                isOpen = (bool)isOpenProp.GetValue(dialogUI);
                currentNpc = currentNpcProp.GetValue(dialogUI);
            }
            catch { return; }

            bool talkingNow = isOpen && ReferenceEquals(currentNpc, npcInteractable);

            if (talkingNow != _lastTalking)
            {
                _lastTalking = talkingNow;
                if (talkingNow) visual.BeginTalk();
                else visual.EndTalk();
            }
        }

        public void TryAutoFill()
        {
            if (visual == null) visual = GetComponent<NPCVisualController>();
            if (definition == null && visual != null) definition = visual.definition;

            if (npcInteractable == null)
            {
                npcInteractable = GetComponent<MonoBehaviour>();
                // 找包含“NPCInteractable”的脚本
                foreach (var mb in GetComponents<MonoBehaviour>())
                {
                    if (mb != null && mb.GetType().Name.ToLower().Contains("npcinteractable"))
                    {
                        npcInteractable = mb; break;
                    }
                }
            }

            if (dialogUI == null)
            {
                // 在场景里找一个带 NPCDialogUI 的组件
                foreach (var c in FindObjectsOfType<MonoBehaviour>(true))
                {
                    if (c != null && c.GetType().Name.ToLower().Contains("npcdialogui"))
                    {
                        dialogUI = c; break;
                    }
                }
            }
        }
    }
}
