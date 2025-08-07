using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class FireballAbility : ProjectileAbility
{
    public FireballAbility(PlayerCharacter creator) : base("EE277E3F-A8D2-4AE8-9DE4-01B8158DD000", creator)
    {
       this.DisplayName = "Fireball";
       this.Description = "Fireball Description";
       this.IconPath = "res://Sprites/SkillIcons/Fire/7_Fireball.png";
       this.MaxStack = 2;
       this.BaseCooldown = 1;
       this.BaseDamage = 100;
       this.BaseType = DamageType.Ice;
    }

    protected override void InternalUpdate()
    {
        
    }
    protected override ProjectileStats GetProjectileStats()
    {
        return new ProjectileStats
        {
            Caller = Caller,
            Direction = null,
            Speed = 100,
            TimeToBeALive = 4,
            SpritePath = "res://Sprites/Projectiles/fireBallProjectile.png",
            BouncingCount = 3,
            PiercingCount = 1
        };
    }
}