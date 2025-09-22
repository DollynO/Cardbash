using System;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class IceOrb : Item
{
    private float StatIncrease = 0.2f;
    public IceOrb() : base(ItemIds.IceOrbGuid)
    {
        this.DisplayName = "Ice Orb";
        this.Description = $"Increases the ice damage by {Math.Round(StatIncrease * 100,0)} %";
        this.IconPath = "res://Sprites/Items/ice_orb.png";
    }

    public override void ApplyItem(PlayerCharacter player)
    {
        player.DamageModifier.Add(new DamageModifier()
        {
            TargetDamageType = DamageType.Ice,
            OutputDamageType = DamageType.Ice,
            Type = DamageModifierType.Modifier,
            Value = StatIncrease,
        });
    }
}