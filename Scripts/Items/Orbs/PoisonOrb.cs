using System;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class PoisonOrb: Item
{
    private float StatIncrease = 0.2f;
    public PoisonOrb() : base(ItemIds.PoisonOrbGuid)
    {
        this.DisplayName = "Poison Orb";
        this.Description = $"Increases the poison damage by {Math.Round(StatIncrease * 100,0)} %";
        this.IconPath = "res://Sprites/Items/poison_orb.png";
    }

    public override void ApplyItem(PlayerCharacter player)
    {
        player.DamageModifier.Add(new DamageModifier()
        {
            TargetDamageType = DamageType.Poison,
            OutputDamageType = DamageType.Poison,
            Type = DamageModifierType.Modifier,
            Value = StatIncrease,
        });
    }
}