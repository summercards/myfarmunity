using UnityEngine;
using System.Collections.Generic;

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
        private Coroutine _typewriterCoroutine;
        private int _currentTypewriterIndex;
        private string _fullText = "";
        private System.Action _onDialogueComplete;

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

            _fullText = text;
            _isTyping = enableTypewriter;
            _onDialogueComplete = onComplete;

            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }

            if (enableTypewriter)
            {
                _typewriterCoroutine = StartCoroutine(TypewriterEffect());
            }
            else
            {
                // 直接显示完整文本
                UpdateDialogueText(text);
            }

            // Phase 7: 通知 UI 对话更新
            NotifyDialogueUpdate();
        }

        /// <summary>
        /// Phase 7: 打字机效果协程
        /// </summary>
        private System.Collections.IEnumerator TypewriterEffect()
        {
            _currentTypewriterIndex = 0;
            UpdateDialogueText("");

            while (_currentTypewriterIndex < _fullText.Length)
            {
                _currentTypewriterIndex++;
                string currentText = _fullText.Substring(0, _currentTypewriterIndex);
                UpdateDialogueText(currentText);
                yield return new WaitForSeconds(typewriterSpeed);
            }

            // 打字完成
            _isTyping = false;

            // Phase 7: 触发完成回调
            _onDialogueComplete?.Invoke();

            // 自动关闭
            if (autoCloseAfterDialogue)
            {
                yield return new WaitForSeconds(autoCloseDelay);
                CloseDialogue();
            }
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
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            _isTyping = false;
            UpdateDialogueText(_fullText);
            _onDialogueComplete?.Invoke();

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

            // 停止打字机
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            _isTyping = false;
            _typewriterCoroutine = null;

            Debug.Log("[DialogueModule] 已禁用");
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
