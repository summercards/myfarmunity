using UnityEngine;
using System.Collections.Generic;
using FarmGame.UI; // For IDialogSubject
using FarmGame.NPCSystem; // For NPCFunction

namespace FarmGame.ActorSystem
{
    [DisallowMultipleComponent]
    public class Actor : MonoBehaviour, IDialogSubject
    {
        // 核心组件缓存
        public ActorIdentity Identity { get; private set; }
        public ActorMemory Memory { get; private set; }
        public ActorBrain Brain { get; private set; }
        public ActorDialogue Dialogue { get; private set; }
        public ActorView View { get; private set; }
        
        // 模块缓存
        private ShopModule _shopModule;
        private QuestModule _questModule;
        private GiftModule _giftModule;
        private DialogueModule _dialogueModule;

        private void Awake()
        {
            Identity = GetComponent<ActorIdentity>();
            Memory = GetComponent<ActorMemory>();
            Brain = GetComponent<ActorBrain>();
            Dialogue = GetComponent<ActorDialogue>();
            View = GetComponent<ActorView>();
            
            // 缓存模块
            _shopModule = GetComponent<ShopModule>();
            _questModule = GetComponent<QuestModule>();
            _giftModule = GetComponent<GiftModule>();
            _dialogueModule = GetComponent<DialogueModule>();
        }

        // IDialogSubject Implementation
        public string Name => Identity ? Identity.Name : "Unknown Actor";
        
        public List<string> DialogLines 
        {
            get 
            {
                // Phase 5 修复：直接从 ActorIdentity 获取所有对话
                // 这样 NPCDialogUI 可以遍历所有台词
                return Identity ? Identity.GetDefaultDialog() : new List<string>();
            }
        }

        public string FunctionButtonText => Identity ? Identity.FunctionButtonText : "功能"; // 从 ActorIdentity 获取

        public Transform SubjectTransform => transform;

        public void InvokeFunction()
        {
            if (Identity == null) 
            {
                Debug.LogWarning("[Actor] 身份组件缺失，无法执行功能。");
                return;
            }

            // Phase 7: 根据 Identity.Function 类型，通过 ActorModules 调用相应功能
            switch (Identity.Function)
            {
                case NPCFunction.OpenShop:
                    if (_shopModule != null)
                    {
                        _shopModule.OpenShop();
                        Debug.Log($"[Actor] {Name} 的商店已打开");
                    }
                    else
                    {
                        Debug.LogWarning($"[Actor] {Name} 缺少 ShopModule，无法打开商店");
                    }
                    break;

                case NPCFunction.Quest:
                    if (_questModule != null)
                    {
                        _questModule.StartQuest("default");
                        Debug.Log($"[Actor] {Name} 的任务已开始");
                    }
                    else
                    {
                        Debug.LogWarning($"[Actor] {Name} 缺少 QuestModule，无法开始任务");
                    }
                    break;

                case NPCFunction.Gift:
                    if (_giftModule != null)
                    {
                        _giftModule.ReceiveGift("Common", "礼物", 1);
                        Debug.Log($"[Actor] {Name} 已接收礼物");
                    }
                    else
                    {
                        Debug.LogWarning($"[Actor] {Name} 缺少 GiftModule，无法接收礼物");
                    }
                    break;

                case NPCFunction.Talk:
                    // 对话功能由 NPCDialogUI 处理，这里不需要额外操作
                    Debug.Log($"[Actor] {Name} 的对话功能触发");
                    break;

                case NPCFunction.None:
                default:
                    Debug.LogWarning($"[Actor] {Name} 的功能 {Identity.Function} 未实现");
                    break;
            }
        }
    }
}
