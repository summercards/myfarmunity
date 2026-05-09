using System;
using System.Collections.Generic;
using FarmGame.Core;
using UnityEngine;

namespace FarmGame.Orchestration
{
    [DefaultExecutionOrder(-850)]
    public sealed class GameRuntimeContext : MonoBehaviour, IGameRuntimeContext
    {
        private static GameRuntimeContext instance;
        private static bool isShuttingDown;

        [SerializeField] private bool logLifecycle = false;

        private GameServiceRegistry serviceRegistry;
        private GameEventBus eventBus;
        private GameCommandBus commandBus;
        private GameStateService stateService;

        public static GameRuntimeContext Instance => EnsureInstance();
        public static bool IsAvailable => instance != null && !isShuttingDown;

        public IGameServiceRegistry Services => serviceRegistry;
        public IGameEventBus Events => eventBus;
        public IGameCommandBus Commands => commandBus;
        public IGameStateService State => stateService;

        public static IGameServiceRegistry ServiceRegistry => EnsureInstance()?.Services;
        public static IGameEventBus EventBus => EnsureInstance()?.Events;
        public static IGameCommandBus CommandBus => EnsureInstance()?.Commands;
        public static IGameStateService StateService => EnsureInstance()?.State;

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

        public static GameRuntimeContext EnsureInstance()
        {
            if (isShuttingDown)
            {
                return null;
            }

            if (instance != null)
            {
                return instance;
            }

            AppRoot root = AppRoot.EnsureInstance();
            if (root == null)
            {
                return null;
            }

            GameObject go = new GameObject("__GameRuntimeContext");
            instance = go.AddComponent<GameRuntimeContext>();
            AppRoot.AttachPersistent(go);
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            gameObject.name = "__GameRuntimeContext";
            AppRoot.AttachPersistent(gameObject);
            InitializeCoreServices();

            if (logLifecycle)
            {
                Debug.Log("[GameRuntimeContext] Initialized.");
            }
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            serviceRegistry?.Clear();
            eventBus?.Clear();
            commandBus?.Clear();
            stateService?.Clear();
            instance = null;
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void InitializeCoreServices()
        {
            if (serviceRegistry == null)
            {
                serviceRegistry = new GameServiceRegistry();
            }

            if (eventBus == null)
            {
                eventBus = new GameEventBus();
            }

            if (commandBus == null)
            {
                commandBus = new GameCommandBus();
            }

            if (stateService == null)
            {
                stateService = new GameStateService();
            }

            serviceRegistry.Register<IGameRuntimeContext>(this, replaceExisting: true);
            serviceRegistry.Register<IGameServiceRegistry>(serviceRegistry, replaceExisting: true);
            serviceRegistry.Register<IGameEventBus>(eventBus, replaceExisting: true);
            serviceRegistry.Register<IGameCommandBus>(commandBus, replaceExisting: true);
            serviceRegistry.Register<IGameStateService>(stateService, replaceExisting: true);
        }
    }

    public sealed class GameServiceRegistry : IGameServiceRegistry
    {
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        public bool Register<TService>(TService service, bool replaceExisting = false) where TService : class
        {
            if (service == null)
            {
                return false;
            }

            Type key = typeof(TService);
            if (services.ContainsKey(key) && !replaceExisting)
            {
                return false;
            }

            services[key] = service;
            return true;
        }

        public bool TryGet<TService>(out TService service) where TService : class
        {
            if (services.TryGetValue(typeof(TService), out object value) && value is TService typed)
            {
                service = typed;
                return true;
            }

            service = null;
            return false;
        }

        public bool Unregister<TService>(TService service = null) where TService : class
        {
            Type key = typeof(TService);
            if (!services.TryGetValue(key, out object existing))
            {
                return false;
            }

            if (service != null && !ReferenceEquals(existing, service))
            {
                return false;
            }

            services.Remove(key);
            return true;
        }

        public bool Contains<TService>() where TService : class
        {
            return services.ContainsKey(typeof(TService));
        }

        public void Clear()
        {
            services.Clear();
        }
    }

    public sealed class GameEventBus : IGameEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> handlersByType = new Dictionary<Type, List<Delegate>>();

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                return EmptySubscription.Instance;
            }

            Type key = typeof(TEvent);
            if (!handlersByType.TryGetValue(key, out List<Delegate> handlers))
            {
                handlers = new List<Delegate>();
                handlersByType[key] = handlers;
            }

            handlers.Add(handler);
            return new EventSubscription<TEvent>(this, handler);
        }

        public void Publish<TEvent>(TEvent gameEvent)
        {
            if (!handlersByType.TryGetValue(typeof(TEvent), out List<Delegate> handlers) || handlers.Count == 0)
            {
                return;
            }

            Delegate[] snapshot = handlers.ToArray();
            foreach (Delegate handler in snapshot)
            {
                Action<TEvent> typedHandler = handler as Action<TEvent>;
                if (typedHandler == null)
                {
                    continue;
                }

                try
                {
                    typedHandler.Invoke(gameEvent);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public void Clear()
        {
            handlersByType.Clear();
        }

        private void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                return;
            }

            Type key = typeof(TEvent);
            if (!handlersByType.TryGetValue(key, out List<Delegate> handlers))
            {
                return;
            }

            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                handlersByType.Remove(key);
            }
        }

        private sealed class EventSubscription<TEvent> : IDisposable
        {
            private GameEventBus owner;
            private Action<TEvent> handler;

            public EventSubscription(GameEventBus owner, Action<TEvent> handler)
            {
                this.owner = owner;
                this.handler = handler;
            }

            public void Dispose()
            {
                owner?.Unsubscribe(handler);
                owner = null;
                handler = null;
            }
        }

        private sealed class EmptySubscription : IDisposable
        {
            public static readonly EmptySubscription Instance = new EmptySubscription();

            public void Dispose()
            {
            }
        }
    }

    public sealed class GameCommandBus : IGameCommandBus
    {
        private readonly Dictionary<Type, Delegate> handlersByType = new Dictionary<Type, Delegate>();

        public bool RegisterHandler<TCommand>(Func<TCommand, CommandResult> handler)
        {
            if (handler == null)
            {
                return false;
            }

            Type key = typeof(TCommand);
            if (handlersByType.ContainsKey(key))
            {
                return false;
            }

            handlersByType[key] = handler;
            return true;
        }

        public bool UnregisterHandler<TCommand>(Func<TCommand, CommandResult> handler = null)
        {
            Type key = typeof(TCommand);
            if (!handlersByType.TryGetValue(key, out Delegate existing))
            {
                return false;
            }

            if (handler != null && !ReferenceEquals(existing, handler))
            {
                return false;
            }

            handlersByType.Remove(key);
            return true;
        }

        public bool HasHandler<TCommand>()
        {
            return handlersByType.ContainsKey(typeof(TCommand));
        }

        public CommandResult Execute<TCommand>(TCommand command)
        {
            Type key = typeof(TCommand);
            if (!handlersByType.TryGetValue(key, out Delegate handler))
            {
                return CommandResult.Fail($"No command handler registered for {key.Name}.");
            }

            Func<TCommand, CommandResult> typedHandler = handler as Func<TCommand, CommandResult>;
            if (typedHandler == null)
            {
                return CommandResult.Fail($"Command handler for {key.Name} has an invalid signature.");
            }

            try
            {
                return typedHandler.Invoke(command);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return CommandResult.Fail(exception.Message);
            }
        }

        public void Clear()
        {
            handlersByType.Clear();
        }
    }

    public sealed class GameStateService : IGameStateService
    {
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public void Set<TState>(string key, TState value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            values[key] = value;
        }

        public bool TryGet<TState>(string key, out TState value)
        {
            if (!string.IsNullOrWhiteSpace(key) &&
                values.TryGetValue(key, out object rawValue) &&
                rawValue is TState typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default(TState);
            return false;
        }

        public bool Remove(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && values.Remove(key);
        }

        public void Clear()
        {
            values.Clear();
        }
    }
}
