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
        return new ProjectileStats
        {
            Caller = Caller,
            Direction = Caller.GetLookAtDirection(),
            Speed = 500,
            TimeToBeALive = 4,
            AnimationResourcePath = "res://AnimationRes/Projectile/Ice/I_LargeBlue.tres",
            BouncingCount = 3,
            PiercingCount = 1,
            OnHit = OnHit,
        };
    }

    private void OnHit(IHitableObject arg1, Projectile arg2)
    {
        
    }


    protected override void InternalUpdate()
    {
        
    }
}