using UnityEngine;

/// <summary>
/// 统一的跨场景根节点。
/// 所有真正需要跨场景存活的服务都挂到这里，避免各自维护生命周期。
/// </summary>
namespace FarmGame.Core
{
    [DefaultExecutionOrder(-1000)]
    public sealed class AppRoot : MonoBehaviour
    {
        private static AppRoot instance;
        private static bool isShuttingDown;

        public static AppRoot Instance => EnsureInstance();

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

        public static AppRoot EnsureInstance()
        {
            if (isShuttingDown)
            {
                return null;
            }

            if (instance != null)
            {
                return instance;
            }

            GameObject root = new GameObject("__AppRoot");
            instance = root.AddComponent<AppRoot>();
            DontDestroyOnLoad(root);
            return instance;
        }

        public static void AttachPersistent(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            AppRoot root = EnsureInstance();
            if (root == null)
            {
                return;
            }

            if (target.transform.parent != root.transform)
            {
                target.transform.SetParent(root.transform, true);
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                gameObject.name = "__AppRoot";
                DontDestroyOnLoad(gameObject);
                return;
            }

            if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }
    }

    /// <summary>
    /// 持久服务的通用注册辅助。
    /// </summary>
    public static class RuntimeService
    {
        public static bool TryClaimSingleton<T>(T candidate, T current, string serviceName, bool logDuplicates = true)
            where T : MonoBehaviour
        {
            if (candidate == null)
            {
                return false;
            }

            if (current != null && current != candidate)
            {
                if (logDuplicates)
                {
                    Debug.Log($"[AppRoot] 检测到重复 {serviceName}，销毁: {candidate.gameObject.name}");
                }

                Object.Destroy(candidate.gameObject);
                return false;
            }

            AppRoot.AttachPersistent(candidate.gameObject);
            return true;
        }
    }
}
