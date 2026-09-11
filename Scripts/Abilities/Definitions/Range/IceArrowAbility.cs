using System.Collections.Generic;
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
    }

    protected override ProjectileRuntime GetProjectileRuntime()
    {
        return CreateProjectileRuntime(OnHit);
    }

    protected override ProjectileSpawnRequest GetProjectileSpawnRequest()
    {
        var request = AimedProjectile(
            "res://AnimationRes/Projectile/Ice/I_LargeBlue.tres",
            ConfigParam("projectileSpeed", 500f),
            ConfigParam("projectileLifetime", 4f));
        request.Movement.BounceCount = ConfigParam("bounceCount", 3);
        request.Collision.PierceCount = ConfigParam("pierceCount", 1);
        ApplyProjectileConfig(request);
        return request;
    }

    private void OnHit(IEntityComponent arg1, Projectile arg2)
    {
        if (arg1.TryGetComponent(out DamageAbleComponent damageAbleComponent))
        {
            var damage = new Damage
            {
                Type = BaseType,
                DamageNumber = (float)BaseDamage,
                AilmentChance = BaseAilmentChance,
            };
            var ctx = new HitContext
            {
                AbilityGuid = GUID,
                Damages = new Dictionary<DamageType, Damage>
                {
                    { damage.Type, damage },
                },
                Source = Caller,
                Target = arg1,
            };
            damageAbleComponent.ReceiveHit(new Hit(arg2, ctx));
        }
    }


    public override void RoundReset()
    {
        return;
    }


    protected override void ApplyUpdate1()
    {
    }

    protected override void ApplyUpdate2()
    {
    }
}
