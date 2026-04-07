using UnityEngine;
using System.Collections.Generic;
using FarmGame.Core.Contracts;

namespace FarmGame.NPCSystem
{
    /// <summary>
    /// NPCInteractable: 旧版交互组件
    /// Phase 8: 已标记为过时，仅保留兼容性。
    /// 新项目请使用 ActorInteraction (FarmGame.ActorSystem)
    /// </summary>
    [System.Obsolete("NPCInteractable is deprecated. Use ActorInteraction (FarmGame.ActorSystem) instead.")]
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class NPCInteractable : MonoBehaviour, IInteractable, IDialogSubject
    {
        private const string ActorInteractionTypeName = "FarmGame.ActorSystem.ActorInteraction";

        [Header("UI References (Legacy)")]
        [Tooltip("(已过时）对话 UI 引用。新系统使用 Actor")]
        public MonoBehaviour dialogUI;

        // 内部桥接到新系统（不直接依赖 Actor 类型）
        private IDialogSubject _actorSubject;
        private IDialogUI _dialogUIContract;
        private bool _redirectLogged;

        void Awake()
        {
            if (HasActorInteractionSibling())
            {
                enabled = false;
                Debug.LogWarning("[NPCInteractable] 检测到 ActorInteraction，已停用 legacy 组件。");
                return;
            }

            _dialogUIContract = dialogUI as IDialogUI;
            ResolveActorSubject();
        }

        private void ResolveActorSubject()
        {
            var candidates = GetComponents<MonoBehaviour>();
            foreach (var candidate in candidates)
            {
                if (candidate == null || candidate == this)
                {
                    continue;
                }

                if (candidate is IDialogSubject dialogSubject)
                {
                    _actorSubject = dialogSubject;
                    return;
                }
            }

            _actorSubject = null;
        }

        // IDialogSubject Implementation
        public string Name => _actorSubject?.Name ?? "Unknown NPC";
        public List<string> DialogLines => _actorSubject?.DialogLines ?? null;
        public string FunctionButtonText => _actorSubject?.FunctionButtonText ?? "";
        public Transform SubjectTransform => transform;

        public void InvokeFunction()
        {
            _actorSubject?.InvokeFunction();
        }

        // IInteractable implementation
        public string GetInteractPrompt()
        {
            if (!enabled)
            {
                return string.Empty;
            }

            if (_actorSubject == null) return "按 [E] 交互";
            return $"按 [E] 对话：{_actorSubject.Name}";
        }

        public Transform GetTransform() => transform;

        public void Interact(GameObject interactor)
        {
            if (!enabled)
            {
                return;
            }

            if (_actorSubject == null)
            {
                Debug.LogWarning("[NPCInteractable] Actor component missing. NPCInteractable is deprecated, please use ActorInteraction.");
                return;
            }

            if (!_redirectLogged)
            {
                Debug.LogWarning("[NPCInteractable] 该组件已过时，仅作兼容桥接。建议迁移为 ActorInteraction。");
                _redirectLogged = true;
            }

            // 桥接到新系统
            var ui = _dialogUIContract ?? RuntimeRefs.DialogUIContract;
            if (ui != null)
            {
                ui.Open(_actorSubject);
            }
            else
            {
                Debug.LogWarning("[NPCInteractable] Dialog UI not found");
            }
        }

        private bool HasActorInteractionSibling()
        {
            MonoBehaviour[] candidates = GetComponents<MonoBehaviour>();
            for (int i = 0; i < candidates.Length; i++)
            {
                MonoBehaviour candidate = candidates[i];
                if (candidate == null || candidate == this)
                {
                    continue;
                }

                if (candidate.GetType().FullName == ActorInteractionTypeName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
