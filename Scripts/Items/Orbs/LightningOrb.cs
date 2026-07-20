using System;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class LightningOrb : Item
{
    private float StatIncrease = 0.2f;
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
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgLightningBonus,
                StatOp.FlatAdd,
                ConfigParam("damageModifier", StatIncrease)));
        }
    }

    public override void RemoveItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
        }
    }
}
