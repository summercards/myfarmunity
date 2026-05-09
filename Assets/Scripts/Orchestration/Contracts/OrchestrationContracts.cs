using System;

namespace FarmGame.Orchestration
{
    public interface IGameServiceRegistry
    {
        bool Register<TService>(TService service, bool replaceExisting = false) where TService : class;
        bool TryGet<TService>(out TService service) where TService : class;
        bool Unregister<TService>(TService service = null) where TService : class;
        bool Contains<TService>() where TService : class;
        void Clear();
    }

    public interface IGameEventBus
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
        void Publish<TEvent>(TEvent gameEvent);
        void Clear();
    }

    public interface IGameCommandBus
    {
        bool RegisterHandler<TCommand>(Func<TCommand, CommandResult> handler);
        bool UnregisterHandler<TCommand>(Func<TCommand, CommandResult> handler = null);
        bool HasHandler<TCommand>();
        CommandResult Execute<TCommand>(TCommand command);
        void Clear();
    }

    public interface ITimeService
    {
        int Hour { get; }
        int Minute { get; }
        int Day { get; }
        int Month { get; }
        int Year { get; }
        Season CurrentSeason { get; }
        WeatherType CurrentWeather { get; }
        TimeOfDay CurrentTimeOfDay { get; }
        bool IsPaused { get; }
        string TimeString { get; }
        string DateString { get; }
        void Pause();
        void Resume();
        void SetTime(int hour, int minute);
        void FastForward(int hours);
    }

    public interface IWalletService
    {
        int Coins { get; }
        bool CanAfford(int cost);
        bool TrySpend(int cost);
        void Add(int amount);
    }

    public interface IGameStateService
    {
        void Set<TState>(string key, TState value);
        bool TryGet<TState>(string key, out TState value);
        bool Remove(string key);
        void Clear();
    }

    public interface IGameRuntimeContext
    {
        IGameServiceRegistry Services { get; }
        IGameEventBus Events { get; }
        IGameCommandBus Commands { get; }
        IGameStateService State { get; }
    }

    public readonly struct CommandResult
    {
        public bool Success { get; }
        public string Message { get; }

        private CommandResult(bool success, string message)
        {
            Success = success;
            Message = message ?? string.Empty;
        }

        public static CommandResult Ok(string message = "")
        {
            return new CommandResult(true, message);
        }

        public static CommandResult Fail(string message)
        {
            return new CommandResult(false, string.IsNullOrWhiteSpace(message) ? "Command failed." : message);
        }

        public override string ToString()
        {
            return Success ? $"Success: {Message}" : $"Failed: {Message}";
        }
    }
}
