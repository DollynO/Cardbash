using System;
using System.Collections.Generic;
using System.Linq;

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
    public static Godot.Collections.Dictionary<DamageType, float> CalculateTotalDamage(Damage orgDamage, List<DamageModifier> modifiers)
    {
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
        
        var totalDamageList = new Godot.Collections.Dictionary<DamageType, float>
        {
            { baseDamage.Type, baseDamage.DamageNumber }
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
            
            var convDamage = totalDamageList[orgDamage.Type] * convValue / 100;
            totalDamageList[orgDamage.Type] -= convDamage;
            totalDamageList.Add(conv.Key, convDamage);
        }
        
        foreach (var mod in extraDamageList.Where(mod => mod.TargetDamageType == orgDamage.Type).Where(mod => !totalDamageList.TryAdd(mod.OutputDamageType, orgDamage.DamageNumber * mod.Value / 100)))
        {
            totalDamageList[mod.OutputDamageType] += (orgDamage.DamageNumber * mod.Value / 100);
        }
        
        var modifierMaxList = new Godot.Collections.Dictionary<DamageType, float>();
        foreach (var mod in modifierList.Where(mod => mod.TargetDamageType == orgDamage.Type).Where(mod => !modifierMaxList.TryAdd(mod.OutputDamageType, mod.Value)))
        {
            modifierMaxList[mod.OutputDamageType] += mod.Value;
        }

        foreach (var mod in modifierList.Where(mod => totalDamageList.ContainsKey(mod.TargetDamageType)))
        {
            totalDamageList[mod.TargetDamageType] *= (1 + mod.Value / 100);
        }
        
        return totalDamageList;
    }
}
