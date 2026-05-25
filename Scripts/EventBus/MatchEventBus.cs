using System;

namespace CardBase.Scripts;

public class MatchEventBus
{
    public EventHandler<MatchEventArgs> RoundStartEventHandler;
    public void EmitRoundStart(MatchEventArgs args)
    {
        this.RoundStartEventHandler?.Invoke(this, args);
    }
    
    public EventHandler<MatchEventArgs> RoundEndEventHandler;
    public void EmitRoundEnd(MatchEventArgs args)
    {
        this.RoundEndEventHandler?.Invoke(this, args);
    }
    
    public EventHandler<MatchEventArgs> MatchStartEventHandler;
    public void EmitMatchStart(MatchEventArgs args)
    {
        this.MatchStartEventHandler?.Invoke(this, args);
    }
    
    public EventHandler<MatchEventArgs> MatchEndEventHandler;
    public void EmitMatchEnd(MatchEventArgs args)
    {
        this.MatchEndEventHandler?.Invoke(this, args);
    }
}

public class MatchEventArgs
{
    public int RoundNumber { get; init; }
    
    public MatchEventArgs(int roundNumber)
    {
        RoundNumber = roundNumber;
    }
}