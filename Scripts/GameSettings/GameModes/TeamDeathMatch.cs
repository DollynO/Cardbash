using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.GameSettings;

public partial class TeamDeathMatch : GameMode
{
    public int WinPoints { get; set; }
    public int KillPoints { get; set; }
    public int GameWinPoints { get; set; }
    private GameManager manager;
    public System.Collections.Generic.Dictionary<int, int> TeamPoints = new();
        
    public override void CheckRoundWinCondition(IContext context)
    {
        if (context is not WorldContext worldContext)
        {
            return;
        }

        if (worldContext.players.Count <= 0)
        {
            return;
        }

        var winningTeam = getLastLivingTeamId(worldContext.players);
        if (!TeamPoints.TryAdd(winningTeam, 1))
        {
            TeamPoints[winningTeam] += WinPoints;
        }
        EmitSignal(SignalName.OnRoundOver, winningTeam);

    }

    public override void CheckGameWinCondition(IContext context)
    {
        if (context is not WorldContext worldContext || worldContext.players.Count <= 0)
        {
            return;
        }
        
        var winningTeam = getLastLivingTeamId(worldContext.players);
        if (TeamPoints.TryGetValue(winningTeam, out var teamPoint)
            && teamPoint >= GameWinPoints)
        {
            EmitSignal(SignalName.OnGameOver, winningTeam);
        }
    }

    public override void AssignGameHooks(GameManager pManager)
    {
        pManager.OnPlayerKilled += onPlayerKilled;
        manager = pManager;
    }

    private void onPlayerKilled(PlayerCharacter victim, PlayerCharacter killer)
    {
        var teamId = killer.TeamId;
        if (!TeamPoints.TryAdd(teamId, 1))
        {
            TeamPoints[teamId]++;
        }

        using var context = new WorldContext();
        CheckRoundWinCondition(manager.GetWorldContext());
    }

    private int getLastLivingTeamId(IList<PlayerCharacter> players)
    {
        var teamsInGame = players.Where(p => p.PlayerStats.CurrentLife > 0)
            .DistinctBy(p => p.TeamId);
        var playerCharacters = teamsInGame.ToList();
        if (playerCharacters.Count != 1)
        {
            return -1;
        }
        
        return playerCharacters.First().TeamId;
    }
}