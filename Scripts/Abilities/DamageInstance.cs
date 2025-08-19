using Godot;
using Godot.Collections;

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
public class Damage : IDictAble<Damage>
{
    public DamageType Type;
    public float AilmentChange;
    public float DamageNumber;

    public const float DEFAULT_AILMENT_CHANGE = 0.1f;
    public const string SOURCE_MODIFIER_ID = "B954BA32-B61C-4EC9-A843-60868EC31733";

    public Dictionary<string, Variant> ToDict()
    {
        return new Dictionary<string, Variant>
        {
            { "type", (int)Type },
            { "ailment_change", AilmentChange },
            { "damage_number", DamageNumber }
        };
    }

    public static Damage FromDict(Dictionary<string, Variant> dict)
    {
        return new Damage()
        {
            Type = (DamageType)(int)dict["type"],
            AilmentChange = (float)dict["ailment_change"],
            DamageNumber = (float)dict["damage_number"]
        };
    }
}
