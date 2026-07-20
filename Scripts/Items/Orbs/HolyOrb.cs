using System;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class HolyOrb : Item
{
    private float StatIncrease = 0.2f;
    public HolyOrb() : base(ItemIds.HolyOrbGuid)
    {
        this.DisplayName = "Holy Orb";
        this.Description = $"Increases the holy damage by {Math.Round(StatIncrease * 100, 0)} %";
        this.IconPath = "res://Sprites/Items/holy_orb.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgHolyBonus,
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
