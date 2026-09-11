using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items;

public partial class ShamanPuppet : Item
{
    private const float AilmentChanceIncrease = 0.05f;

    public ShamanPuppet() : base(ItemIds.ShamanPuppetGuid)
    {
        this.DisplayName = "Shaman Puppet";
        this.Description = "Increases global ailment chance by 5 %";
        this.IconPath = "res://Sprites/Items/shaman_puppet.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.AddModifiers(new StatModifier(InstanceGuid, StatType.GlobalAilmentChance, StatOp.FlatAdd,
                ConfigParam("ailmentChance", AilmentChanceIncrease)));
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
