using System.Collections.Generic;
using CardBase.Scripts.Abilities.HitMods;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class LightningStrike : Ability
{
    public LightningStrike(PlayerCharacter creator) : base(AbilityIds.LightningStrikeGuid, creator)
    {
        this.DisplayName = "Lightning Strike";
        this.Description = "Fast lightning strike";
        this.IconPath = "res://Sprites/SkillIcons/Lightning/3_Electric_Boom.png";
    }

    public override void RoundReset()
    {
        return;
    }

    public override void InternalUse()
    {
        var stats = new AoeBaseStats()
        {
            Angle = ConfigParam("angle", 15f),
            ActivationTime = ConfigParam("activationTime", 0.3f),
            Radius = ConfigParam("radius", 150f),
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
        this._hitModifiers.Add(new ResetCooldownHitModifier());
    }

    protected override void ApplyUpdate2()
    {
        this.BaseCooldown = 5;
    }
}
