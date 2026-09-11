using System.Collections.Generic;
using CardBase.Scripts.Abilities.HitMods;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class ShatterStrike : Ability
{
    public ShatterStrike(PlayerCharacter creator) : base(AbilityIds.ShatterStrikeGuid, creator)
    {
        this.DisplayName = "Shatter Strike";
        this.Description = "A brutal melee blow that consumes all stacks of Frost on the target, detonating the icy buildup. The chill explodes into shards, stunning the enemy briefly while dealing heavy cold-infused damage.";
        this.IconPath = "res://Sprites/SkillIcons/Snow/18_Ice_Sword.png";

        this._hitModifiers.Add(new FrostShatterHitModifier());
    }

    public override void RoundReset()
    {
        return;
    }

    public override void InternalUse()
    {
        var aoeStats = new AoeBaseStats()
        {
            Angle = ConfigParam("angle", 45f),
            ActivationTime = ConfigParam("activationTime", 0.5f),
            Callbacks = new AoeBaseCallbacks
            {
                OnActivation = OnActivation,
            },
            Radius = ConfigParam("radius", 150f),
            AngleOffset = ConfigParam("angleOffset", 0f),
            AbilityGUID = GUID,
            Owner = Caller,
            CanAffectOwner = false
        };
        ApplyAoeConfig(aoeStats);
        GlobalAbilitySpawner.SpawnAoe(aoeStats);
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
