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

    public override void InternalUse()
    {
        base.InternalUse();
        this.Caller.MoveController.ApplyRoot();
    }

    protected override void _onProjectileDestroyed(Vector2 position, Projectile projectile)
    {
        this.Caller.MoveController.RemoveRoot();
    }
    
    

    protected override ProjectileStats GetProjectileStats()
    {
        return new ProjectileStats()
        {
            Caller = Caller,
            Direction = Vector2.Zero,
            Speed = 500,
            TimeToBeALive = 4,
            AnimationResourcePath = "res://Sprites/Projectiles/fireBallProjectile.png",
            CustomProjectilePath = "res://Scenes/Projectiles/GrapplingProjectile.tscn",
            BouncingCount = 0,
            PiercingCount = 0,
        };
    }
}