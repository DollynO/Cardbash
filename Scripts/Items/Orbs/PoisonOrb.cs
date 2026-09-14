using System;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class PoisonOrb : Item
{
    private float StatIncrease = 0.2f;
    public PoisonOrb() : base(ItemIds.PoisonOrbGuid)
    {
        this.DisplayName = "Poison Orb";
        this.Description = $"Increases the poison damage by {Math.Round(StatIncrease * 100, 0)} %";
        this.IconPath = "res://Sprites/Items/poison_orb.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgPoisonBonus,
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
