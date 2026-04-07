using UnityEngine;
using FarmGame.Core;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// DialogueModule: 负责"对话"能力模块
    /// Phase 7: 管理对话相关逻辑，包括打字机效果、对话选项等。
    /// - 继承 ActorModule，提供启用/禁用接口
    /// </summary>
    public class DialogueModule : ActorModule
    {
        // 对话配置
        [Header("Typewriter Settings")]
        [Tooltip("是否启用打字机效果（逐字显示）")]
        public bool enableTypewriter = true;
        [Range(0.01f, 0.2f)] public float typewriterSpeed = 0.05f;

        [Header("Options")]
        [Tooltip("对话结束后是否自动关闭对话框")]
        public bool autoCloseAfterDialogue = false;
        [Range(0f, 5f)] public float autoCloseDelay = 1f;

        // 内部状态
        private bool _isTyping;
        private int _currentTypewriterIndex;
        private string _fullText = "";
        private System.Action _onDialogueComplete;
        private TaskManager.ITaskHandle _typewriterTask;
        private TaskManager.ITaskHandle _autoCloseTask;

        /// <summary>
        /// Phase 7: 启用打字机效果
        /// </summary>
        public void PlayDialogue(string text, System.Action onComplete = null)
        {
            if (_memory == null)
            {
                Debug.LogWarning("[DialogueModule] Memory 组件缺失");
                return;
            }

            CancelScheduledTasks();

            _fullText = text;
            _onDialogueComplete = onComplete;
            _isTyping = enableTypewriter;

            if (enableTypewriter)
            {
                StartTypewriter();
            }
            else
            {
                // 直接显示完整文本
                UpdateDialogueText(text);
                CompleteDialogueInternal();
            }

            // Phase 7: 通知 UI 对话更新
            NotifyDialogueUpdate();
        }

        private void StartTypewriter()
        {
            _currentTypewriterIndex = 0;
            UpdateDialogueText("");

            TaskManager mgr = TaskManager.Instance;
            if (mgr == null)
            {
                // 极端情况下（退出/关机流程）兜底：直接展示全文
                UpdateDialogueText(_fullText);
                CompleteDialogueInternal();
                return;
            }

            _typewriterTask = mgr.ScheduleRepeating(
                intervalSeconds: Mathf.Max(0.001f, typewriterSpeed),
                callback: StepTypewriter,
                useUnscaledTime: false,
                initialDelaySeconds: Mathf.Max(0.001f, typewriterSpeed)
            );
        }

        private void StepTypewriter()
        {
            if (!_isTyping)
            {
                _typewriterTask?.Cancel();
                _typewriterTask = null;
                return;
            }

            if (_currentTypewriterIndex >= _fullText.Length)
            {
                _typewriterTask?.Cancel();
                _typewriterTask = null;
                _isTyping = false;
                CompleteDialogueInternal();
                return;
            }

            _currentTypewriterIndex++;
            string currentText = _fullText.Substring(0, _currentTypewriterIndex);
            UpdateDialogueText(currentText);
        }

        private void CompleteDialogueInternal()
        {
            _isTyping = false;
            _onDialogueComplete?.Invoke();

            if (!autoCloseAfterDialogue)
            {
                return;
            }

            TaskManager mgr = TaskManager.Instance;
            if (mgr == null)
            {
                return;
            }

            _autoCloseTask = mgr.ScheduleOnce(
                delaySeconds: Mathf.Max(0f, autoCloseDelay),
                callback: CloseDialogue,
                useUnscaledTime: false
            );
        }

        /// <summary>
        /// Phase 7: 更新对话文本（通知 ActorDialogue）
        /// </summary>
        private void UpdateDialogueText(string text)
        {
            // Phase 7: 通过 Actor 更新对话内容
            // 这里可以添加打字机相关的动画、声音等
            
            // 临时：直接打印日志
            if (!string.IsNullOrEmpty(text))
            {
                Debug.Log($"[DialogueModule] 显示台词：{text}");
            }
        }

        /// <summary>
        /// Phase 7: 跳过剩余打字机（立即显示全部）
        /// </summary>
        public void SkipTypewriter()
        {
            CancelScheduledTasks();

            UpdateDialogueText(_fullText);
            CompleteDialogueInternal();

            Debug.Log("[DialogueModule] 打字机已跳过");
        }

        /// <summary>
        /// Phase 7: 关闭对话
        /// </summary>
        public void CloseDialogue()
        {
            // Phase 7: 通知 ActorBrain 对话结束
            if (_actor != null)
            {
                var brain = _actor.GetComponent<ActorBrain>();
                if (brain != null)
                {
                    brain.EndTalking();
                }
            }
        }

        /// <summary>
        /// Phase 7: 启用模块时的初始化
        /// </summary>
        protected override void OnEnabled()
        {
            base.OnEnabled();
            Debug.Log("[DialogueModule] 已启用");
        }

        /// <summary>
        /// Phase 7: 禁用模块时的清理
        /// </summary>
        protected override void OnDisabled()
        {
            base.OnDisabled();

            CancelScheduledTasks();

            Debug.Log("[DialogueModule] 已禁用");
        }

        private void CancelScheduledTasks()
        {
            _typewriterTask?.Cancel();
            _typewriterTask = null;
            _autoCloseTask?.Cancel();
            _autoCloseTask = null;
            _isTyping = false;
        }

        /// <summary>
        /// Phase 7: 通知 UI 对话更新（内部方法，未来可改为事件）
        /// </summary>
        private void NotifyDialogueUpdate()
        {
            // Phase 7: 通过 Actor 通知 UI 刷新对话显示
            // 临时实现：直接打印
            // 未来：通过 Actor.DialogueUpdated 事件通知
        }
    }
}
