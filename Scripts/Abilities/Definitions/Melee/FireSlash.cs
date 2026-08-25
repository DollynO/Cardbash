using System.Collections.Generic;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class FireSlash : Ability
{
    public FireSlash(PlayerCharacter creator) : base(AbilityIds.FireSlashGuid, creator)
    {
        this.DisplayName = "Fire Slash";
        this.Description = "Melee fire strike. ";
        this.IconPath = "res://Sprites/SkillIcons/Fire/10_Fire_Tongue.png";
    }

    public override void RoundReset()
    {
        return;
    }

    public override void InternalUse()
    {
        var stats = new AoeBaseStats()
        {
            Angle = ConfigParam("angle", 120f),
            ActivationTime = ConfigParam("activationTime", 0.2f),
            Radius = ConfigParam("radius", 160f),
            AngleOffset = ConfigParam("angleOffset", 0f),
            Owner = Caller,
            Callbacks = new AoeBaseCallbacks { OnActivation = OnActivation },
        };
        ApplyAoeConfig(stats);
        GlobalAbilitySpawner.SpawnAoe(stats);
    }

    private void OnActivation(List<IEntityComponent> arg1, AoeBase arg2)
    {

        foreach (var playerCharacter in arg1)
        {
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
                dac.ReceiveHit(hit);
            }

            if (UpdateCounter != 2) continue;
            
            var stats = new AoeBaseStats()
            {
                Angle = 360f,
                ActivationTime = ConfigParam("activationTimeWildfire", 1f),
                Radius = ConfigParam("radiusWildfire", 300f),
                AngleOffset = ConfigParam("angleOffsetWildfire", 0f),
                Owner = Caller,
                Callbacks = new AoeBaseCallbacks { OnActivation = OnActivation },
            };
            ApplyAoeConfig(stats);
            GlobalAbilitySpawner.SpawnAoe(stats);
        }
    }

    protected override void ApplyUpdate1()
    {
        BaseAilmentChance = 0.35f;
    }

    protected override void ApplyUpdate2()
    {
    }
}
