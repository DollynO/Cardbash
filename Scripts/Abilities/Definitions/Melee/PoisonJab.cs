using System.Collections.Generic;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class PoisonJab : Ability
{
    private bool toxicWoundEnabled;
    private bool venomDashEnabled;

    public PoisonJab(PlayerCharacter creator) : base(AbilityIds.PoisonJabGuid, creator)
    {
        this.DisplayName = "Poison Jab";
        this.Description = "Well poison jab. Upgrade 1: poisoned targets take increased poison damage briefly. Upgrade 2: lunge before the jab.";
        this.IconPath = "res://Sprites/SkillIcons/Poison/20_Poison_Bone.png";
    }

    public override void RoundReset()
    {
        return;
    }

    public override void InternalUse()
    {
        if (venomDashEnabled
            && Caller.TryGetComponent(out MoveComponent moveComponent)
            && Caller.TryGetComponent(out AimComponent aimComponent))
        {
            moveComponent.ForcePull(
                aimComponent.GetProjectileStartPosition(),
                ConfigParam("venomDashStrength", 650f),
                ConfigParam("venomDashDuration", 0.15f));
        }

        var stats = new AoeBaseStats()
        {
            Angle = ConfigParam("angle", 120f),
            ActivationTime = ConfigParam("activationTime", 0.8f),
            Radius = ConfigParam("radius", 40f),
            AngleOffset = ConfigParam("angleOffset", 0f),
            Owner = Caller,
            AbilityGUID = GUID,
            Callbacks = new AoeBaseCallbacks { OnActivation = OnActivation },
        };
        ApplyAoeConfig(stats);
        GlobalAbilitySpawner.SpawnAoe(stats);
    }

    private void OnActivation(List<IEntityComponent> arg1, AoeBase arg2)
    {

        foreach (var playerCharacter in arg1)
        {
            var targetWasPoisoned = playerCharacter.TryGetComponent(out BuffManagerComponent buffManager)
                                    && buffManager.CountBuff(typeof(PoisonDebuff)) > 0;
            var damage = new Damage
            {
                DamageNumber = (float)BaseDamage,
                AilmentChance = BaseAilmentChance,
                Type = BaseType
            };
            var damageDict = new Dictionary<DamageType, Damage>()
            {
                { BaseType, damage }
            };
            var ctx = new HitContext()
            {
                AbilityGuid = GUID,
                Target = playerCharacter,
                Damages = damageDict,
                Source = Caller,
            };
            var hit = new Hit(arg2, ctx);
            if (playerCharacter.TryGetComponent(out DamageAbleComponent dac))
            {
                var hitLanded = dac.ReceiveHit(hit);
                if (hitLanded && toxicWoundEnabled && targetWasPoisoned && buffManager != null)
                {
                    buffManager.ApplyBuff(new ToxicWound(
                        Caller,
                        playerCharacter,
                        ConfigParam("toxicWoundPoisonDamageIncrease", 0.25f),
                        ConfigParam("toxicWoundDuration", 4f)));
                }
            }
        }
    }

    protected override void ApplyUpdate1()
    {
        toxicWoundEnabled = true;
    }

    protected override void ApplyUpdate2()
    {
        venomDashEnabled = true;
    }
}
