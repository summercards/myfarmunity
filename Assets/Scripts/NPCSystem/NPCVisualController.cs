using UnityEngine;

namespace FarmGame.NPCSystem
{
    /// <summary>
    /// 负责：实例化模型、套 Animator、按时间段播放对应状态。
    /// </summary>
    [DisallowMultipleComponent]
    public class NPCVisualController : MonoBehaviour
    {
        public NPCDefinition definition;

        [Header("运行时")]
        public Transform modelRoot;       // 实例化的模型根（名：_Model）
        public Animator animator;         // 模型上的 Animator

        [Header("时钟")]
        public bool useSystemClock = true;     // 用系统时间（小时 0-24）
        [Range(0, 24)] public float manualHour = 10f; // 手动调时间，useSystemClock=false 时生效
        public float tickInterval = 2f;        // 多久检测一次

        private float _tick;
        private string _currentState;
        private float _speedApplied = 1f;

        private const string MODEL_NODE_NAME = "_Model";

        private void Awake()
        {
            SetupModelAndAnimator();
            ForceEvaluateAnimation(true);
        }

        private void OnEnable()
        {
            _tick = 0f;
        }

        private void Update()
        {
            _tick += Time.deltaTime;
            if (_tick < tickInterval) return;
            _tick = 0f;
            ForceEvaluateAnimation(false);
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

            // 如果有 Prefab，且当前没有子模型，就实例化
            if (definition != null && definition.modelPrefab != null)
            {
                if (modelRoot.childCount == 0 ||
                    (modelRoot.childCount == 1 && modelRoot.GetChild(0).name.StartsWith("Placeholder")))
                {
                    foreach (Transform c in modelRoot) DestroyImmediate(c.gameObject);
                    var inst = Instantiate(definition.modelPrefab, modelRoot);
                    inst.name = definition.modelPrefab.name;
                }
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
            if (animator == null)
            {
                // 尝试在根挂一个 Animator
                animator = modelRoot.gameObject.AddComponent<Animator>();
            }
            if (definition != null && definition.animatorController != null)
            {
                animator.runtimeAnimatorController = definition.animatorController;
            }
            animator.applyRootMotion = false;
        }

        private float GetHour01_24()
        {
            if (useSystemClock)
            {
                var now = System.DateTime.Now;
                return now.Hour + now.Minute / 60f + now.Second / 3600f;
            }
            else
            {
                return Mathf.Repeat(manualHour, 24f);
            }
        }

        private void ForceEvaluateAnimation(bool forcePlay)
        {
            if (definition == null || animator == null) return;

            float hour = GetHour01_24();
            // 选中第一个匹配的动画
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

            if (forcePlay || _currentState != targetState || Mathf.Abs(_speedApplied - targetSpeed) > 0.001f)
            {
                _currentState = targetState;
                _speedApplied = targetSpeed;
                animator.speed = _speedApplied;

                // 0.2s 过渡
                animator.CrossFade(_currentState, 0.2f, 0, 0f);
            }
        }
    }
}
