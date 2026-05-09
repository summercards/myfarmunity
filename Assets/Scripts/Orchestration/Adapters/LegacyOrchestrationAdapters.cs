using System;
using FarmGame.Core;
using UnityEngine;

namespace FarmGame.Orchestration
{
    [DefaultExecutionOrder(-820)]
    public sealed class LegacyOrchestrationAdapters : MonoBehaviour
    {
        private static LegacyOrchestrationAdapters instance;
        private static bool isShuttingDown;

        private TimeServiceAdapter timeAdapter;
        private WalletServiceAdapter walletAdapter;
        private ShopCommandAdapter shopCommandAdapter;

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

        public static LegacyOrchestrationAdapters EnsureInstance()
        {
            if (isShuttingDown)
            {
                return null;
            }

            if (instance != null)
            {
                return instance;
            }

            GameRuntimeContext context = GameRuntimeContext.EnsureInstance();
            if (context == null)
            {
                return null;
            }

            GameObject go = new GameObject("__LegacyOrchestrationAdapters");
            instance = go.AddComponent<LegacyOrchestrationAdapters>();
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
            gameObject.name = "__LegacyOrchestrationAdapters";
            AppRoot.AttachPersistent(gameObject);
        }

        private void OnEnable()
        {
            RuntimeRefs.TimeSystemChanged += BindTimeSystem;
            RuntimeRefs.PlayerWalletChanged += BindWallet;

            BindTimeSystem(RuntimeRefs.TimeSystem);
            BindWallet(RuntimeRefs.PlayerWallet);
            EnsureShopCommandAdapter();
        }

        private void OnDisable()
        {
            RuntimeRefs.TimeSystemChanged -= BindTimeSystem;
            RuntimeRefs.PlayerWalletChanged -= BindWallet;

            DisposeTimeAdapter();
            DisposeWalletAdapter();
            DisposeShopCommandAdapter();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void BindTimeSystem(GameTimeSystem timeSystem)
        {
            if (timeAdapter != null && timeAdapter.Source == timeSystem)
            {
                return;
            }

            DisposeTimeAdapter();
            if (timeSystem == null)
            {
                return;
            }

            GameRuntimeContext context = GameRuntimeContext.EnsureInstance();
            if (context == null)
            {
                return;
            }

            timeAdapter = new TimeServiceAdapter(timeSystem, context.Events);
            context.Services.Register<ITimeService>(timeAdapter, replaceExisting: true);
            context.Events.Publish(new TimeSystemChangedEvent(timeAdapter));
        }

        private void BindWallet(PlayerWallet wallet)
        {
            if (walletAdapter != null && walletAdapter.Source == wallet)
            {
                return;
            }

            DisposeWalletAdapter();
            if (wallet == null)
            {
                return;
            }

            GameRuntimeContext context = GameRuntimeContext.EnsureInstance();
            if (context == null)
            {
                return;
            }

            walletAdapter = new WalletServiceAdapter(wallet, context.Events);
            context.Services.Register<IWalletService>(walletAdapter, replaceExisting: true);
            context.Events.Publish(new WalletChangedEvent(wallet.coins));
        }

        private void EnsureShopCommandAdapter()
        {
            if (shopCommandAdapter != null)
            {
                return;
            }

            GameRuntimeContext context = GameRuntimeContext.EnsureInstance();
            if (context == null)
            {
                return;
            }

            shopCommandAdapter = new ShopCommandAdapter(context);
            shopCommandAdapter.Register();
        }

        private void DisposeTimeAdapter()
        {
            if (timeAdapter == null)
            {
                return;
            }

            GameRuntimeContext context = GameRuntimeContext.IsAvailable ? GameRuntimeContext.Instance : null;
            if (context != null)
            {
                context.Services.Unregister<ITimeService>(timeAdapter);
            }

            timeAdapter.Dispose();
            timeAdapter = null;
        }

        private void DisposeWalletAdapter()
        {
            if (walletAdapter == null)
            {
                return;
            }

            GameRuntimeContext context = GameRuntimeContext.IsAvailable ? GameRuntimeContext.Instance : null;
            if (context != null)
            {
                context.Services.Unregister<IWalletService>(walletAdapter);
            }

            walletAdapter.Dispose();
            walletAdapter = null;
        }

        private void DisposeShopCommandAdapter()
        {
            if (shopCommandAdapter == null)
            {
                return;
            }

            shopCommandAdapter.Dispose();
            shopCommandAdapter = null;
        }
    }

    public sealed class TimeServiceAdapter : ITimeService, IDisposable
    {
        private readonly IGameEventBus eventBus;

        public TimeServiceAdapter(GameTimeSystem source, IGameEventBus eventBus)
        {
            Source = source;
            this.eventBus = eventBus;
            Attach();
        }

        public GameTimeSystem Source { get; }
        public int Hour => Source != null ? Source.Hour : 0;
        public int Minute => Source != null ? Source.Minute : 0;
        public int Day => Source != null ? Source.Day : 1;
        public int Month => Source != null ? Source.Month : 1;
        public int Year => Source != null ? Source.Year : 2024;
        public Season CurrentSeason => Source != null ? Source.Season : global::Season.Spring;
        public WeatherType CurrentWeather => Source != null ? Source.CurrentWeather : WeatherType.Sunny;
        public TimeOfDay CurrentTimeOfDay => Source != null ? Source.CurrentTimeOfDay : global::TimeOfDay.Morning;
        public bool IsPaused => Source != null && Source.IsPaused;
        public string TimeString => Source != null ? Source.TimeString : "00:00";
        public string DateString => Source != null ? Source.DateString : "2024年1月1日";

        public void Pause()
        {
            Source?.Pause();
        }

        public void Resume()
        {
            Source?.Resume();
        }

        public void SetTime(int hour, int minute)
        {
            Source?.SetTime(hour, minute);
        }

        public void FastForward(int hours)
        {
            Source?.FastForward(hours);
        }

        public void Dispose()
        {
            Detach();
        }

        private void Attach()
        {
            if (Source == null)
            {
                return;
            }

            Source.onHourChanged.AddListener(HandleHourChanged);
            Source.onDayChanged.AddListener(HandleDayChanged);
            Source.onSeasonChanged.AddListener(HandleSeasonChanged);
            Source.onWeatherChanged.AddListener(HandleWeatherChanged);
        }

        private void Detach()
        {
            if (Source == null)
            {
                return;
            }

            Source.onHourChanged.RemoveListener(HandleHourChanged);
            Source.onDayChanged.RemoveListener(HandleDayChanged);
            Source.onSeasonChanged.RemoveListener(HandleSeasonChanged);
            Source.onWeatherChanged.RemoveListener(HandleWeatherChanged);
        }

        private void HandleHourChanged()
        {
            eventBus?.Publish(new GameHourChangedEvent(Hour, Minute, TimeString));
        }

        private void HandleDayChanged()
        {
            eventBus?.Publish(new GameDayChangedEvent(Year, Month, Day, DateString));
        }

        private void HandleSeasonChanged()
        {
            eventBus?.Publish(new SeasonChangedEvent(CurrentSeason));
        }

        private void HandleWeatherChanged()
        {
            eventBus?.Publish(new WeatherChangedEvent(CurrentWeather));
        }
    }

    public sealed class WalletServiceAdapter : IWalletService, IDisposable
    {
        private readonly IGameEventBus eventBus;

        public WalletServiceAdapter(PlayerWallet source, IGameEventBus eventBus)
        {
            Source = source;
            this.eventBus = eventBus;
            Attach();
        }

        public PlayerWallet Source { get; }
        public int Coins => Source != null ? Source.coins : 0;

        public bool CanAfford(int cost)
        {
            return Source != null && Source.CanAfford(cost);
        }

        public bool TrySpend(int cost)
        {
            return Source != null && Source.TrySpend(cost);
        }

        public void Add(int amount)
        {
            Source?.Add(amount);
        }

        public void Dispose()
        {
            Detach();
        }

        private void Attach()
        {
            if (Source != null)
            {
                Source.onCoinsChanged.AddListener(HandleCoinsChanged);
            }
        }

        private void Detach()
        {
            if (Source != null)
            {
                Source.onCoinsChanged.RemoveListener(HandleCoinsChanged);
            }
        }

        private void HandleCoinsChanged(int coins)
        {
            eventBus?.Publish(new WalletChangedEvent(coins));
        }
    }

    public sealed class ShopCommandAdapter : IDisposable
    {
        private readonly GameRuntimeContext context;

        public ShopCommandAdapter(GameRuntimeContext context)
        {
            this.context = context;
        }

        public void Register()
        {
            if (context != null)
            {
                context.Commands.RegisterHandler<BuyShopItemCommand>(HandleBuyShopItem);
            }
        }

        public void Dispose()
        {
            if (context != null)
            {
                context.Commands.UnregisterHandler<BuyShopItemCommand>(HandleBuyShopItem);
            }
        }

        private CommandResult HandleBuyShopItem(BuyShopItemCommand command)
        {
            if (command.Catalog == null)
            {
                return CommandResult.Fail("商店目录为空，无法购买。");
            }

            ShopCatalogSO.ShopEntrySO entry = command.Catalog.Get(command.ItemId);
            if (entry == null)
            {
                return CommandResult.Fail($"商店目录中不存在商品: {command.ItemId}");
            }

            int quantity = Mathf.Max(1, command.Quantity);
            int totalCost = Mathf.Max(0, entry.buyPrice) * quantity;

            IWalletService wallet = null;
            if (context != null)
            {
                context.Services.TryGet<IWalletService>(out wallet);
            }
            if (wallet == null)
            {
                return CommandResult.Fail("没有可用的钱包服务。");
            }

            if (!wallet.TrySpend(totalCost))
            {
                return CommandResult.Fail("金币不足。");
            }

            InventoryBridge inventoryBridge = RuntimeRefs.InventoryBridge;
            if (inventoryBridge == null)
            {
                wallet.Add(totalCost);
                return CommandResult.Fail("没有可用的背包桥接，已退回金币。");
            }

            Transform player = command.PlayerTransform != null ? command.PlayerTransform : RuntimeRefs.PlayerTransform;
            bool added = inventoryBridge.TryAdd(entry.itemId, quantity, player, entry.pickupPrefab);
            if (!added)
            {
                wallet.Add(totalCost);
                return CommandResult.Fail("添加物品失败，已退回金币。");
            }

            return CommandResult.Ok($"购买成功: {entry.displayName}");
        }
    }
}
