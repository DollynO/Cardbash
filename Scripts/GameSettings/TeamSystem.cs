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
        Godot.Collections.Dictionary<int, Godot.Collections.Array<int>> teams = new();
        foreach (var player in _players.Values)
        {
            if (!teams.TryGetValue(player.TeamId, out var array))
            {
                array =  new Godot.Collections.Array<int>();
                
            }
            array.Add(player.TeamId);
            
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

        Rpc(MethodName.updatTeamsClients, teams);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void updatTeamsClients(Godot.Collections.Dictionary<int, Godot.Collections.Array<int>> dict)
    {
        foreach (var kvp in dict)
        {
            Teams.TryAdd(kvp.Key, new Team()
            {
                TeamId = kvp.Key
            });
            var team = Teams[kvp.Key];
            
            foreach (var player in kvp.Value)
            {
                var playerCharacter = GameManager.GetPlayerCharacter(player);
                if (!team.Players.Contains(playerCharacter))
                {
                    team.Players.Add(playerCharacter);
                }
            }
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