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

    protected override ProjectileRuntime GetProjectileRuntime()
    {
        return CreateProjectileRuntime(OnHit);
    }

    protected override ProjectileSpawnRequest GetProjectileSpawnRequest()
    {
        var request = AimedProjectile("res://AnimationRes/Projectile/Ice/I_LargeBlue.tres", 500, 4);
        request.Movement.BounceCount = 3;
        request.Collision.PierceCount = 1;
        return request;
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
