using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.GameSettings;

public partial class ScoreSystem : Node
{
    public readonly Dictionary<int, int> TeamScores = new();
    public GameManager gameManager;
    
    public ScoreSystem(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void AddToTeamScore(PlayerCharacter player, int score)
    {
        if (player == null) return;
        
        AddToTeamScore(player.TeamId, score);
    }

    public void AddToTeamScore(Team team, int score)
    {
        if (team == null) return;
        
        AddToTeamScore(team.TeamId, score);
    }

    public void AddToTeamScore(int teamId, int score)
    {
        if (!Multiplayer.IsServer()) return;
        
        ServerSetTeamScore(teamId, GetTeamScore(teamId) + score);
    }

    public int GetTeamScore(PlayerCharacter player)
    {
        return player == null ? 0 : GetTeamScore(player.TeamId);
    }

    public int GetTeamScore(Team team)
    {
        return team == null ? 0 : GetTeamScore(team.TeamId);
    }

    public int GetTeamScore(int teamId)
    {
        return TeamScores.GetValueOrDefault(teamId, 0);
    }
    
    public bool TryRemoveFromTeamScore(PlayerCharacter player, int score)
    {
        return player != null && TryRemoveFromTeamScore(player.TeamId, score);
    }

    public bool TryRemoveFromTeamScore(int teamId, int score)
    {
        if (!Multiplayer.IsServer()) return false;
        
        var currentScore = GetTeamScore(teamId);
        if (currentScore < score)
        {
            return false;
        }

        ServerSetTeamScore(teamId, currentScore - score);
        return true;
    }

    private void ServerSetTeamScore(int teamId, int score)
    {
        Rpc(MethodName.SyncTeamScore, teamId, score);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncTeamScore(int teamId, int score)
    {
        SetTeamScore(teamId, score);
    }

    private void SetTeamScore(int teamId, int score)
    {
        if (TeamScores.TryGetValue(teamId, out var currentScore) && currentScore == score)
        {
            return;
        }

        TeamScores[teamId] = score;
        gameManager.EventBus.MatchEventBus.EmitScoreChanged(new ScoreEventArgs(teamId, score));
    }
}
