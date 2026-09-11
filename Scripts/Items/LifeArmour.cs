using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public partial class LifeArmour : Item
{
    private const int StatIncrease = 25;

    public LifeArmour() : base(ItemIds.LifeArmourGuid)
    {
        this.DisplayName = "Life Armour";
        this.Description = $"Increases life by {StatIncrease}";
        this.IconPath = "res://Sprites/Items/life_armour.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.AddModifiers(new StatModifier(InstanceGuid, StatType.Life, StatOp.FlatAdd,
                ConfigParam("statIncrease", StatIncrease)));
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
