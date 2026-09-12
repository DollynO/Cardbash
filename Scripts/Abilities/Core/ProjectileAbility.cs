using System;
using System.Collections.Generic;
using CardBase.Scripts.Abilities.ProjectileBehavior;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public abstract class ProjectileAbility : Ability
{
    protected int SpawnCount;
    protected float SpawnDelay;
    protected List<IProjectileBehavior> Behaviors =  new();

    protected ProjectileAbility(string guid, PlayerCharacter creator) : base(guid, creator)
    {
    }

    protected virtual void PostSpawnProjectile(Projectile projectile)
    {

    }

    protected virtual bool PreSpawnProjectile()
    {
        return true;
    }

    public override void InternalUse()
    {
        if (PreSpawnProjectile())
        {
            var projectile = SpawnProjectile();
            if (projectile is { } proj)
            {
                PostSpawnProjectile(proj);
            }
            else
            {
                OnProjectileSpawnFailed();
                RefundFailedSpawn();
                MarkUseFailed();
            }
        }
    }

    protected virtual void OnProjectileSpawnFailed()
    {
    }

    protected Projectile SpawnProjectile()
    {
        var spawnRequest = GetProjectileSpawnRequest();
        if (spawnRequest == null)
        {
            return null;
        }

        if (Caller.TryGetComponent(out AimComponent aimComponent))
        {
            spawnRequest.StartPosition = aimComponent.GetProjectileStartPosition();
        }
        else
        {
            spawnRequest.StartPosition = ((Node2D)Caller).GlobalPosition;
        }

        spawnRequest.Caller = Caller;
        var projectile = GlobalAbilitySpawner.SpawnProjectile(spawnRequest, GetProjectileRuntime());
        if (projectile == null)
        {
            return null;
        }

        projectile.OnCollision += _onProjectileCollided;
        projectile.OnPiercing += _onProjectilePierced;
        projectile.OnDestroyed += _onProjectileDestroyed;
        return projectile;
    }

    private void RefundFailedSpawn()
    {
        CurrentStack = Math.Min(CurrentStack + 1, MaxStack);
    }

    protected virtual void _onProjectileDestroyed(Vector2 position, Projectile projectile)
    {
        return;
    }

    protected virtual void _onProjectilePierced(Vector2 position, Projectile projectile)
    {
        return;
    }

    protected virtual void _onProjectileCollided(Vector2 position, Projectile projectile)
    {
        return;

    }

    protected virtual ProjectileRuntime GetProjectileRuntime()
    {
        return ProjectileRuntime.Empty();
    }

    protected Vector2 GetAimDirection()
    {
        return Caller.TryGetComponent(out AimComponent aimComponent) ? aimComponent.GetLookAtDirection() : Vector2.Zero;
    }

    protected ProjectileSpawnRequest AimedProjectile(string animationPath, float speed, float lifetime)
    {
        return new ProjectileSpawnRequest
        {
            Caller = Caller,
            Movement = new ProjectileMovementConfig
            {
                Direction = GetAimDirection(),
                Speed = speed,
            },
            Lifetime = new ProjectileLifetimeConfig
            {
                Seconds = lifetime,
            },
            Visual = new ProjectileVisualConfig
            {
                AnimationPath = animationPath,
            },
        };
    }

    protected ProjectileRuntime CreateProjectileRuntime(Action<IEntityComponent, Projectile> onHit = null, params IProjectileBehavior[] behaviors)
    {
        var behaviorList = new List<IProjectileBehavior>(behaviors);
        behaviorList.AddRange(Behaviors);
        
        return new ProjectileRuntime
        {
            OnHit = onHit,
            Behaviors = behaviorList,
        };
    }

    protected abstract ProjectileSpawnRequest GetProjectileSpawnRequest();
}
