using System;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class DarknessOrb: Item
{
    private float StatIncrease = 0.2f;
    public DarknessOrb() : base(ItemIds.DarknessOrbGuid)
    {
        this.DisplayName = "Darkness Orb";
        this.Description = $"Increases the darkness damage by {Math.Round(StatIncrease * 100,0)} %";
        this.IconPath = "res://Sprites/Items/darkness_orb.png";
    }

    public override void ApplyItem(PlayerCharacter player)
    {
        player.DamageModifier.Add(new DamageModifier()
        {
            TargetDamageType = DamageType.Darkness,
            OutputDamageType = DamageType.Darkness,
            Type = DamageModifierType.Modifier,
            Value = StatIncrease,
        });
    }
}