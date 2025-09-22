using System;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public abstract class ProjectileAbility : Ability
{
    private PackedScene ProjectileScene = GD.Load<PackedScene>("res://Scenes//Projectile.tscn");

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
        var abilitySpawner = Caller.GetTree().Root.GetNode<AbilitySpawner>("/root/Main/Game/AbilitySpawner");

        var damage = new Damage { DamageNumber = (float)BaseDamage, Type = BaseType, AilmentChange = BaseAilmentChance };
        projectile_stats.Damage = damage;
        projectile_stats.StartPosition = Caller.GetProjectileStartPosition();
        projectile_stats.Caller = Caller;
        var dict = new Dictionary<string, Variant>
        {
            ["spawn_properties"] = new SpawnerBaseProperties
            {
                AbilityGuid = GUID,
                CreatorId = Caller.PlayerId,
                SpawnType = SpawnType.SpawnTypeProjectile,
                SpawnCount = SpawnCount,
                SpawnDelay = SpawnDelay,
                
            }.ToDict(),
            ["object_stats"] = projectile_stats.ToDict(),
            };
        abilitySpawner.SpawnObject(dict);
    }

    public override void RegisterSpawnedNode(Node node)
    {
        if (node is Projectile projectile)
        {
            projectile.OnCollision += _onProjectileCollided;
            projectile.OnPiercing += _onProjectilePierced;
            projectile.OnDestroyed += _onProjectileDestroyed;
            
            InternalRegisterSpawnedNode(node);
        }
    }

    protected virtual void _onProjectileDestroyed(Vector2 position)
    {
        return;
    }

    protected virtual void _onProjectilePierced(Vector2 position)
    {
        return;
    }

    protected virtual void _onProjectileCollided(Vector2 position)
    {
        return;
        
    }

    protected virtual void InternalRegisterSpawnedNode(Node node)
    {
        return;
    }

    protected abstract ProjectileStats GetProjectileStats();
}