using System;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public partial class ChestArmor : Item
{
    private const int StatIncrease = 50;
    public ChestArmor() : base("798BBD60-2903-41B1-927F-06A27007B282")
    {
        this.DisplayName = "Chest Armor";
        this.Description = $"Increases the armor by {StatIncrease}";
        this.IconPath = "res://Sprites/Items/ArmorItem.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.AddModifiers(new StatModifier(InstanceGuid, StatType.Armor, StatOp.FlatAdd, StatIncrease));
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