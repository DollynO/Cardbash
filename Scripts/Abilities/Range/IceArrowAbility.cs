using System;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class IceArrowAbility : ProjectileAbility
{
    public IceArrowAbility(PlayerCharacter creator) : base(AbilityIds.IceArrowGuid, creator)
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
        var direction = Vector2.Zero;
        if (Caller.TryGetComponent(out AimComponent aimComponent))
        {
            direction = aimComponent.GetLookAtDirection();
        }
        return new ProjectileStats
        {
            Caller = Caller,
            Direction = direction,
            Speed = 500,
            TimeToBeALive = 4,
            AnimationResourcePath = "res://AnimationRes/Projectile/Ice/I_LargeBlue.tres",
            BouncingCount = 3,
            PiercingCount = 1,
            OnHit = OnHit,
        };
    }

    private void OnHit(IEntityComponent arg1, Projectile arg2)
    {
        
    }


    public override void RoundReset()
    {
        return;
    }

    protected override void InternalUpdate()
    {
        
    }
}