using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items.Converter;

public class LightningDarknessConverter : Item
{
    private float conversionValue = 50.0f;

    public LightningDarknessConverter() : base(ItemIds.LightningDarknessConverterGuid)
    {
        this.DisplayName = "Lightning darkness converter";
        this.Description = $"Converts 50% of lightning damage into darkness damage.";
        this.IconPath = "res://Sprites/Items/DMG_Converter_LightningDarkness.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgConvertLightningDarkness,
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
