using System;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public partial class ChestArmor : Item
{
    private const int StatIncrease = 50;
    public ChestArmor() : base("798BBD60-2903-41B1-927F-06A27007B282")
    {
        this.DisplayName = "Chest Armor";
        this.Description = $"Increases the armor by {StatIncrease}";
        this.IconPath = "res://Sprites/Items/ArmorItem.png";
    }

    public override void ApplyItem(PlayerCharacter player)
    {
        player.StatBlock.AddModifiers(new StatModifier(Guid.NewGuid().ToString("N"), StatType.Armor, StatOp.FlatAdd, StatIncrease));
    }
}