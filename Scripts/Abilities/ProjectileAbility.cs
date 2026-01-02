using System;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public abstract class ProjectileAbility : Ability
{
    private PackedScene ProjectileScene = GD.Load<PackedScene>("res://Scenes//Projectiles//Projectile.tscn");

    protected int SpawnCount;
    protected float SpawnDelay;
    
    protected ProjectileAbility(string guid, PlayerCharacter creator) : base(guid, creator)
    {
    }

    protected virtual void PostSpawnProjectile()
    {
        
    }
    
    public override void InternalUse()
    {
        SpawnProjectile();
        PostSpawnProjectile();
    }

    private void SpawnProjectile()
    {
        var projectile_stats = GetProjectileStats();

        projectile_stats.StartPosition = Caller.GetProjectileStartPosition();
        projectile_stats.Caller = Caller;
        var projectile = globalAbilitySpawner.SpawnProjectile(projectile_stats);
        
        projectile.OnCollision += _onProjectileCollided;
        projectile.OnPiercing += _onProjectilePierced;
        projectile.OnDestroyed += _onProjectileDestroyed;
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

    protected abstract ProjectileStats GetProjectileStats();
}