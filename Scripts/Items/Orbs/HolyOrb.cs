using System;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class HolyOrb: Item
{
    private float StatIncrease = 0.2f;
    public HolyOrb() : base(ItemIds.HolyOrbGuid)
    {
        this.DisplayName = "Holy Orb";
        this.Description = $"Increases the holy damage by {Math.Round(StatIncrease * 100,0)} %";
        this.IconPath = "res://Sprites/Items/holy_orb.png";
    }

    public override void ApplyItem(PlayerCharacter player)
    {
        player.DamageModifier.Add(new DamageModifier()
        {
            TargetDamageType = DamageType.Holy,
            OutputDamageType = DamageType.Holy,
            Type = DamageModifierType.Modifier,
            Value = StatIncrease,
        });
    }
}