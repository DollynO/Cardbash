using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items.Converter;

public class FrostDarknessConverter : Item
{
    private float conversionValue = 50.0f;

    public FrostDarknessConverter() : base(ItemIds.FrostDarknessConverterGuid)
    {
        this.DisplayName = "Frost darkness converter";
        this.Description = $"Converts 50% of frost damage into darkness damage.";
        this.IconPath = "res://Sprites/Items/DMG_Converter_FrostDarkness.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgConvertFrostDarkness,
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
