using System;

namespace CardBase.Scripts;

public class CardSystemEventBus
{
    public EventHandler<CardEventArgs> CardLockedEventHandler;
    public void EmitCardLocked(CardEventArgs args)
    {
        this.CardLockedEventHandler?.Invoke(this, args);
    }
    
    public EventHandler<CardEventArgs> CardUnlockedEventHandler;
    public void EmitCardUnlocked(CardEventArgs args)
    {
        this.CardUnlockedEventHandler?.Invoke(this, args);
    }
    
    public EventHandler<CardEventArgs> CardPickedEventHandler;
    public void EmitCardPicked(CardEventArgs args)
    {
        this.CardPickedEventHandler?.Invoke(this, args);
    }
}

public class CardEventArgs
{
    public string CardGuid { get; init; }
    
    public CardEventArgs(string cardGuid)
    {
        CardGuid = cardGuid;
    }
}