using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CardBase.Scripts.Abilities;

/**
 * @brief Central point to calculate the total damage.
 */
public static class DamageCalculator
{
    /**
     * @brief Calculates the total damage based on the origianl damage and a list of modifiers.
     * @param[in]   orgDamage   The base damage of the caller
     * @param[in]   modifiers   Modifiers to change the type or value of the damage.
     * conversion -> extra damage -> modifier
     */
    public static void CalculateTotalDamage(Dictionary<DamageType, Damage> orgDamages, List<DamageModifier> modifiers)
    {
        var calculatedDamages = new Dictionary<DamageType, Damage>();
        foreach (var orgDamageDict in orgDamages)
        {
            var orgDamage = orgDamageDict.Value;
            var baseDamage = new Damage()
            {
 
                Type = orgDamage.Type, 
                DamageNumber = orgDamage.DamageNumber,
            };
            var conversionList = new List<DamageModifier>();
            var extraDamageList = new List<DamageModifier>();
            var modifierList = new List<DamageModifier>();
            foreach (var mod in modifiers)
            {
                switch (mod.Type)
                {
                    case DamageModifierType.ExtraDamage:
                        extraDamageList.Add(mod);
                        break;
                    case DamageModifierType.Conversion:
                        conversionList.Add(mod);
                        break;
                    case DamageModifierType.Modifier:
                        modifierList.Add(mod);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var totalDamageList = new Dictionary<DamageType, Damage>
            {
                { baseDamage.Type, baseDamage }
            };
            var conversionMaxVal = new Godot.Collections.Dictionary<DamageType, float>();
            foreach (var mod in conversionList.Where(mod => mod.OutputDamageType == orgDamage.Type).Where(mod => !conversionMaxVal.TryAdd(mod.TargetDamageType, mod.Value)))
            {
                conversionMaxVal[mod.TargetDamageType] += mod.Value;
            }

            var totalConversion = 0f;
            foreach (var conv in conversionMaxVal)
            {
                if (totalConversion >= 100)
                {
                    break;
                }
            
                var convValue = 0f;
                if (totalConversion + conv.Value < 100)
                {
                    totalConversion += conv.Value;
                    convValue = conv.Value;
                }
                else
                {
                    convValue = 100 - totalConversion;
                    totalConversion = 100;
                }

                var convDamage = new Damage
                {
                    DamageNumber = totalDamageList[orgDamage.Type].DamageNumber * convValue / 100,
                    AilmentChange = baseDamage.AilmentChange,
                    Type = conv.Key,
                };
                totalDamageList[orgDamage.Type].DamageNumber -= convDamage.DamageNumber;
                totalDamageList.Add(conv.Key, convDamage);
            }

            foreach (var mod in extraDamageList
                         .Where(mod => mod.TargetDamageType == orgDamage.Type)
                         .Where(mod => !totalDamageList.TryAdd(mod.OutputDamageType, new Damage() { DamageNumber = orgDamage.DamageNumber * mod.Value / 100, Type = mod.OutputDamageType, AilmentChange = baseDamage.AilmentChange})))
            {
                totalDamageList[mod.OutputDamageType].DamageNumber += (orgDamage.DamageNumber * mod.Value / 100);
            }

            var modifierMaxList = new Godot.Collections.Dictionary<DamageType, float>();
            foreach (var mod in modifierList.Where(mod => mod.TargetDamageType == orgDamage.Type).Where(mod => !modifierMaxList.TryAdd(mod.OutputDamageType, mod.Value)))
            {
                modifierMaxList[mod.OutputDamageType] += mod.Value;
            }

            foreach (var mod in modifierList.Where(mod => totalDamageList.ContainsKey(mod.TargetDamageType)))
            {
                totalDamageList[mod.TargetDamageType].DamageNumber *= (1 + mod.Value / 100);
            }

            foreach (var dmg in totalDamageList)
            {
                if (!calculatedDamages.TryAdd(dmg.Key, dmg.Value))
                {
                    calculatedDamages[dmg.Key].DamageNumber += dmg.Value.DamageNumber;
                    calculatedDamages[dmg.Key].AilmentChange 
                        = Math.Max(calculatedDamages[dmg.Key].AilmentChange, dmg.Value.AilmentChange);
                }
            }
        }
    }

    public static Godot.Collections.Dictionary<DamageType, Variant> ConvertDmgDictToGodotDict(Dictionary<DamageType, Damage> dict)
    {
        var godotDict = new Godot.Collections.Dictionary<DamageType, Variant>();
        foreach (var entry in dict)
        {
            godotDict.Add(entry.Key, entry.Value.ToDict());
        }

        return godotDict;
    }

    public static Dictionary<DamageType, Damage> ConvertGodotDmgDictToSystemDict(
        Godot.Collections.Dictionary<DamageType, Variant> dict)
    {
        var systemDict = new Dictionary<DamageType, Damage>();
        foreach (var entry in dict)
        {
            systemDict.Add(entry.Key, Damage.FromDict((Godot.Collections.Dictionary<string, Variant>)entry.Value));
        }

        return systemDict;
    }
}
