using System;

namespace CardBase.Scripts;

public class EventBus
{
    public CardSystemEventBus CardSystemEventBus { get; } = new();
    public MatchEventBus MatchEventBus { get; } = new();
    public CombatEventBus CombatEventBus { get; } = new();
}