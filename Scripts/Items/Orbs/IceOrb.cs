using System;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class IceOrb : Item
{
    private float StatIncrease = 0.2f;
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
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgIceBonus,
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
