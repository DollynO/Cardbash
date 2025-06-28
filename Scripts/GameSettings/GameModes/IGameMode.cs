using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.GameSettings;

public abstract partial class GameMode : Node
{
    public Dictionary<int, int> TeamPoints { get; set; }
    public abstract void CheckRoundWinCondition(IContext context);
    public abstract void CheckGameWinCondition(IContext context);

    public abstract void AssignGameHooks(GameManager p_manager);
    
    [Signal]
    public delegate void OnRoundOverEventHandler(int winnerTeam);
    
    [Signal]
    public delegate void OnGameOverEventHandler(int winnerTeam);
}