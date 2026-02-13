using System.Collections.Generic;

public enum ModeId { LastTeamStanding, CaptureTheFlag, Herrschaft }
public enum MatchPhase { None, RoundSetup, CardDraw, CardApply, Combat, RoundEnd, GameEnd }

public interface IGameMode
{
    ModeId Id { get; }
    GameModeSettings Settings { get; }

    void ServerInitialize(GameContext ctx);
    void ServerStartGame();
    void ServerStartRound(int roundIndex);

    // Called by flow controller during Combat
    void ServerTick(double delta);

    // Mode decides when round ends + who gets points
    bool ServerIsRoundOver(out RoundResult result);

    // Mode decides when game ends
    bool ServerIsGameOver(out GameResult result);
}

public sealed class GameResult
{
    
}

public sealed class RoundResult
{
    public List<Team> WinningTeams { get; }
    private readonly List<Team> winnerTeams;

    public RoundResult()
    {
        winnerTeams = new List<Team>();
        WinningTeams = winnerTeams;
    }
    
    public static RoundResult TeamWin(Team winnerTeam)
    {
        var res = new RoundResult();
        if (winnerTeam != null)
        {
            res.WinningTeams.Add(winnerTeam);
        }

        return res;
    }

    public static RoundResult DrawByTimeout(List<Team> winnerTeams)
    {
        var res = new RoundResult();
        res.WinningTeams.AddRange(winnerTeams);
        return res;
    }
}