using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items.Converter;

public class LightningFireConverter : Item
{
    private float conversionValue = 50.0f;

    public LightningFireConverter() : base(ItemIds.LightningFireConverterGuid)
    {
        this.DisplayName = "Lightning fire converter";
        this.Description = $"Converts 50% of lightning damage into fire damage.";
        this.IconPath = "res://Sprites/Items/DMG_Converter_LightningFire.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgConvertLightningFire,
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
