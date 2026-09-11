using Godot;

[GlobalClass]
public partial class GameModeSettings : Resource
{
    /*[Export] public int RoundsPerGame { get; set; } = 5;
    [Export] public float RoundTimeLimitSeconds { get; set; } = 180f;
    [Export] public int RoundPointLimit { get; set; } = 10;*/

    public int PointsToWin { get; set; } = 0;
    public int PointsOnKill { get; set; } = 0;
    public int PointsOnRoundEnd { get; set; } = 0;
    [Export] public int CardsPerRound { get; set; } = 2;
    [Export] public bool FriendlyFire { get; set; }
    
    public int CardLockCosts { get; set; } = 10;
    public int CardRerollCosts { get; set; } = 10;

    public void Copy(GameModeSettings gameSettings)
    {
        PointsToWin = gameSettings.PointsToWin;
        PointsOnKill = gameSettings.PointsOnKill;
        PointsOnRoundEnd = gameSettings.PointsOnRoundEnd;
        CardsPerRound = gameSettings.CardsPerRound;
        FriendlyFire = gameSettings.FriendlyFire;
        CardLockCosts = gameSettings.CardLockCosts;
        CardRerollCosts = gameSettings.CardRerollCosts;
    }
}
