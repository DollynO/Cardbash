using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.GameSettings;

public partial class TeamSystem : Node
{
    public GameManager GameManager { get; }
    private readonly Dictionary<long, PlayerCharacter> _players;
    public Dictionary<int, Team> Teams { get; } = new();
    public TeamSystem(GameManager gm, Dictionary<long, PlayerCharacter> players)
    {
        GameManager = gm;
        _players = players;
    }

    public List<Team> GetTeamsWithAlivePlayers()
    {
        return Teams.Values.Where(t => t.HasAlivePlayers()).ToList();
    }

    public void UpdateTeams()
    {
        foreach (var player in _players.Values)
        {
            if (!Teams.TryGetValue(player.TeamId, out var team))
            {
                team = new Team
                {
                    TeamId = player.TeamId
                };
                Teams.Add(team.TeamId, team);
            }

            team.AddPlayer(player);
        }
    }
}

public class Team
{
    public List<PlayerCharacter> Players { get; } = new List<PlayerCharacter>();
    public int TeamId;

    public bool AddPlayer(PlayerCharacter player)
    {
        if (Players.Contains(player)) return false;
        Players.Add(player);
        return true;
    }

    public bool HasAlivePlayers()
    {
        return Players.Count > 0 && Players.Any(p => !p.HealthComponent.IsDead);
    }
}