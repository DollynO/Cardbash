using System;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.Abilities.ProjectileBehavior;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class Snowball : ProjectileAbility
{
    public Snowball(PlayerCharacter creator) : base(AbilityIds.SnowballGuid, creator)
    {
        this.DisplayName = "Snowball";
        this.Description = "Shoots a snowball";
        this.IconPath = "res://Sprites/SkillIcons/Snow/16_Ice_Ball.png";
        this.BaseCooldown = 1;
        this.BaseDamage = 1;
        this.BaseType = DamageType.Ice;
    }

    public override void RoundReset()
    {
        return;
    }

    protected override void InternalUpdate()
    {
        
    }

    protected override ProjectileRuntime GetProjectileRuntime()
    {
        return CreateProjectileRuntime(onHit, new SizeIncreaseBehavior(1f, 10.0f));
    }

    protected override ProjectileSpawnRequest GetProjectileSpawnRequest()
    {
        var request = AimedProjectile("res://AnimationRes/Projectile/MagicMissile/mm_lrage_blue.tres", 100, -1);
        request.Health.Life = 100;
        request.Collision.CollisionMask = 1 << 2;
        request.Visual.AnimationOffset = new Vector2(-8, 0);
        request.Visual.Scale = new Vector2(1.0f, 1.0f);
        return request;
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
