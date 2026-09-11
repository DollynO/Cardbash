using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items.Converter;

public class FrostHolyConverter : Item
{
    private float conversionValue = 50.0f;

    public FrostHolyConverter() : base(ItemIds.FrostHolyConverterGuid)
    {
        this.DisplayName = "Frost holy converter";
        this.Description = $"Converts 50% of frost damage into holy damage.";
        this.IconPath = "res://Sprites/Items/DMG_Converter_FrostHoly.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgConvertFrostHoly,
                StatOp.FlatAdd,
                ConfigParam("conversion", conversionValue)));
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
