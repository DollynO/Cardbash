using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class PoisonJab : Ability
{
    public PoisonJab(PlayerCharacter creator) : base(AbilityIds.PoisonJabGuid, creator)
    {
        this.DisplayName = "Poison Jab";
        this.Description = "Well poison jab";
        this.IconPath = "res://Sprites/SkillIcons/Poison/20_Poison_Bone.png";
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
