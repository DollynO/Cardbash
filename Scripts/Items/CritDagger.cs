using System;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public partial class CritDagger : Item
{
    private const int StatIncrease = 2;
    public CritDagger() : base("166D1A7F-8322-420F-A0A8-EC8DBE9FFA54")
    {
        this.DisplayName = "Crit Dagger";
        this.Description = $"Increase global crit change by {StatIncrease} %";
        this.IconPath = "res://Sprites/Items/CritDagger.png";
    }

    public override void ApplyItem(PlayerCharacter player)
    {
        player.StatBlock.AddModifiers(
            new StatModifier(
                Guid.NewGuid().ToString("N"),
                StatType.CritChance,
                StatOp.FlatAdd,
                StatIncrease));
    } 
}