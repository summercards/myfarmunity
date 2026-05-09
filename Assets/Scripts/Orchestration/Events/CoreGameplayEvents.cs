namespace FarmGame.Orchestration
{
    public readonly struct TimeSystemChangedEvent
    {
        public ITimeService TimeService { get; }

        public TimeSystemChangedEvent(ITimeService timeService)
        {
            TimeService = timeService;
        }
    }

    public readonly struct GameHourChangedEvent
    {
        public int Hour { get; }
        public int Minute { get; }
        public string TimeString { get; }

        public GameHourChangedEvent(int hour, int minute, string timeString)
        {
            Hour = hour;
            Minute = minute;
            TimeString = timeString;
        }
    }

    public readonly struct GameDayChangedEvent
    {
        public int Year { get; }
        public int Month { get; }
        public int Day { get; }
        public string DateString { get; }

        public GameDayChangedEvent(int year, int month, int day, string dateString)
        {
            Year = year;
            Month = month;
            Day = day;
            DateString = dateString;
        }
    }

    public readonly struct SeasonChangedEvent
    {
        public Season Season { get; }

        public SeasonChangedEvent(Season season)
        {
            Season = season;
        }
    }

    public readonly struct WeatherChangedEvent
    {
        public WeatherType Weather { get; }

        public WeatherChangedEvent(WeatherType weather)
        {
            Weather = weather;
        }
    }

    public readonly struct WalletChangedEvent
    {
        public int Coins { get; }

        public WalletChangedEvent(int coins)
        {
            Coins = coins;
        }
    }
}
