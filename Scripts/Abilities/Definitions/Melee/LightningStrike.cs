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
        this.BaseCooldown = 10;
        this.BaseDamage = 20;
        this.BaseType = DamageType.Lightning;
    }

    public override void RoundReset()
    {
        return;
    }

    public override void InternalUse()
    {
        var stats = new AoeBaseStats()
        {
            Angle = 15,
            ActivationTime = 0.3f,
            Radius = 150,
            Owner = Caller,
            AbilityGUID = GUID,
            Callbacks = new AoeBaseCallbacks { OnActivation = OnActivation },
        };
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

    protected override void InternalUpdate()
    {
        if (UpdateCounter == 1)
        {
            this._hitModifiers.Add(new ResetCooldownHitModifier());
        }

        if (UpdateCounter == 2)
        {
            this.BaseCooldown = 5;
        }
    }
}