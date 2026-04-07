using UnityEngine;
using FarmGame.Core.Contracts;

namespace FarmGame.ActorSystem
{
    [RequireComponent(typeof(Actor))]
    public class ActorInteraction : MonoBehaviour, IInteractable, IDialogSubject
    {
        private Actor _actor;
        private static IDialogUI _cachedDialogUI;

        private void Awake()
        {
            _actor = GetComponent<Actor>();
            _cachedDialogUI = RuntimeRefs.DialogUIContract;
        }

        private void OnEnable()
        {
            RuntimeRefs.DialogUIContractChanged += HandleDialogUIChanged;
        }

        private void OnDisable()
        {
            RuntimeRefs.DialogUIContractChanged -= HandleDialogUIChanged;
        }

        private void HandleDialogUIChanged(IDialogUI dialogUI)
        {
            _cachedDialogUI = dialogUI;
        }

        // IDialogSubject Implementation (Proxy to Actor)
        public string Name => _actor != null ? _actor.Name : "Unknown";
        public System.Collections.Generic.List<string> DialogLines => _actor != null ? _actor.DialogLines : null;
        public string FunctionButtonText => _actor != null ? _actor.FunctionButtonText : "";
        public Transform SubjectTransform => transform;
        public void InvokeFunction() => _actor?.InvokeFunction();

        public string GetInteractPrompt()
        {
            if (_actor == null || _actor.Identity == null) return "按 [E] 交互";
            return $"按 [E] 对话：{_actor.Identity.Name}";
        }

        public Transform GetTransform() => transform;

        public void Interact(GameObject interactor)
        {
            // Phase 4 修复：使用缓存的引用，避免重复查找导致持续报错
            if (_cachedDialogUI == null)
            {
                Debug.LogWarning("[ActorInteraction] Dialog UI 未找到，无法打开对话");
                return;
            }

            Debug.Log($"[ActorInteraction] 请求与 {_actor.name} 对话");
            _cachedDialogUI.Open(_actor); // _actor implements IDialogSubject
        }

        /// <summary>
        /// Phase 4 修复：清除缓存的 UI 引用（例如切换场景时）
        /// </summary>
        public static void ClearCache()
        {
            _cachedDialogUI = null;
            Debug.Log("[ActorInteraction] 已清除 Dialog UI 缓存");
        }
    }
}
