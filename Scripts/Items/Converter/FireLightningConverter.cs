using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items.Converter;

public class FireLightningConverter : Item
{
    private float conversionValue = 50.0f;

    public FireLightningConverter() : base(ItemIds.FireLightningConverterGuid)
    {
        this.DisplayName = "Fire lightning converter";
        this.Description = $"Converts 50% of fire damage into lightning damage.";
        this.IconPath = "res://Sprites/Items/DMG_Converter_FireLightning.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgConvertFireLightning,
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
