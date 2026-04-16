using System;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class GrapplingHookAbility : ProjectileAbility
{
    public GrapplingHookAbility(PlayerCharacter creator) : base(AbilityIds.GrapplingHookGuid, creator)
    {
        this.Description = "Fires a grappling hook that pulls the enemy to the caster.";
        this.IconPath = "res://Sprites/SkillIcons/Metal/17_Magnet.png";
        this.BaseCooldown = 15;
        this.BaseDamage = 1;
        this.BaseType = DamageType.Physical;
        this.SpawnCount = 1;
        this.SpawnDelay = 0;
    }

    protected override void InternalUpdate()
    {
    }

    public override void RoundReset()
    {
        return;
    }

    public override void InternalUse()
    {
        base.InternalUse();
        if (Caller.TryGetComponent(out MoveComponent moveComponent))
        {
            moveComponent.ApplyRoot();
        }
    }

    protected override void _onProjectileDestroyed(Vector2 position, Projectile projectile)
    {
        if (Caller.TryGetComponent(out MoveComponent moveComponent))
        {
            moveComponent.RemoveRoot();
        }
    }
    
    

    protected override ProjectileSpawnRequest GetProjectileSpawnRequest()
    {
        var request = AimedProjectile("res://Sprites/Projectiles/fireBallProjectile.png", 500, 4);
        request.Movement.Direction = Vector2.Zero;
        request.Visual.ScenePath = "res://Scenes/Projectiles/GrapplingProjectile.tscn";
        return request;
    }
}
