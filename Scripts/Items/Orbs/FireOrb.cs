using System;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public class FireOrb : Item
{
    private float StatIncrease = 0.2f;
    public FireOrb() : base(ItemIds.FireOrbGuid)
    {
        this.DisplayName = "Fire Orb";
        this.Description = $"Increases the fire damage by {Math.Round(StatIncrease * 100, 0)} %";
        this.IconPath = "res://Sprites/Items/fire_orb.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgFireBonus,
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
