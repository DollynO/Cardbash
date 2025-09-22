using System;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class PhysicalOrb : Item
{
    private float StatIncrease = 0.2f;
    public PhysicalOrb() : base(ItemIds.PhysicalOrbGuid)
    {
        this.DisplayName = "Physical Orb";
        this.Description = $"Increases the physical damage by {Math.Round(StatIncrease * 100,0)} %";
        this.IconPath = "res://Sprites/Items/physical_orb.png";
    }

    public override void ApplyItem(PlayerCharacter player)
    {
        player.DamageModifier.Add(new DamageModifier()
        {
            TargetDamageType = DamageType.Physical,
            OutputDamageType = DamageType.Physical,
            Type = DamageModifierType.Modifier,
            Value = StatIncrease,
        });
    }
}