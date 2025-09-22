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
       this.BaseCooldown = 10;
       this.BaseDamage = 5;
       this.BaseType = DamageType.Ice;
       this.SpawnCount = 2;
       this.SpawnDelay = 0.5f;
    }

    protected override ProjectileStats GetProjectileStats()
    {
        return new ProjectileStats
        {
            Caller = Caller,
            Direction = null,
            Speed = 500,
            TimeToBeALive = 4,
            SpritePath = "res://Sprites/Projectiles/fireBallProjectile.png",
            BouncingCount = 3,
            PiercingCount = 1,
            Scale = new Vector2(0.33f, 0.33f),
            Color = new Vector3(0.256f, 0.757f, 0.914f),
        };
    }


    protected override void InternalUpdate()
    {
        
    }
}