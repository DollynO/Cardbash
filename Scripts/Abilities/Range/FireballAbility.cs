using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class FireballAbility : ProjectileAbility
{
    private float chargePower = 0;
    public FireballAbility(PlayerCharacter creator) : base("EE277E3F-A8D2-4AE8-9DE4-01B8158DD000", creator)
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
            Direction = null,
            Speed = 300,
            TimeToBeALive = 4,
            SpritePath = "res://Sprites/Projectiles/fireBallProjectile.png",
            BouncingCount = 3,
            PiercingCount = 1,
            Scale = new Vector2(0.33f, 0.33f),
            Color = new Vector3(0.909f, 0.264f, 0.446f)
            
        };
    }
}