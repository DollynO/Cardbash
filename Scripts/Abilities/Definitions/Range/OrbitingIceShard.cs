using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class OrbitingIceShard : Ability
{
    private int maxProjectiles = 3;
    private float baseStunDuration = 5;
    private Ring ring;

    public OrbitingIceShard(PlayerCharacter creator) : base(AbilityIds.OrbitingIceShard, creator)
    {
        this.DisplayName = "Orbiting Ice Shard";
        this.Description = "";
        this.IconPath = "res://Sprites/SkillIcons/Snow/16_Ice_Ball.png";
        this.MaxStack = 1;
        this.BaseCooldown = 2;
        this.BaseDamage = 1;
        this.BaseType = DamageType.Ice;
        this.BaseAilmentChance = 1f;
        this.AutoCast = true;

        if (creator != null && creator.TryGetComponent(out AbilityComponent abilityComponent))
        {
            creator.EventBus.MatchEventBus.RoundStartEventHandler += CreatorOnNewRoundStarted;
            ring = abilityComponent.RingContainer.AddRing(100, 1);
        }
    }

    private void CreatorOnNewRoundStarted(object sender, MatchEventArgs e)
    {
        CurrentStack = 0;
        CurrentCooldown = BaseCooldown;
    }

    public override void RoundReset()
    {
        return;
    }

    public override void ClearAbility()
    {
        if (Caller == null)
        {
            return;
        }

        Caller.EventBus.MatchEventBus.RoundStartEventHandler -= CreatorOnNewRoundStarted;
    }

    protected override bool preventAutoCast()
    {
        return ring.GetStackCount() >= maxProjectiles;
    }

    public override void InternalUse()
    {
        var spawnRequest = new ProjectileSpawnRequest
        {
            Caller = Caller,
            Movement = new ProjectileMovementConfig
            {
                Speed = 0,
            },
            Lifetime = new ProjectileLifetimeConfig
            {
                Seconds = -1,
            },
            Visual = new ProjectileVisualConfig
            {
                AnimationPath = "res://AnimationRes/Projectile/Ice/I_RockLargeBlue.tres",
            },
            StartPosition = Caller.TryGetComponent(out AimComponent aimComponent)
                ? aimComponent.GetProjectileStartPosition()
                : ((Node2D)Caller).GetGlobalPosition()
        };

        var runtime = new ProjectileRuntime
        {
            OnHit = OnHit,
        };
        var projectile = GlobalAbilitySpawner.SpawnProjectile(spawnRequest, runtime);

        projectile.OnDestroyed += ProjectileOnOnDestroyed;
        ring.AddNode(projectile);
    }

    private void OnHit(IEntityComponent hitObject, Projectile source)
    {
        if (hitObject.TryGetComponent(out DamageAbleComponent dac))
        {
            var ctx = new HitContext();
            ctx.Target = hitObject;
            ctx.Source = Caller;
            ctx.AbilityGuid = GUID;
            var damage = new Damage { DamageNumber = (float)BaseDamage, Type = BaseType, AilmentChance = BaseAilmentChance };
            var damageDict = new Dictionary<DamageType, Damage>()
            {
                { damage.Type, damage }
            };
            ctx.Damages = damageDict;
            var hit = new Hit(source, ctx);
            if (!dac.ReceiveHit(hit))
            {
                return;
            }

            if (hitObject.TryGetComponent<BuffManagerComponent>(out var buffManagerComponent))
            {
                if (buffManagerComponent.CountBuff(typeof(Frost)) > 5)
                {
                    buffManagerComponent.ConsumeBuff(typeof(Frost));
                    if (hitObject.TryGetComponent<MoveComponent>(out var moveComponent))
                    {
                        moveComponent.ApplyStun(baseStunDuration);
                    }
                }
            }
        }
    }

    protected override void InternalUpdate()
    {
        switch (UpdateCounter)
        {
            case 1:
                this.BaseAilmentChance = 0.3f;
                return;
            case 2:
                return;
            default:
                return;
        }
    }

    private void ProjectileOnOnDestroyed(Vector2 position, Projectile projectile)
    {
        ring.RemoveNode(projectile, false);
    }
}

public class OrbitingIceShardHitModifier : IHitModifier
{

    public void ApplyBefore(HitContext ctx)
    {
        return;
    }

    public void ApplyAfter(HitContext ctx)
    {

    }
}
