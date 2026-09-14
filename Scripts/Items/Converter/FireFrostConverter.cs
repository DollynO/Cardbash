using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items.Converter;

public class FireFrostConverter : Item
{
    private float conversionValue = 50.0f;
    public FireFrostConverter() : base(ItemIds.FireFrostConverterGuid)
    {
        this.DisplayName = "Fire frost converter";
        this.Description = $"Converts 50% of fire damage into frost damage.";
        this.IconPath = "res://Sprites/Items/DMG_Converter_FireFrost.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgConvertFireFrost,
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