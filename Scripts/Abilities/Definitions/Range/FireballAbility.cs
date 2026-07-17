using System.Collections.Generic;
using CardBase.Scripts.Abilities.HitMods;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class FireballAbility : ProjectileAbility
{
    private ExplodeOnHitModifier explodeOnHitModifier;

    public FireballAbility(PlayerCharacter creator) : base(AbilityIds.FireballGuid, creator)
    {
        this.DisplayName = "Fireball";
        this.Description = "Fireball Description";
        this.IconPath = "res://Sprites/SkillIcons/Fire/7_Fireball.png";
    }

    public override void RoundReset()
    {
        CancelAbility();
    }

    public override void ClearAbility()
    {
        explodeOnHitModifier?.CancelActiveAoes();
    }

    protected override void ApplyUpdate1()
    {
        // increase max stack
        this.MaxStack += 1;
    }

    protected override void ApplyUpdate2()
    {
        explodeOnHitModifier = new ExplodeOnHitModifier(Caller, GUID);
        this._hitModifiers.Add(explodeOnHitModifier);
    }

    protected override void InternalCancel()
    {
        explodeOnHitModifier?.CancelActiveAoes();
    }

    protected override ProjectileRuntime GetProjectileRuntime()
    {
        return CreateProjectileRuntime(OnHit);
    }

    protected override ProjectileSpawnRequest GetProjectileSpawnRequest()
    {
        var request = AimedProjectile(
            "res://AnimationRes/Projectile/MagicMissile/mm_lrage_blue.tres",
            ConfigParam("projectileSpeed", 300f),
            ConfigParam("projectileLifetime", 10f));
        ApplyProjectileConfig(request);
        return request;
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
