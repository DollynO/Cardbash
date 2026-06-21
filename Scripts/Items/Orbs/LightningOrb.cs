using System;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class LightningOrb : Item
{
    private float StatIncrease = 0.2f;
    private DamageModifier _damageModifier;
    public LightningOrb() : base(ItemIds.LightningOrbGuid)
    {
        this.DisplayName = "Lightning Orb";
        this.Description = $"Increases the Lightning damage by {Math.Round(StatIncrease * 100, 0)} %";
        this.IconPath = "res://Sprites/Items/lightning_orb.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            const DamageType damageType = DamageType.Lightning;
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
