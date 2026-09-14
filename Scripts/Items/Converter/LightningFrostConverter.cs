using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items.Converter;

public class LightningFrostConverter : Item
{
    private float conversionValue = 50.0f;

    public LightningFrostConverter() : base(ItemIds.LightningFrostConverterGuid)
    {
        this.DisplayName = "Lightning frost converter";
        this.Description = $"Converts 50% of lightning damage into frost damage.";
        this.IconPath = "res://Sprites/Items/DMG_Converter_LightningFrost.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgConvertLightningFrost,
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
