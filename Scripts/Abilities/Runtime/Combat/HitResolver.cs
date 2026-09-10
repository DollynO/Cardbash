using System;
using System.Collections.Generic;
using System.Linq;
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
        if (ctx.Source.TryGetComponent<AbilityComponent>(out var component) && !string.IsNullOrEmpty(ctx.AbilityGuid))
        {
            var abilityHitMods = component.Abilities.FirstOrDefault(a => a.Value.GUID == ctx.AbilityGuid).Value.GetHitModifiers();
            if (abilityHitMods != null)
            {
                hitMods.AddRange(abilityHitMods);
            }
        }
        if (ctx.Target.TryGetComponent<BuffManagerComponent>(out var targetBuffManager))
        {
            hitMods.AddRange(targetBuffManager.GetActiveBuffs<IHitModifier>());
        }

        foreach (var mod in hitMods)
        {
            mod.ApplyBefore(ctx);
        }

        var damageMods = new List<DamageModifier>();
        if (ctx.Source.TryGetComponent<StatblockComponent>(out var srcStatblock))
        {
            damageMods.AddRange(srcStatblock.GetDamageModifiers());
        }

        ctx.Target.TryGetComponent<StatblockComponent>(out var targetStatblock);        
        ctx.Source.TryGetComponent<StatblockComponent>(out var sourceStatblock);        
        DamageCalculator.CalculateTotalDamage(ctx.Damages, damageMods, targetStatblock, sourceStatblock);

        foreach (var dmg in ctx.Damages)
        {
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

    private static void ApplyDamageTypeAilment(DamageType type, float ailmentChance, IEntityComponent attacker, IEntityComponent target)
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
