using System;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.Abilities.ProjectileBehavior;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities;

public class Snowball : ProjectileAbility
{
    private SizeIncreaseBehavior behavior;
    public Snowball(PlayerCharacter creator) : base(AbilityIds.SnowballGuid, creator)
    {
        behavior = new SizeIncreaseBehavior(1.0f);
    }

    protected override void InternalUpdate()
    {
        
    }

    protected override ProjectileStats GetProjectileStats()
    {
        return new ProjectileStats()
        {
            AngleOffset = 0,
            AnimationResourcePath = "res://AnimationRes/Projectile/MagicMissile/mm_lrage_blue.tres",
            OnHit = onHit,
        };
    }

    private void onHit(IEntityComponent arg1, Projectile arg2)
    {
        if (arg1.TryGetComponent<BuffManagerComponent>(out var buffManagerComponent))
        {
            var slow = new SnowballSlow(this.Caller, arg1);
            slow.SetSnowballScale(arg2.Scale);
            buffManagerComponent.ApplyBuff(slow);
        }
    }
}