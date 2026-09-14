using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public partial class LifeArmour : Item
{
    private const int StatIncrease = 25;
    private StatModifier lifeModifier;

    public LifeArmour() : base(ItemIds.LifeArmourGuid)
    {
        this.DisplayName = "Life Armour";
        this.Description = $"Increases life by {StatIncrease}";
        this.IconPath = "res://Sprites/Items/life_armour.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        var statIncrease = ConfigParam("statIncrease", StatIncrease);
        lifeModifier ??= new StatModifier(InstanceGuid, StatType.Life, StatOp.FlatAdd, statIncrease);
        lifeModifier.Value = statIncrease;

        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(lifeModifier);
        }

        if (targetEntity.TryGetComponent<HealthComponent>(out var healthComponent))
        {
            healthComponent.ApplyMod(lifeModifier);
        }
    }

    public override void RemoveItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
        }

        if (lifeModifier != null && targetEntity.TryGetComponent<HealthComponent>(out var healthComponent))
        {
            healthComponent.RemoveMod(lifeModifier.Id);
        }
    }
}
