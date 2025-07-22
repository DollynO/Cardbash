using Godot;

namespace CardBase.Scripts.Abilities;

public partial class IceArrowAbility : ProjectileAbility
{
    public IceArrowAbility() : base("8E481FBF-DE0A-4673-BDF1-50EE9CC041D4")
    {
       this.DisplayName = "Ice Arrow";
       this.Description = "Fires an ice arrow";
       this.IconPath = "res://Sprites/SkillIcons/Snow/8_Ice_Arrow.png";
       this.BaseCooldown = 1;
       this.AbilityName = "Ice Arrow";
       this.BaseDamage = 50;
       this.BaseType = DamageType.Ice;
       this.ProjectileStats = new ProjectileStats()
       {
           Caller = this.Caller,
           Direction = Vector2.Right,
           Speed = 100,
           TimeToBeALive = 3,
           SpritePath = "res://Sprites/Projectiles/fireBallProjectile.png",
       };
    }

    protected override void InternalUse()
    {
    }
    
    protected override void InternalUpdate()
    {
        
    }
}