using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class FireballAbility : ProjectileAbility
{
    private float chargePower = 0;
    public FireballAbility(PlayerCharacter creator) : base(AbilityIds.FireballGuid, creator)
    {
       this.DisplayName = "Fireball";
       this.Description = "Fireball Description";
       this.IconPath = "res://Sprites/SkillIcons/Fire/7_Fireball.png";
       this.MaxStack = 2;
       this.BaseCooldown = 1;
       this.BaseDamage = 85;
       this.BaseType = DamageType.Fire;
    }

    protected override void InternalUpdate()
    {
        
    }
    
    protected override ProjectileStats GetProjectileStats()
    {
        return new ProjectileStats
        {
            Caller = Caller,
            Direction = Vector2.Zero,
            Speed = 300,
            TimeToBeALive = 15,
            AnimationResourcePath = "res://Sprites/Projectiles/fireBallProjectile.png",
            BouncingCount = 3,
            PiercingCount = 1,
        };
    }
}