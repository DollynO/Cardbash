namespace CardBase.Scripts.Abilities;

/**
 * @brief Different types of damage modifiers.
 * @details Calculation order. Conversion -> Extra Damage -> Modifier.
 */
public enum DamageModifierType{
    ExtraDamage,        //!< Gain extra damage based from the base damage. The base damage is not affected with this modifier
    Conversion,         //!< Converts a part of the base damage in the new type.
    Modifier            //!< Modifies the Target type.
}

/**
 * @brief Modifier that are applied for the damage calculation.
 */
public record DamageModifier
{
    public DamageType TargetDamageType;
    public DamageType OutputDamageType;
    public DamageModifierType Type;
    public float Value;
}