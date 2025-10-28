using UnityEngine;

namespace FarmGame.NPCSystem
{
    /// <summary>
    /// 负责：实例化模型、套 Animator、按时间段播放状态；
    ///       对话期间临时覆盖为“说话”状态，结束后恢复。
    /// </summary>
    [DisallowMultipleComponent]
    public class NPCVisualController : MonoBehaviour
    {
        public NPCDefinition definition;

        [Header("运行时")]
        public Transform modelRoot;
        public Animator animator;

        [Header("时钟")]
        public bool useSystemClock = true;
        [Range(0, 24)] public float manualHour = 10f;
        public float tickInterval = 2f;

        private float _tick;
        private string _currentState;
        private float _speedApplied = 1f;

        // 对话覆盖
        private bool _isTalking = false;
        private string _stateBeforeTalk = null;
        private float _speedBeforeTalk = 1f;

        private const string MODEL_NODE_NAME = "_Model";

        private void Awake()
        {
            SetupModelAndAnimator();
            EvaluateAndPlay(true);
        }

        private void Update()
        {
            if (_isTalking) return; // 对话时不跑日常刷新

            _tick += Time.deltaTime;
            if (_tick < tickInterval) return;
            _tick = 0f;
            EvaluateAndPlay(false);
        }

        public void BeginTalk()
        {
            if (definition == null || animator == null) return;
            if (_isTalking) return;

            _isTalking = true;
            _stateBeforeTalk = _currentState;
            _speedBeforeTalk = _speedApplied;

            var state = string.IsNullOrEmpty(definition.talkStateName) ? "Talk" : definition.talkStateName;
            animator.speed = Mathf.Max(0.1f, definition.talkSpeed);
            animator.CrossFade(state, definition.talkCrossFade, 0, 0f);

            _currentState = state;
            _speedApplied = animator.speed;
        }

        public void EndTalk()
        {
            if (definition == null || animator == null) return;
            if (!_isTalking) return;

            _isTalking = false;

            // 回到对话前的日常状态；若空则按时间段重新计算
            if (!string.IsNullOrEmpty(_stateBeforeTalk))
            {
                animator.speed = _speedBeforeTalk;
                animator.CrossFade(_stateBeforeTalk, 0.2f, 0, 0f);
                _currentState = _stateBeforeTalk;
                _speedApplied = _speedBeforeTalk;
            }
            else
            {
                EvaluateAndPlay(true);
            }

            _stateBeforeTalk = null;
        }

        private void SetupModelAndAnimator()
        {
            // 找/建模型根
            var t = transform.Find(MODEL_NODE_NAME);
            if (t == null)
            {
                var go = new GameObject(MODEL_NODE_NAME);
                go.transform.SetParent(transform, false);
                t = go.transform;
            }
            modelRoot = t;

            // 如果有 Prefab，实例化（清理旧子物体）
            if (definition != null && definition.modelPrefab != null)
            {
                foreach (Transform c in modelRoot) DestroyImmediate(c.gameObject);
                var inst = Instantiate(definition.modelPrefab, modelRoot);
                inst.name = definition.modelPrefab.name;
            }

            // 位置/旋转/缩放
            if (definition != null)
            {
                modelRoot.localPosition = definition.modelLocalPosition;
                modelRoot.localEulerAngles = definition.modelLocalEuler;
                modelRoot.localScale = definition.modelLocalScale;
            }

            // Animator
            animator = modelRoot.GetComponentInChildren<Animator>(true);
            if (animator == null) animator = modelRoot.gameObject.AddComponent<Animator>();
            if (definition != null && definition.animatorController != null)
                animator.runtimeAnimatorController = definition.animatorController;

            animator.applyRootMotion = false;
        }

        private float GetHour()
        {
            if (useSystemClock)
            {
                var now = System.DateTime.Now;
                return now.Hour + now.Minute / 60f + now.Second / 3600f;
            }
            return Mathf.Repeat(manualHour, 24f);
        }

        private void EvaluateAndPlay(bool force)
        {
            if (definition == null || animator == null) return;

            float hour = GetHour();
            string targetState = null;
            float targetSpeed = 1f;

            if (definition.dailyAnimations != null)
            {
                foreach (var e in definition.dailyAnimations)
                {
                    if (e == null) continue;
                    if (e.ContainsHour(hour))
                    {
                        targetState = string.IsNullOrEmpty(e.stateName) ? "Idle" : e.stateName;
                        targetSpeed = Mathf.Max(0.1f, e.speed);
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(targetState))
            {
                targetState = "Idle";
                targetSpeed = 1f;
            }

            if (force || _currentState != targetState || Mathf.Abs(_speedApplied - targetSpeed) > 0.001f)
            {
                _currentState = targetState;
                _speedApplied = targetSpeed;
                animator.speed = _speedApplied;
                animator.CrossFade(_currentState, 0.2f, 0, 0f);
            }
        }
    }
}
