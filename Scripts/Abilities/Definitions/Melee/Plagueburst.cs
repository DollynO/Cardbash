using System;
using System.Collections.Generic;
using CardBase.Scripts.Abilities.HitMods;
using CardBase.Scripts.PlayerScripts;
using Godot;
using AgonyDebuff = CardBase.Scripts.Abilities.Buffs.DoTs.Agony;
using CorruptionDebuff = CardBase.Scripts.Abilities.Buffs.DoTs.Corruption;

namespace CardBase.Scripts.Abilities;

public class Plagueburst : Ability
{
    private int consumedBuffs;
    private bool contagionBurstEnabled;

    public Plagueburst(PlayerCharacter creator) : base(AbilityIds.PlagueburstGuid, creator)
    {
        DisplayName = "Plagueburst";
        Description = "Bursts all corruption and agony buffs to cause damage. Upgrade 1: consumed debuffs spread to nearby enemies.";
        IconPath = "res://Sprites/SkillIcons/Dark/15_Rod_of_Darkness.png";
        _hitModifiers.Add(new ConsumeBuffHitModifier(
            (count, context) => OnBuffConsumed(count, context, typeof(CorruptionDebuff)),
            typeof(CorruptionDebuff),
            true));
        _hitModifiers.Add(new ConsumeBuffHitModifier(
            (count, context) => OnBuffConsumed(count, context, typeof(AgonyDebuff)),
            typeof(AgonyDebuff),
            true));
    }

    private void OnBuffConsumed(int count, HitContext context, Type consumedType)
    {

        bool doubleBuffConsumed = consumedBuffs > 0;
        consumedBuffs += count;
        context.Damages[DamageType.Darkness].DamageNumber += (float)(count * BaseDamage);
        if (contagionBurstEnabled)
        {
            SpawnContagionBurst(context.Target, consumedType, count);
        }

        if (doubleBuffConsumed && UpdateCounter == 2)
        {
            context.Damages[DamageType.Darkness].DamageNumber *= 2;
        }
    }

    private void SpawnContagionBurst(IEntityComponent origin, Type consumedType, int consumedCount)
    {
        if (origin is not Node2D originNode)
        {
            return;
        }

        var stats = new AoeBaseStats()
        {
            ActivationTime = ConfigParam("contagionActivationTime", 0.05f),
            Radius = ConfigParam("contagionRadius", 90f),
            Duration = 0f,
            Callbacks = new AoeBaseCallbacks
            {
                OnActivation = (targets, _) => ApplyContagion(targets, origin, consumedType, consumedCount),
            },
            Owner = Caller,
            AbilityGUID = GUID,
            Angle = 360f,
            IsStationary = true,
            StationaryPosition = originNode.GlobalPosition,
            CanAffectOwner = false,
        };
        GlobalAbilitySpawner.SpawnAoe(stats);
    }

    private void ApplyContagion(List<IEntityComponent> targets, IEntityComponent origin, Type consumedType, int consumedCount)
    {
        foreach (var target in targets)
        {
            if (target == origin)
            {
                continue;
            }

            if (!target.TryGetComponent<BuffManagerComponent>(out var buffManager))
            {
                continue;
            }

            if (consumedType == typeof(CorruptionDebuff))
            {
                buffManager.ApplyBuff(new CorruptionDebuff(Caller, target));
            }
            else if (consumedType == typeof(AgonyDebuff))
            {
                var stacksToSpread = Math.Min(consumedCount, ConfigParam("contagionAgonyStacks", 2));
                for (var i = 0; i < stacksToSpread; i++)
                {
                    buffManager.ApplyBuff(new AgonyDebuff(Caller, target));
                }
            }
        }
    }

    public override void RoundReset()
    {
        return;
    }

    public override void InternalUse()
    {
        var stats = new AoeBaseStats()
        {
            ActivationTime = ConfigParam("activationTime", 3f),
            Radius = ConfigParam("radius", 100f),
            Duration = ConfigParam("duration", 0f),
            Callbacks = new AoeBaseCallbacks { OnActivation = OnActivation },
            Owner = Caller,
            AbilityGUID = GUID,
            Angle = ConfigParam("angle", 90f),
        };
        ApplyAoeConfig(stats);
        GlobalAbilitySpawner.SpawnAoe(stats);
    }

    private void OnActivation(List<IEntityComponent> playersHit, AoeBase source)
    {
        if (playersHit.Count > 0)
        {
            foreach (var player in playersHit)
            {
                consumedBuffs = 0;
                var damage = new Damage
                {
                    DamageNumber = (float)BaseDamage,
                    AilmentChance = Damage.DEFAULT_AILMENT_CHANGE,
                    Type = BaseType
                };
                var damageDict = new Dictionary<DamageType, Damage> { { damage.Type, damage } };
                var ctx = new HitContext()
                {
                    AbilityGuid = GUID,
                    Source = Caller,
                    Target = player,
                    Damages = damageDict
                };
                var hit = new Hit(source, ctx);
                if (player.TryGetComponent(out DamageAbleComponent dac))
                {
                    dac.ReceiveHit(hit);
                }
            }
        }
    }

    protected override void ApplyUpdate1()
    {
        contagionBurstEnabled = true;
    }

    protected override void ApplyUpdate2()
    {
    }
}
