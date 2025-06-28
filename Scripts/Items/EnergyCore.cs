using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public partial class EnergyCore : Item
{
    private const int StatIncrease = 20;
    public EnergyCore() : base("3DB74D75-0CDD-4B69-B38C-337ED4CD0A42")
    {
        this.DisplayName = "EnergyCore";
        this.Description = $"Increase Energy Shield by {StatIncrease}";
        this.IconPath = "res://Sprites/Items/EnergyCore.png";
    }

    public override void ApplyItem(PlayerCharacter player)
    {
        player.PlayerStats.EnergyShield += StatIncrease;
    }
}