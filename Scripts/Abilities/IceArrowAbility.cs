using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class IceArrowAbility : ProjectileAbility
{
    public IceArrowAbility(PlayerCharacter creator) : base("8E481FBF-DE0A-4673-BDF1-50EE9CC041D4", creator)
    {
       this.DisplayName = "Ice Arrow";
       this.Description = "Fires an ice arrow";
       this.IconPath = "res://Sprites/SkillIcons/Snow/8_Ice_Arrow.png";
       this.BaseCooldown = 1;
       this.BaseDamage = 50;
       this.BaseType = DamageType.Ice;
    }

    protected override void InternalUse()
    {
    }

    protected override ProjectileStats GetProjectileStats()
    {
        return new ProjectileStats
        {
            Caller = Caller,
            Direction = null,
            Speed = 300,
            TimeToBeALive = 4,
            SpritePath = "res://Sprites/Projectiles/fireBallProjectile.png",
            BouncingCount = 3,
            PiercingCount = 1
        };
    }


    protected override void InternalUpdate()
    {
        
    }
}