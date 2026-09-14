using System.Collections.Generic;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class OrbitingIceShard : PassiveStackAbility
{
    private int maxProjectiles = 3;
    private float baseStunDuration = 5;
    private Ring ring;
    private bool canFireOrbitingShards;

    public OrbitingIceShard(PlayerCharacter creator) : base(AbilityIds.OrbitingIceShard, creator)
    {
        this.DisplayName = "Orbiting Ice Shard";
        this.Description = "Passively creates orbiting ice shards. Upgrade 1: increases Frost chance. Upgrade 2: recast fires all orbiting shards in the aim direction.";
        this.IconPath = "res://Sprites/SkillIcons/Snow/16_Ice_Ball.png";

        
        if (creator != null)
        {
            maxProjectiles = ConfigParam("maxProjectiles", maxProjectiles);
            InitializePassiveStacks(maxProjectiles);

            if (creator.TryGetComponent(out AbilityComponent abilityComponent))
            {
                ring = abilityComponent.RingContainer.AddRing(
                    ConfigParam("ringRadius", 100f),
                    ConfigParam("ringScale", 1f),
                    maxProjectiles);
            }
        }
    }

    protected override bool CanManualCast()
    {
        return canFireOrbitingShards && PassiveStackCount > 0;
    }

    public override void InternalUse()
    {
        FireOrbitingShards();
    }

    protected override bool CreatePassiveStack()
    {
        if (ring == null)
        {
            return false;
        }

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
        ApplyProjectileConfig(spawnRequest);

        var runtime = new ProjectileRuntime
        {
            OnHit = OnHit,
        };
        var projectile = GlobalAbilitySpawner.SpawnProjectile(spawnRequest, runtime);
        if (projectile == null)
        {
            return false;
        }

        projectile.OnDestroyed += ProjectileOnOnDestroyed;
        ring.AddNode(projectile);
        return true;
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
                        moveComponent.ApplyStun(ConfigParam("stunDuration", baseStunDuration));
                    }
                }
            }
        }
    }


    protected override void ApplyUpdate1()
    {
        this.BaseAilmentChance = 0.3f;
    }

    protected override void ApplyUpdate2()
    {
        canFireOrbitingShards = true;
    }

    private void ProjectileOnOnDestroyed(Vector2 position, Projectile projectile)
    {
        if (ring?.RemoveNode(projectile, false) == true)
        {
            ConsumePassiveStack();
        }
    }

    protected override void ClearPassiveStacks()
    {
        if (ring == null)
        {
            base.ClearPassiveStacks();
            return;
        }

        foreach (var node in ring.RemoveAllNodes(false))
        {
            if (node is Projectile projectile)
            {
                projectile.OnDestroyed -= ProjectileOnOnDestroyed;
                projectile.DestroyProjectile();
            }
            else
            {
                node.QueueFree();
            }
        }

        base.ClearPassiveStacks();
    }

    private void FireOrbitingShards()
    {
        var direction = GetAimDirection();
        if (direction == Vector2.Zero)
        {
            direction = Vector2.Right;
        }

        var speed = ConfigParam("recastProjectileSpeed", 500f);
        var lifetime = ConfigParam("recastProjectileLifetime", 4f);
        ConsumeAllPassiveStacks();

        foreach (var node in ring.RemoveAllNodes(false))
        {
            if (node is not Projectile projectile)
            {
                continue;
            }

            var projectileDirection = direction - projectile.GlobalPosition;
            projectile.OnDestroyed -= ProjectileOnOnDestroyed;
            projectile.SpawnRequest.Movement.Direction = projectileDirection;
            projectile.SpawnRequest.Movement.Speed = speed;
            projectile.SpawnRequest.Movement.Mode = MovementMode.STRAIGHT;
            projectile.SpawnRequest.Movement.AngleOffset = 0;
            projectile.Rotation = direction.Angle();
            projectile.RestartLifetime(lifetime);
        }
    }

    private Vector2 GetAimDirection()
    {
        if (Caller != null && Caller.TryGetComponent(out AimComponent aimComponent))
        {
            return aimComponent.GetPlayerMouesPosition(float.MaxValue);
        }

        return Caller is Node2D callerNode
            ? Vector2.Right.Rotated(callerNode.GlobalRotation)
            : Vector2.Right;
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
