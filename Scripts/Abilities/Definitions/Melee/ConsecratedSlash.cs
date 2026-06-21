using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class ConsecratedSlash : Ability
{
    public ConsecratedSlash(PlayerCharacter creator) : base(AbilityIds.ConsecratedSlashGuid, creator)
    {
        this.DisplayName = "Consecrated Slash";
        this.Description = "Let the crusade begin";
        this.IconPath = "res://Sprites/SkillIcons/Holy/11_Holy_Wave.png";
    }

    public override void RoundReset()
    {
        return;
    }

    public override void InternalUse()
    {
        Caller.TryGetComponent(out AimComponent aimComponent);
        var angle = ConfigParam("angle", 45f);
        var activationTime = ConfigParam("activationTime", 0.8f);
        var radius = ConfigParam("radius", 60f);
        var angleOffset = ConfigParam("angleOffset", 0f);

        var stats = new AoeBaseStats()
        {
            Angle = angle,
            ActivationTime = activationTime,
            Callbacks = new AoeBaseCallbacks { OnActivation = OnActivation },
            Radius = radius,
            AngleOffset = angleOffset,
            Owner = Caller,
            AbilityGUID = GUID,
        };
        ApplyAoeConfig(stats, false);
        GlobalAbilitySpawner.SpawnAoe(stats);

        var stats1 = new AoeBaseStats()
        {
            Angle = angle,
            ActivationTime = activationTime,
            Callbacks = new AoeBaseCallbacks { OnActivation = OnActivation },
            Radius = radius,
            AngleOffset = angleOffset + 90,
            Owner = Caller,
            AbilityGUID = GUID,
        };
        ApplyAoeConfig(stats1, false);
        GlobalAbilitySpawner.SpawnAoe(stats1);

        var stats2 = new AoeBaseStats()
        {
            Angle = angle,
            ActivationTime = activationTime,
            Callbacks = new AoeBaseCallbacks { OnActivation = OnActivation },
            Radius = radius,
            AngleOffset = angleOffset + 180,
            Owner = Caller,
            AbilityGUID = GUID,
        };
        ApplyAoeConfig(stats2, false);
        GlobalAbilitySpawner.SpawnAoe(stats2);

        var stats3 = new AoeBaseStats()
        {
            Angle = angle,
            ActivationTime = activationTime,
            Callbacks = new AoeBaseCallbacks { OnActivation = OnActivation },
            Radius = radius,
            AngleOffset = angleOffset + 270,
            Owner = Caller,
            AbilityGUID = GUID,
        };
        ApplyAoeConfig(stats3, false);
        GlobalAbilitySpawner.SpawnAoe(stats3);
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
        }
    }

    protected override void ApplyUpdate1()
    {
    }

    protected override void ApplyUpdate2()
    {
    }
}
