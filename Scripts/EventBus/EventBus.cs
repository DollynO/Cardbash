namespace CardBase.Scripts;

public class EventBus
{
    public static EventBus Instance { get; } = new();

    public CardSystemEventBus CardSystemEventBus { get; } = new();
    public MatchEventBus MatchEventBus { get; } = new();
    public CombatEventBus CombatEventBus { get; } = new();

    private EventBus()
    {
    }
}
