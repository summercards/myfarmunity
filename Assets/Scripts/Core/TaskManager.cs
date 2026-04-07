using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Core
{
    /// <summary>
    /// TaskManager: 统一的主线程轻量调度器（延迟/重复任务）。
    /// 设计目标：
    /// - 让高频/分散的 Update 逻辑集中到一个 Update，便于性能治理与可观测性；
    /// - 为 NPC/Actor 相关系统提供可取消的“定时 tick”，减少每个组件自己跑 Update/Coroutine 的需求。
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class TaskManager : MonoBehaviour
    {
        public interface ITaskHandle
        {
            bool IsCancelled { get; }
            void Cancel();
        }

        private sealed class TaskHandle : ITaskHandle
        {
            private readonly TaskManager _owner;
            private readonly int _id;
            private bool _cancelled;

            public bool IsCancelled => _cancelled;

            public TaskHandle(TaskManager owner, int id)
            {
                _owner = owner;
                _id = id;
            }

            public void Cancel()
            {
                if (_cancelled)
                {
                    return;
                }

                _cancelled = true;
                _owner?.CancelInternal(_id);
            }
        }

        private struct ScheduledTask
        {
            public int id;
            public float intervalSeconds;
            public float remainingSeconds;
            public bool repeating;
            public bool useUnscaledTime;
            public Action callback;
            public bool cancelled;
        }

        private static TaskManager instance;
        private static bool isShuttingDown;

        public static TaskManager Instance => EnsureInstance();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            isShuttingDown = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        public static TaskManager EnsureInstance()
        {
            if (isShuttingDown)
            {
                return null;
            }

            if (instance != null)
            {
                return instance;
            }

            // 确保 AppRoot 存在，并将 TaskManager 挂到其下跨场景常驻。
            AppRoot root = AppRoot.EnsureInstance();
            if (root == null)
            {
                return null;
            }

            GameObject go = new GameObject("__TaskManager");
            TaskManager mgr = go.AddComponent<TaskManager>();
            AppRoot.AttachPersistent(go);
            instance = mgr;
            return instance;
        }

        private int _nextId = 1;
        private readonly List<ScheduledTask> _tasks = new List<ScheduledTask>(64);

        public ITaskHandle ScheduleOnce(float delaySeconds, Action callback, bool useUnscaledTime = false)
        {
            if (callback == null)
            {
                return null;
            }

            if (delaySeconds < 0f)
            {
                delaySeconds = 0f;
            }

            int id = _nextId++;
            _tasks.Add(new ScheduledTask
            {
                id = id,
                intervalSeconds = delaySeconds,
                remainingSeconds = delaySeconds,
                repeating = false,
                useUnscaledTime = useUnscaledTime,
                callback = callback,
                cancelled = false
            });

            return new TaskHandle(this, id);
        }

        public ITaskHandle ScheduleRepeating(float intervalSeconds, Action callback, bool useUnscaledTime = false, float initialDelaySeconds = -1f)
        {
            if (callback == null)
            {
                return null;
            }

            if (intervalSeconds <= 0f)
            {
                intervalSeconds = 0.0001f; // 避免 0 导致卡死循环/每帧无限触发
            }

            if (initialDelaySeconds < 0f)
            {
                initialDelaySeconds = intervalSeconds;
            }

            int id = _nextId++;
            _tasks.Add(new ScheduledTask
            {
                id = id,
                intervalSeconds = intervalSeconds,
                remainingSeconds = Mathf.Max(0f, initialDelaySeconds),
                repeating = true,
                useUnscaledTime = useUnscaledTime,
                callback = callback,
                cancelled = false
            });

            return new TaskHandle(this, id);
        }

        private void Awake()
        {
            if (!RuntimeService.TryClaimSingleton(this, instance, nameof(TaskManager)))
            {
                return;
            }

            instance = this;
            gameObject.name = "__TaskManager";
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void CancelInternal(int id)
        {
            // O(n) 取消：任务量预期很小（几十级），简化实现。
            for (int i = 0; i < _tasks.Count; i++)
            {
                if (_tasks[i].id == id)
                {
                    ScheduledTask t = _tasks[i];
                    t.cancelled = true;
                    _tasks[i] = t;
                    return;
                }
            }
        }

        private void Update()
        {
            if (_tasks.Count == 0)
            {
                return;
            }

            float dt = Time.deltaTime;
            float udt = Time.unscaledDeltaTime;

            for (int i = 0; i < _tasks.Count; i++)
            {
                ScheduledTask t = _tasks[i];
                if (t.cancelled || t.callback == null)
                {
                    RemoveAtSwapBack(i--);
                    continue;
                }

                float step = t.useUnscaledTime ? udt : dt;
                t.remainingSeconds -= step;

                if (t.remainingSeconds > 0f)
                {
                    _tasks[i] = t;
                    continue;
                }

                try
                {
                    t.callback.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                if (t.repeating && !t.cancelled)
                {
                    // 不做“追帧”补偿，避免长时间卡顿后一次性执行多次导致更大尖刺。
                    t.remainingSeconds = t.intervalSeconds;
                    _tasks[i] = t;
                }
                else
                {
                    RemoveAtSwapBack(i--);
                }
            }
        }

        private void RemoveAtSwapBack(int index)
        {
            int last = _tasks.Count - 1;
            if (index < 0 || index > last)
            {
                return;
            }

            _tasks[index] = _tasks[last];
            _tasks.RemoveAt(last);
        }
    }
}

