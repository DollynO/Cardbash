using System;

namespace CardBase.Scripts;

public class MatchEventBus
{
    public event EventHandler<MatchEventArgs> RoundStartEventHandler;
    public void EmitRoundStart(MatchEventArgs args)
    {
        this.RoundStartEventHandler?.Invoke(this, args);
    }
    
    public event EventHandler<MatchEventArgs> RoundEndEventHandler;
    public void EmitRoundEnd(MatchEventArgs args)
    {
        this.RoundEndEventHandler?.Invoke(this, args);
    }
    
    public event EventHandler<MatchEventArgs> MatchStartEventHandler;
    public void EmitMatchStart(MatchEventArgs args)
    {
        this.MatchStartEventHandler?.Invoke(this, args);
    }
    
    public event EventHandler<MatchEventArgs> MatchEndEventHandler;
    public void EmitMatchEnd(MatchEventArgs args)
    {
        this.MatchEndEventHandler?.Invoke(this, args);
    }
    
    public event EventHandler<ScoreEventArgs> ScoreChangedEventHandler;

    public void EmitScoreChanged(ScoreEventArgs args)
    {
        this.ScoreChangedEventHandler?.Invoke(this, args);
    }
    
    public event EventHandler<MatchEventArgs> GamePhaseStartedEventHandler;

    public void EmitGamePhaseStarted(MatchEventArgs args)
    {
        this.GamePhaseStartedEventHandler?.Invoke(this, args);
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

public class ScoreEventArgs
{
    public int TeamId { get; init; }
    public int Score { get; init; }
    
    public ScoreEventArgs(int teamId, int score)
    {
        TeamId = teamId;
        Score = score;
    }
}
