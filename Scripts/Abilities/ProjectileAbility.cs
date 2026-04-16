using System;
using CardBase.Scripts.Abilities.ProjectileBehavior;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public abstract class ProjectileAbility : Ability
{
    protected int SpawnCount;
    protected float SpawnDelay;
    
    protected ProjectileAbility(string guid, PlayerCharacter creator) : base(guid, creator)
    {
    }

    protected virtual void PostSpawnProjectile(Projectile projectile)
    {
        
    }

    protected virtual void PreSpawnProjectile()
    {
        
    }
    
    public override void InternalUse()
    {
        PreSpawnProjectile();
        PostSpawnProjectile(SpawnProjectile());
    }

    protected Projectile SpawnProjectile()
    {
        var spawnRequest = GetProjectileSpawnRequest();

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
        
        projectile.OnCollision += _onProjectileCollided;
        projectile.OnPiercing += _onProjectilePierced;
        projectile.OnDestroyed += _onProjectileDestroyed;
        return projectile;
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
        return new ProjectileRuntime
        {
            OnHit = onHit,
            Behaviors = new System.Collections.Generic.List<IProjectileBehavior>(behaviors),
        };
    }

    protected abstract ProjectileSpawnRequest GetProjectileSpawnRequest();
}
