using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

/**
 * @brief Central point to calculate the total damage.
 */
public static class DamageCalculator
{
    private const float PercentMax = 100f;

    /**
     * @brief Calculates the total damage based on the origianl damage and a list of modifiers.
     * @param[in]   orgDamage   The base damage of the caller
     * @param[in]   modifiers   Modifiers to change the type or value of the damage.
     * conversion -> extra damage -> modifier
     */
    public static void CalculateTotalDamage(Dictionary<DamageType, Damage> orgDamages, List<DamageModifier> modifiers,
        StatblockComponent targetStatblock, StatblockComponent sourceStatblock)
    {
        var calculatedDamages = CalculateOutgoingDamage(orgDamages, modifiers);
        
        // apply mitigation
        if (targetStatblock != null)
        {
            foreach (var dmg in calculatedDamages)
            {
                var dr = 0f;
                var defenseStat = dmg.Key switch
                {
                    DamageType.Physical or DamageType.Poison => targetStatblock.GetStat(StatType.Armor),
                    DamageType.Darkness => 0,
                    DamageType.Holy => 0,
                    DamageType.Fire => targetStatblock.GetStat(StatType.EnergyShield),
                    DamageType.Ice => targetStatblock.GetStat(StatType.EnergyShield),
                    DamageType.Lightning => targetStatblock.GetStat(StatType.EnergyShield),
                    _ => 0,
                };
                dr = defenseStat / (defenseStat + 5 * dmg.Value.DamageNumber);
                calculatedDamages[dmg.Key].DamageNumber = dmg.Value.DamageNumber * (1 - dr);
            }
        }

        if (sourceStatblock != null)
        {
            var critChance = sourceStatblock.GetStat(StatType.CritChance);
            if (critChance > 0)
            {
                var rnd = new Random();
                var chance = rnd.NextDouble();
                if (chance <= (critChance/100))
                {
                    foreach (var dmg in calculatedDamages)
                    {
                        dmg.Value.DamageNumber *= (sourceStatblock.GetStat(StatType.CritBonus) + 2);
                    }
                }
            }
        }

        if (calculatedDamages.Count <= 0) return;

        orgDamages.Clear();
        foreach (var calculatedDamage in calculatedDamages)
        {
            orgDamages.Add(calculatedDamage.Key, calculatedDamage.Value);
        }
    }

    /**
     * @brief Calculates source-side damage changes without target mitigation or random critical hits.
     * @details This is useful for gameplay telegraphs because it exposes converted damage types at cast time.
     */
    public static Dictionary<DamageType, Damage> CalculateOutgoingDamage(
        Dictionary<DamageType, Damage> orgDamages,
        IEnumerable<DamageModifier> modifiers)
    {
        var calculatedDamages = new Dictionary<DamageType, Damage>();
        if (orgDamages == null)
        {
            return calculatedDamages;
        }

        var modifierGroups = SplitModifiers(modifiers);
        foreach (var orgDamageDict in orgDamages)
        {
            var baseDamage = CloneDamage(orgDamageDict.Value);
            var totalDamageList = ApplyConversions(baseDamage, modifierGroups.Conversions);

            ApplyExtraDamage(totalDamageList, baseDamage, modifierGroups.ExtraDamages);
            ApplyDamageModifiers(totalDamageList, baseDamage.Type, modifierGroups.Modifiers);

            foreach (var dmg in totalDamageList)
            {
                AddDamage(calculatedDamages, dmg.Value);
            }
        }

        return calculatedDamages;
    }

    /**
     * @brief Returns the outgoing damage type mix as percentages.
     */
    public static Dictionary<DamageType, float> PreviewDamageTypeMix(
        DamageType baseType,
        IEnumerable<DamageModifier> modifiers)
    {
        return PreviewDamageTypeMix(new Dictionary<DamageType, Damage>
        {
            {
                baseType,
                new Damage
                {
                    Type = baseType,
                    DamageNumber = PercentMax,
                    AilmentChance = 0f,
                }
            },
        }, modifiers);
    }

    /**
     * @brief Returns the outgoing damage type mix as percentages.
     */
    public static Dictionary<DamageType, float> PreviewDamageTypeMix(
        Dictionary<DamageType, Damage> orgDamages,
        IEnumerable<DamageModifier> modifiers)
    {
        var outgoingDamages = CalculateOutgoingDamage(orgDamages, modifiers);
        var totalDamage = outgoingDamages.Values.Sum(damage => Mathf.Max(0f, damage.DamageNumber));
        var result = new Dictionary<DamageType, float>();

        if (totalDamage <= 0f)
        {
            return result;
        }

        foreach (var damage in outgoingDamages.Values.Where(damage => damage.DamageNumber > 0f))
        {
            result[damage.Type] = damage.DamageNumber / totalDamage * PercentMax;
        }

        return result;
    }

    private static DamageModifierGroups SplitModifiers(IEnumerable<DamageModifier> modifiers)
    {
        var groups = new DamageModifierGroups();
        if (modifiers == null)
        {
            return groups;
        }

        foreach (var mod in modifiers)
        {
            switch (mod.Type)
            {
                case DamageModifierType.ExtraDamage:
                    groups.ExtraDamages.Add(mod);
                    break;
                case DamageModifierType.Conversion:
                    groups.Conversions.Add(mod);
                    break;
                case DamageModifierType.Modifier:
                    groups.Modifiers.Add(mod);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return groups;
    }

    private static Dictionary<DamageType, Damage> ApplyConversions(
        Damage baseDamage,
        IEnumerable<DamageModifier> conversionList)
    {
        var totalDamageList = new Dictionary<DamageType, Damage>
        {
            { baseDamage.Type, CloneDamage(baseDamage) },
        };

        var conversionByOutputType = new Dictionary<DamageType, float>();
        foreach (var mod in conversionList.Where(mod => mod.TargetDamageType == baseDamage.Type))
        {
            if (mod.OutputDamageType == baseDamage.Type || mod.Value <= 0f)
            {
                continue;
            }

            if (!conversionByOutputType.TryAdd(mod.OutputDamageType, mod.Value))
            {
                conversionByOutputType[mod.OutputDamageType] += mod.Value;
            }
        }

        var totalConversion = 0f;
        foreach (var conv in conversionByOutputType)
        {
            if (totalConversion >= PercentMax)
            {
                break;
            }

            var conversionPercent = Mathf.Min(conv.Value, PercentMax - totalConversion);
            totalConversion += conversionPercent;
            var convertedDamageNumber = baseDamage.DamageNumber * conversionPercent / PercentMax;

            totalDamageList[baseDamage.Type].DamageNumber -= convertedDamageNumber;
            AddDamage(totalDamageList, new Damage
            {
                DamageNumber = convertedDamageNumber,
                AilmentChance = baseDamage.AilmentChance,
                Type = conv.Key,
            });
        }

        return totalDamageList;
    }

    private static void ApplyExtraDamage(
        Dictionary<DamageType, Damage> totalDamageList,
        Damage baseDamage,
        IEnumerable<DamageModifier> extraDamageList)
    {
        foreach (var mod in extraDamageList.Where(mod => mod.TargetDamageType == baseDamage.Type))
        {
            AddDamage(totalDamageList, new Damage
            {
                DamageNumber = baseDamage.DamageNumber * mod.Value / PercentMax,
                AilmentChance = baseDamage.AilmentChance,
                Type = mod.OutputDamageType,
            });
        }
    }

    private static void ApplyDamageModifiers(
        Dictionary<DamageType, Damage> totalDamageList,
        DamageType baseDamageType,
        IEnumerable<DamageModifier> modifierList)
    {
        var modifierByOutputType = new Dictionary<DamageType, float>();
        foreach (var mod in modifierList.Where(mod => mod.TargetDamageType == baseDamageType))
        {
            if (!modifierByOutputType.TryAdd(mod.OutputDamageType, mod.Value))
            {
                modifierByOutputType[mod.OutputDamageType] += mod.Value;
            }
        }

        foreach (var mod in modifierByOutputType)
        {
            if (totalDamageList.TryGetValue(mod.Key, out var damage))
            {
                damage.DamageNumber *= 1 + mod.Value;
            }
        }
    }

    private static Damage CloneDamage(Damage damage)
    {
        return new Damage
        {
            Type = damage.Type,
            DamageNumber = damage.DamageNumber,
            AilmentChance = damage.AilmentChance,
        };
    }

    private static void AddDamage(Dictionary<DamageType, Damage> damages, Damage damage)
    {
        if (!damages.TryAdd(damage.Type, damage))
        {
            damages[damage.Type].DamageNumber += damage.DamageNumber;
            damages[damage.Type].AilmentChance = Math.Max(damages[damage.Type].AilmentChance, damage.AilmentChance);
        }
    }

    private sealed class DamageModifierGroups
    {
        public List<DamageModifier> Conversions { get; } = new();
        public List<DamageModifier> ExtraDamages { get; } = new();
        public List<DamageModifier> Modifiers { get; } = new();
    }
}
