using Godot;

[GlobalClass]
public partial class GameModeSettings : Resource
{
    [Export] public int RoundsPerGame { get; set; } = 5;
    [Export] public int CardsDrawnAtRoundBegin { get; set; } = 2;
    [Export] public float RoundTimeLimitSeconds { get; set; } = 180f;
    [Export] public int RoundPointLimit { get; set; } = 10;
}