using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items.Converter;

public class FireDarknessConverter : Item
{
    private float conversionValue = 50.0f;

    public FireDarknessConverter() : base(ItemIds.FireDarknessConverterGuid)
    {
        this.DisplayName = "Fire darkness converter";
        this.Description = $"Converts 50% of fire damage into darkness damage.";
        this.IconPath = "res://Sprites/Items/DMG_Converter_FireDarkness.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgConvertFireDarkness,
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
