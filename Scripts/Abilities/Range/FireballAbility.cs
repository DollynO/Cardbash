using System;
using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class FireballAbility : ProjectileAbility
{
    private float chargePower = 0;
    public FireballAbility(PlayerCharacter creator) : base(AbilityIds.FireballGuid, creator)
    {
       this.DisplayName = "Fireball";
       this.Description = "Fireball Description";
       this.IconPath = "res://Sprites/SkillIcons/Fire/7_Fireball.png";
       this.MaxStack = 2;
       this.BaseCooldown = 1;
       this.BaseDamage = 20;
       this.BaseType = DamageType.Fire;
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
        var direction = Vector2.Zero;
        if (Caller.TryGetComponent(out AimComponent aimComponent))
        {
            direction = aimComponent.GetLookAtDirection();
        }
        return new ProjectileStats
        {
            Caller = Caller,
            Direction = direction,
            Speed = 300,
            TimeToBeALive = 15,
            AnimationResourcePath = "res://AnimationRes/Projectile/MagicMissile/mm_lrage_blue.tres",
            BouncingCount = 3,
            PiercingCount = 1,
            OnHit = OnHit,
        };
    }
    
    private void OnHit(IEntityComponent arg1, Projectile arg2)
    {
        if (arg1.TryGetComponent(out DamageAbleComponent damageAbleComponent))
        {
            var damageDict = new Dictionary<DamageType, Damage>
            { { BaseType, new Damage()
            {
                Type = BaseType,
                DamageNumber = (float)BaseDamage,
                AilmentChance = BaseAilmentChance,
            } } };
            var ctx = new HitContext()
            {
                AbilityGuid = GUID,
                Damages = damageDict,
                Source = Caller,
                Target = arg1,
            };
            var hit = new Hit(arg2, ctx);
            damageAbleComponent.ReceiveHit(hit);
        }
    }
}