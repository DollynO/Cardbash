using Godot;

namespace CardBase.Scripts.Abilities;

public partial class IceArrowAbility : ProjectileAbility
{
    public IceArrowAbility() : base("8E481FBF-DE0A-4673-BDF1-50EE9CC041D4")
    {
       this.DisplayName = "Ice Arrow";
       this.Description = "Fires an ice arrow";
       this.IconPath = "res://Sprites/SkillIcons/Snow/8_Ice_Arrow.png";
       this.BaseCooldown = 10;
       this.Name = "Ice Arrow";
       this.BaseDamage = 100;
       this.ProjectileStats = new ProjectileStats()
       {
           Caller = this.Caller,
           Damage = (float)this.BaseDamage,
           Direction = Vector2.Right,
           Speed = 100,
           TimeToBeALive = 3,
           SpriteTexture = GD.Load<Texture2D>("res://Sprites/Projectiles/fireBallProjectile.png"),
       };
    }

    protected override void InternalUse()
    {
        Caller.ApplyDamage(BaseDamage, Caller);
    }
    
    protected override void InternalUpdate()
    {
        
    }
}