using System;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class FireOrb : Item
{
    private float StatIncrease = 0.2f;
    public FireOrb() : base(ItemIds.FireOrbGuid)
    {
        this.DisplayName = "Fire Orb";
        this.Description = $"Increases the fire damage by {Math.Round(StatIncrease * 100,0)} %";
        this.IconPath = "res://Sprites/Items/fire_orb.png";
    }

    public override void ApplyItem(PlayerCharacter player)
    {
        player.DamageModifier.Add(new DamageModifier()
        {
            TargetDamageType = DamageType.Fire,
            OutputDamageType = DamageType.Fire,
            Type = DamageModifierType.Modifier,
            Value = StatIncrease
        });
    }
}