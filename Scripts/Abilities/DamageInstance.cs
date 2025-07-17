namespace CardBase.Scripts.Abilities;

/**
 * @brief The different damage types.
 */
public enum DamageType
{
    Physical,
    Poison,
    Fire,
    Ice,
    Lightning,
    Darkness,
    Holy,
}

/**
 * @brief Dataclass for damage.
 */
public class Damage
{
    public DamageType Type;
    public float DamageNumber;
}
