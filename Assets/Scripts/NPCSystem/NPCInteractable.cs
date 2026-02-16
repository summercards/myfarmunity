using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using FarmGame.UI;
using FarmGame.ActorSystem;

namespace FarmGame.NPCSystem
{
    /// <summary>
    /// NPCInteractable: 旧版交互组件
    /// Phase 8: 已标记为过时，仅保留兼容性。
    /// 新项目请使用 ActorInteraction (FarmGame.ActorSystem)
    /// </summary>
    [System.Obsolete("NPCInteractable is deprecated. Use ActorInteraction (FarmGame.ActorSystem) instead.")]
    [DisallowMultipleComponent]
    public class NPCInteractable : MonoBehaviour, IInteractable, IDialogSubject
    {
        [Header("UI References (Legacy)")]
        [Tooltip("(已过时）对话 UI 引用。新系统使用 Actor")]
        public NPCDialogUI dialogUI;

        // 内部引用到新系统
        private Actor _actor;

        void Awake()
        {
            // Phase 8: 桥接到新系统
            _actor = GetComponent<Actor>();
        }

        // IDialogSubject Implementation
        public string Name => _actor?.Identity?.Name ?? "Unknown NPC";
        public List<string> DialogLines => _actor?.DialogLines ?? null;
        public string FunctionButtonText => _actor?.FunctionButtonText ?? "";
        public Transform SubjectTransform => transform;

        public void InvokeFunction()
        {
            _actor?.InvokeFunction();
        }

        // IInteractable implementation
        public string GetInteractPrompt()
        {
            if (_actor == null || _actor.Identity == null) return "按 [E] 交互";
            return $"按 [E] 对话：{_actor.Identity.Name}";
        }

        public Transform GetTransform() => transform;

        public void Interact(GameObject interactor)
        {
            if (_actor == null)
            {
                Debug.LogWarning("[NPCInteractable] Actor component missing. NPCInteractable is deprecated, please use ActorInteraction.");
                return;
            }

            Debug.Log("[NPCInteractable] Redirecting to Actor system (Deprecated)");
            
            // 桥接到新系统
            var ui = dialogUI ?? FindObjectOfType<NPCDialogUI>();
            if (ui != null)
            {
                ui.Open(_actor);
            }
            else
            {
                Debug.LogWarning("[NPCInteractable] NPCDialogUI not found");
            }
        }
    }
}
