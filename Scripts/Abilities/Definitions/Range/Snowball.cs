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

    protected override ProjectileRuntime GetProjectileRuntime()
    {
        return CreateProjectileRuntime(
            onHit,
            new SizeIncreaseBehavior(
                ConfigParam("sizeIncreasePerSecond", 1f),
                ConfigParam("healthIncreasePerSecond", 10.0f)));
    }

    protected override ProjectileSpawnRequest GetProjectileSpawnRequest()
    {
        var request = AimedProjectile(
            "res://AnimationRes/Projectile/MagicMissile/mm_lrage_blue.tres",
            ConfigParam("projectileSpeed", 100f),
            ConfigParam("projectileLifetime", -1f));
        request.Health.Life = ConfigParam("projectileHealth", 100f);
        request.Collision.CollisionMask = (uint)ConfigParam("collisionMask", 1 << 2);
        request.Visual.AnimationOffset = new Vector2(ConfigParam("animationOffsetX", -8f), ConfigParam("animationOffsetY", 0f));
        var scale = ConfigParam("projectileScale", 1.0f);
        request.Visual.Scale = new Vector2(scale, scale);
        ApplyProjectileConfig(request);
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
