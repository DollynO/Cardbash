using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Items.Converter;

public class FireHolyConverter : Item
{
    private float conversionValue = 50.0f;

    public FireHolyConverter() : base(ItemIds.FireHolyConverterGuid)
    {
        this.DisplayName = "Fire holy converter";
        this.Description = $"Converts 50% of fire damage into holy damage.";
        this.IconPath = "res://Sprites/Items/DMG_Converter_FireHoly.png";
    }

    public override void ApplyItem(IEntityComponent targetEntity)
    {
        if (targetEntity.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(InstanceGuid);
            statblock.AddModifiers(new StatModifier(
                InstanceGuid,
                StatType.DmgConvertFireHoly,
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
