using System;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public partial class ProjectileAbility : Ability
{
    protected ProjectileStats ProjectileStats;
    private PackedScene ProjectileScene = GD.Load<PackedScene>("res://Scenes//Projectile.tscn");
    
    protected ProjectileAbility(string guid) : base(guid)
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
        if (ProjectileStats == null)
        {
            return;
        }

        var abilitySpawner = Caller.GetTree().Root.GetNode<AbilitySpawner>("/root/Main/Game/AbilitySpawner");
        var dict = new Dictionary<string, Variant>
        {
            ["spawn_type"] = (int)SpawnType.SpawnTypeProjectile,
            ["damage"] = DamageCalculator.CalculateTotalDamage(
                new Damage {DamageNumber = (float)BaseDamage, Type = BaseType},
                Caller.DamageModifier),
            ["speed"] = ProjectileStats.Speed,
            ["direction"] = ProjectileStats.Direction ?? Caller.GetLookAtDirection(),
            ["sprite_path"] = ProjectileStats.SpritePath,
            ["time_to_be_a_live"] = ProjectileStats.TimeToBeALive,
            ["caller_id"] = Caller.PlayerId,
            ["piercing_count"] =  ProjectileStats.PiercingCount,
            ["bouncing_count"] = ProjectileStats.BouncingCount,
            ["start_position"] = Caller.GetProjectileStartPosition()
        };
        abilitySpawner.SpawnObject(dict);
    }
}