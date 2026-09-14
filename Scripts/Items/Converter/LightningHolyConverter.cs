using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items.Converter;

public class LightningHolyConverter : Item
{
    private float conversionValue = 50.0f;

    public LightningHolyConverter() : base(ItemIds.LightningHolyConverterGuid)
    {
        this.DisplayName = "Lightning holy converter";
        this.Description = $"Converts 50% of lightning damage into holy damage.";
        this.IconPath = "res://Sprites/Items/DMG_Converter_LightningHoly.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgConvertLightningHoly,
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
