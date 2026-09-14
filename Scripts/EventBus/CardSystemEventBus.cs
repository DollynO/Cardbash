using System;

namespace CardBase.Scripts;

public class CardSystemEventBus
{
    public event EventHandler<CardEventArgs> CardLockedEventHandler;
    public void EmitCardLocked(CardEventArgs args)
    {
        EventBus.Instance.EmitTrace(nameof(CardSystemEventBus), nameof(CardLockedEventHandler), args);
        this.CardLockedEventHandler?.Invoke(this, args);
    }
    
    public event EventHandler<CardEventArgs> CardUnlockedEventHandler;
    public void EmitCardUnlocked(CardEventArgs args)
    {
        EventBus.Instance.EmitTrace(nameof(CardSystemEventBus), nameof(CardUnlockedEventHandler), args);
        this.CardUnlockedEventHandler?.Invoke(this, args);
    }
    
    public event EventHandler<CardEventArgs> CardPickedEventHandler;
    public void EmitCardPicked(CardEventArgs args)
    {
        EventBus.Instance.EmitTrace(nameof(CardSystemEventBus), nameof(CardPickedEventHandler), args);
        this.CardPickedEventHandler?.Invoke(this, args);
    }
}

public class CardEventArgs
{
    public string CardGuid { get; init; }
    public IEntityComponent Player { get; init; }
    
    public CardEventArgs(string cardGuid, IEntityComponent player = null)
    {
        CardGuid = cardGuid;
        Player = player;
    }
}
