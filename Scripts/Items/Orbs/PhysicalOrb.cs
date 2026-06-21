using System;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class PhysicalOrb : Item
{
    private float StatIncrease = 0.2f;
    private DamageModifier _damageModifier;
    public PhysicalOrb() : base(ItemIds.PhysicalOrbGuid)
    {
        this.DisplayName = "Physical Orb";
        this.Description = $"Increases the physical damage by {Math.Round(StatIncrease * 100, 0)} %";
        this.IconPath = "res://Sprites/Items/physical_orb.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            const DamageType damageType = DamageType.Physical;
            _damageModifier ??= new DamageModifier()
            {
                TargetDamageType = damageType,
                OutputDamageType = damageType,
                Type = DamageModifierType.Modifier,
                Value = ConfigParam("damageModifier", StatIncrease),
            };
            _damageModifier.Value = ConfigParam("damageModifier", StatIncrease);

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
