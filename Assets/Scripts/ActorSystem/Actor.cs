using UnityEngine;
using System.Collections.Generic;
using FarmGame.UI; // For IDialogSubject

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

        private void Awake()
        {
            Identity = GetComponent<ActorIdentity>();
            Memory = GetComponent<ActorMemory>();
            Brain = GetComponent<ActorBrain>();
            Dialogue = GetComponent<ActorDialogue>();
            View = GetComponent<ActorView>();
        }

        // IDialogSubject Implementation
        public string Name => Identity ? Identity.Name : "Unknown Actor";
        
        public List<string> DialogLines 
        {
            get 
            {
                // Phase 5 将使用 DialogueResolver，目前从 ActorDialogue 获取
                return Dialogue ? Dialogue.GetCurrentLines() : new List<string>();
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

            Debug.Log($"[Actor] {Name} 的功能 {Identity.Function} 被调用 (Phase 7 模块系统将接管)");
            // Phase 7: 根据 Identity.Function 类型，通过 ActorModules 调用相应功能 (Shop, Quest, etc.)
        }
    }
}
