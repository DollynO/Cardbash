using System;
using System.Collections.Generic;
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

    protected override ProjectileStats GetProjectileStats()
    {
        var beList = new List<IProjectileBehavior> {  new SizeIncreaseBehavior(1f, 10.0f) };

        var direction = Vector2.Zero;
        if (Caller.TryGetComponent(out AimComponent aimComponent))
        {
            direction = aimComponent.GetLookAtDirection();
        }
        
        return new ProjectileStats()
        {
            AngleOffset = 0,
            Direction = direction,
            AnimationResourcePath = "res://AnimationRes/Projectile/MagicMissile/mm_lrage_blue.tres",
            AnimationOffset = new Vector2(-8, 0),
            OnHit = onHit,
            Behaviors = beList,
            Speed = 100,
            TimeToBeALive = -1,
            Life = 100,
            Scale = new Vector2(1.0f, 1.0f),
            CollisionMask = 1<<2,
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