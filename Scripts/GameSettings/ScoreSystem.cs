using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.GameSettings;

public partial class ScoreSystem : Node
{
    public readonly Dictionary<PlayerCharacter, int> PlayerScores = new();

    public void AddToPlayerScore(PlayerCharacter player, int score)
    {
        if (!PlayerScores.TryAdd(player, score))
        {
            PlayerScores[player] += score;
        }
    }

    public int GetScore(PlayerCharacter player)
    {
        return PlayerScores.GetValueOrDefault(player, 0);
    }
    
    public bool TryRemoveFromPlayerScore(PlayerCharacter player, int score)
    {
        if (PlayerScores.ContainsKey(player) && PlayerScores[player] > score)
        {
            PlayerScores[player] -= score;
            return true;
        }

        return false;
    }
}