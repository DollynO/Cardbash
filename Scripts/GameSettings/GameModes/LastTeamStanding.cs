using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.GameSettings;
using CardBase.Scripts.PlayerScripts;
using Godot;

public sealed class LastTeamStandingMode : IGameMode
{
    public ModeId Id => ModeId.LastTeamStanding;
    public GameModeSettings Settings { get; }
    public List<RoundResult> RoundResults { get; } = new();
    private GameContext _ctx;


    private Dictionary<int, List<Team>> roundWins = new();

    public LastTeamStandingMode(GameModeSettings settings) => Settings = settings;

    public void ServerInitialize(GameContext ctx) => _ctx = ctx;

    public void ServerStartGame()
    {
        _ctx.GameManager.OnPlayerKilled += onPlayerKilled;
    }

    private void onPlayerKilled(PlayerCharacter victimid, PlayerCharacter killerid)
    {
        _ctx.ScoreSystem.AddToTeamScore(killerid, Settings.PointsOnKill);
    }

    public void ServerStartRound(int roundIndex)
    {
        // reset alive, respawns, etc.
        foreach (var p in _ctx.Players.Values)
        {
            p.RoundReset(roundIndex);
            p.GlobalPosition = _ctx.GameManager.GetNextFreeSpawnPoint();
        }
    }

    public void ServerStartCombat()
    {
        foreach (var p in _ctx.Players.Values)
        {
            p.RoundStart();
        }
    }

    public void ServerTick(double delta) { }

    public bool ServerIsRoundOver(out RoundResult result)
    {
        var aliveTeams = _ctx.TeamSystem.GetTeamsWithAlivePlayers();

        if (aliveTeams.Count <= 1)
        {
            foreach (var team in aliveTeams)
            {
                _ctx.ScoreSystem.AddToTeamScore(team, Settings.PointsOnRoundEnd);
            }
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

        var dict = new Dictionary<Team, int>();
        foreach (var teams in _ctx.TeamSystem.Teams)
        {
            var teamScore = _ctx.ScoreSystem.GetTeamScore(teams.Key);
            dict.Add(teams.Value, teamScore);
        }

        var winningTeams = dict.Where(kvp => kvp.Value >= Settings.PointsToWin).Select(kvp => kvp.Key).ToList();
        if (winningTeams.Any())
        {
            result = new GameResult(winningTeams);
            return true;
        }
        result = default;
        return false;
    }
}
