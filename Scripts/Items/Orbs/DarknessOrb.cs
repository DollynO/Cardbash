using System;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class DarknessOrb : Item
{
    private float StatIncrease = 0.2f;
    public DarknessOrb() : base(ItemIds.DarknessOrbGuid)
    {
        this.DisplayName = "Darkness Orb";
        this.Description = $"Increases the darkness damage by {Math.Round(StatIncrease * 100, 0)} %";
        this.IconPath = "res://Sprites/Items/darkness_orb.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgDarknessBonus,
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
