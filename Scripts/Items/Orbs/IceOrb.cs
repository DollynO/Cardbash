using System;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class IceOrb : Item
{
    private float StatIncrease = 0.2f;
    private DamageModifier _damageModifier;
    public IceOrb() : base(ItemIds.IceOrbGuid)
    {
        this.DisplayName = "Ice Orb";
        this.Description = $"Increases the ice damage by {Math.Round(StatIncrease * 100, 0)} %";
        this.IconPath = "res://Sprites/Items/ice_orb.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            const DamageType damageType = DamageType.Ice;
            _damageModifier ??= new DamageModifier()
            {
                TargetDamageType = damageType,
                OutputDamageType = damageType,
                Type = DamageModifierType.Modifier,
                Value = StatIncrease,
            };

            statblock.DamageModifier.Add(_damageModifier);
        }
    }

    public override void RemoveItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.DamageModifier.Remove(_damageModifier);
        }
    }
}