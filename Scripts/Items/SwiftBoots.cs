using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public partial class SwiftBoots : Item
{
    private const int StatIncrease = 25;
    public SwiftBoots() : base("E852D4A6-1630-4548-A23F-8570F21E41704")
    {
        this.DisplayName = "Swift Boots";
        this.Description = $"Increase movement speed by {StatIncrease} %";
        this.IconPath = "res://Sprites/Items/SwiftBoots.png";
    }

    public override void ApplyItem(PlayerCharacter player)
    {
        player.PlayerStats.MovementSpeed = (int)(player.PlayerStats.MovementSpeed * (1f + StatIncrease / 100f));
    } 
}