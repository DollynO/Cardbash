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
        this.BaseCooldown = 1;
        this.BaseDamage = 50;
        this.BaseType = DamageType.Holy;
    }

    public override void RoundReset()
    {
        return;
    }

    public override void InternalUse()
    {
        var stats = new AoeBaseStats()
        {
            Angle = 45,
            ActivationTime = 0.8f,
            OnActivation = OnActivation,
            Radius = 60,
            AngleOffset = 0,
            Owner = Caller,
            AbilityGUID = GUID,
        };
        for (var i = 0; i < 4; i++)
        {
            stats.AngleOffset = 0 + i * 90;
            GlobalAbilitySpawner.SpawnAoe(stats);
        }
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
        
    }
}