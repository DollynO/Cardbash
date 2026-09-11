using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items.Converter;

public class FrostLightningConverter : Item
{
    private float conversionValue = 50.0f;

    public FrostLightningConverter() : base(ItemIds.FrostLightningConverterGuid)
    {
        this.DisplayName = "Frost lightning converter";
        this.Description = $"Converts 50% of frost damage into lightning damage.";
        this.IconPath = "res://Sprites/Items/DMG_Converter_FrostLightning.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgConvertFrostLightning,
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
