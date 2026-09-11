namespace CardBase.Scripts;

public class EventBus
{
    public static EventBus Instance { get; } = new();
    public CardSystemEventBus CardSystemEventBus { get; } = new();
    public MatchEventBus MatchEventBus { get; } = new();
    public CombatEventBus CombatEventBus { get; } = new();
    public UiEventBus UiEventBus { get; } = new();

    public event System.EventHandler<EventBusTraceEventArgs> EventTraceEventHandler;

    private EventBus()
    {
    }

    public void EmitTrace(string busName, string eventName, object args)
    {
        EventTraceEventHandler?.Invoke(this, new EventBusTraceEventArgs(busName, eventName, args));
    }
}

public sealed class EventBusTraceEventArgs : System.EventArgs
{
    public string BusName { get; }
    public string EventName { get; }
    public object Args { get; }

    public EventBusTraceEventArgs(string busName, string eventName, object args)
    {
        BusName = busName;
        EventName = eventName;
        Args = args;
    }
}
