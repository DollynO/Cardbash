using Godot;

public sealed class LastTeamStandingMode : IGameMode
{
    public ModeId Id => ModeId.LastTeamStanding;
    public GameModeSettings Settings { get; }
    private GameContext _ctx;
    
    
    private System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<Team>> roundWins = new ();

    public LastTeamStandingMode(GameModeSettings settings) => Settings = settings;

    public void ServerInitialize(GameContext ctx) => _ctx = ctx;
    public void ServerStartGame() { }
    public void ServerStartRound(int roundIndex)
    {
        // reset alive, respawns, etc.
        foreach (var p in _ctx.Players.Values)
        {
            p.RoundReset();
            p.GlobalPosition = _ctx.GameManager.GetNextFreeSpawnPoint();
        }
    }

    public void ServerTick(double delta) { }

    public bool ServerIsRoundOver(out RoundResult result)
    {
        var aliveTeams = _ctx.TeamSystem.GetTeamsWithAlivePlayers();

        if (aliveTeams.Count <= 1)
        {
            result = RoundResult.TeamWin(aliveTeams.Count == 1 ? aliveTeams[0] : null);
            return true;
        }

        result = default;
        return false;
    }

    public bool ServerIsGameOver(out GameResult result)
    {
        /*if (_ctx.ScoreSystem.AnyTeamReachedGameWin(Settings.RoundsPerGame))
        {
            result = _ctx.ScoreSystem.BuildGameResult();
            return true;
        }*/
        result = default;
        return false;
    }
}