using System;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public abstract class ProjectileAbility : Ability
{
    private PackedScene ProjectileScene = GD.Load<PackedScene>("res://Scenes//Projectile.tscn");
    
    protected ProjectileAbility(string guid, PlayerCharacter creator) : base(guid, creator)
    {
    }

    protected virtual void InternalUse()
    {
        
    }
    
    public override void Use()
    {
        SpawnProjectile();
        InternalUse();
    }

    private void SpawnProjectile()
    {
        var projectile_stats = GetProjectileStats();
        var abilitySpawner = Caller.GetTree().Root.GetNode<AbilitySpawner>("/root/Main/Game/AbilitySpawner");
        
        var dict = new Dictionary<string, Variant>
        {
            ["spawn_properties"] = new SpawnerBaseProperties
            {
                AbilityGuid = GUID,
                CreatorId = Caller.PlayerId,
                SpawnType = SpawnType.SpawnTypeProjectile
            }.ToDict(),
            ["damage"] = DamageCalculator.CalculateTotalDamage(
                new Damage {DamageNumber = (float)BaseDamage, Type = BaseType},
                Caller.DamageModifier),
            ["speed"] = projectile_stats.Speed,
            ["direction"] = projectile_stats.Direction ?? Caller.GetLookAtDirection(),
            ["sprite_path"] = projectile_stats.SpritePath,
            ["time_to_be_a_live"] = projectile_stats.TimeToBeALive,
            ["caller_id"] = Caller.PlayerId,
            ["piercing_count"] =  projectile_stats.PiercingCount,
            ["bouncing_count"] = projectile_stats.BouncingCount,
            ["start_position"] = Caller.GetProjectileStartPosition()
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