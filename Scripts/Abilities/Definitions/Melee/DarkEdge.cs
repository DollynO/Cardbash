using System.Collections.Generic;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.Abilities.HitMods;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class DarkEdge : Ability
{
    public DarkEdge(PlayerCharacter creator) : base(AbilityIds.DarkEdgeGuid, creator)
    {
        this.DisplayName = "Dark Edge";
        this.Description = "Consumes all darkness / active debuffs on the target and deals damage for each consumed.";
        this.IconPath = "res://Sprites/SkillIcons/Dark/10_Dark_Blade.png";
        this._hitModifiers.Add(new ConsumeBuffTypeHitModifier(onBuffConsumed, DamageType.Darkness));
    }

    private void onBuffConsumed(int count, HitContext context)
    {
        context.Damages[DamageType.Darkness].DamageNumber = (float)(count * BaseDamage);
    }

    public override void RoundReset()
    {
        return;
    }

    public override void InternalUse()
    {
        var stats = new AoeBaseStats()
        {
            Angle = ConfigParam("angle", 160f),
            ActivationTime = ConfigParam("activationTime", 0.8f),
            Radius = ConfigParam("radius", 60f),
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
        }
    }

    protected override void ApplyUpdate1()
    {
    }

    protected override void ApplyUpdate2()
    {
    }
}
