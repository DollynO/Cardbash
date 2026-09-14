using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items.Converter;

public class FrostFireConverter : Item
{
    private float conversionValue = 50.0f;

    public FrostFireConverter() : base(ItemIds.FrostFireConverterGuid)
    {
        this.DisplayName = "Frost fire converter";
        this.Description = $"Converts 50% of frost damage into fire damage.";
        this.IconPath = "res://Sprites/Items/DMG_Converter_FrostFire.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgConvertFrostFire,
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
