using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts;

public class HitResolver
{
    public static void ApplyDamage(HitContext ctx)
    {
        if (!ctx.Target.TryGetComponent<HealthComponent>(out var targetHealthComponent))
        {
            return;
        }
            
            var hitMods = new List<IHitModifier>();
            if (ctx.Source.TryGetComponent<AbilityComponent>(out var component))
            {
                var abilityHitMods = component.Abilities.FirstOrDefault(a => a.GUID == ctx.AbilityGuid)?.GetHitModifiers();
                if (abilityHitMods != null)
                {
                    hitMods.AddRange(abilityHitMods);
                }
            }

            foreach (var mod in hitMods)
            {
                mod.ApplyBefore(ctx);
            }
            
            var damageMods = new List<DamageModifier>();
            if (ctx.Source.TryGetComponent<StatblockComponent>(out var srcStatblock))
            {
                damageMods.AddRange(srcStatblock.DamageModifier);
            }
            DamageCalculator.CalculateTotalDamage(ctx.Damages, damageMods);

            ctx.Target.TryGetComponent<StatblockComponent>(out var targetStatblock);
            // apply mitigation
            foreach (var dmg in ctx.Damages)
            {
                var dr = 0f;
                
                if (targetStatblock != null)
                {
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
                } 

                ctx.Damages[dmg.Key].DamageNumber = dmg.Value.DamageNumber * (1 - dr);
                targetHealthComponent.ApplyDamage(ctx.Damages[dmg.Key], ctx.Source);
                ApplyDamageTypeAilment(dmg.Value.Type, dmg.Value.AilmentChance, ctx.Source, ctx.Target);
            }
            
            if (!targetHealthComponent.IsDead)
            {
                foreach (var mod in hitMods)
                {
                    mod.ApplyAfter(ctx);
                }   
            }
    }
    
    private static void ApplyDamageTypeAilment(DamageType type, float ailmentChance, IEntityComponent target, IEntityComponent attacker)
    {
        if (!target.TryGetComponent<BuffManagerComponent>(out var buffManagerComponent))
        {
            return;
        }
        
        var rnd = new Random();
        var chance = rnd.NextDouble();
        if (chance > ailmentChance)
        {
            return;
        }
        
        switch (type)
        {
            case DamageType.Fire:
                buffManagerComponent.ApplyBuff(new BurnDebuff(attacker, target));
                break;
            case DamageType.Physical:
                break;
            case DamageType.Poison:
                buffManagerComponent.ApplyBuff(new PoisonDebuff(attacker, target));
                break;
            case DamageType.Ice:
                buffManagerComponent.ApplyBuff(new Frost(attacker, target));
                break;
            case DamageType.Lightning:
                buffManagerComponent.ApplyBuff(new ShockDebuff(attacker, target));
                break;
            case DamageType.Darkness: 
                buffManagerComponent.ApplyBuff(new Darkness(attacker, target));
                break;
            case DamageType.Holy:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}