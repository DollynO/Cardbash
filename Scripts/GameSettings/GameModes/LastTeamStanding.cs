using System.Collections.Generic;
using System.Linq;
using Godot;

public sealed class LastTeamStandingMode : IGameMode
{
    public ModeId Id => ModeId.LastTeamStanding;
    public GameModeSettings Settings { get; }
    public List<RoundResult> RoundResults { get; } = new ();
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
        var winCounts = RoundResults
            .SelectMany(r => r.WinningTeams)
            .GroupBy(team => team)
            .ToDictionary(g => g.Key, g => g.Count());

        var winningTeams = winCounts.Where(kvp => kvp.Value >= Settings.RoundsPerGame).Select(kvp => kvp.Key).ToList();
        if (winningTeams.Any())
        {
            result = new GameResult(winningTeams);
            return true;
        }
        result = default;
        return false;
    }
}