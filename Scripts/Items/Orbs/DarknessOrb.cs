using System;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class DarknessOrb: Item
{
    private float StatIncrease = 0.2f;
    private DamageModifier _damageModifier;
    public DarknessOrb() : base(ItemIds.DarknessOrbGuid)
    {
        this.DisplayName = "Darkness Orb";
        this.Description = $"Increases the darkness damage by {Math.Round(StatIncrease * 100,0)} %";
        this.IconPath = "res://Sprites/Items/darkness_orb.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            const DamageType damageType = DamageType.Darkness;
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