using System;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class LightningOrb: Item
{
    private float StatIncrease = 0.2f;
    public LightningOrb() : base(ItemIds.LightningOrbGuid)
    {
        this.DisplayName = "Lightning Orb";
        this.Description = $"Increases the Lightning damage by {Math.Round(StatIncrease * 100,0)} %";
        this.IconPath = "res://Sprites/Items/lightning_orb.png";
    }

    public override void ApplyItem(PlayerCharacter player)
    {
        player.DamageModifier.Add(new DamageModifier()
        {
            TargetDamageType = DamageType.Lightning,
            OutputDamageType = DamageType.Lightning,
            Type = DamageModifierType.Modifier,
            Value = StatIncrease,
        });
    }
}