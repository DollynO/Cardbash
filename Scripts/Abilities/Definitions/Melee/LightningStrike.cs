using System.Collections.Generic;
using CardBase.Scripts.Abilities.HitMods;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class LightningStrike : Ability
{
    private bool chainStrikeEnabled;
    private ChainLightning chainLightning;

    public LightningStrike(PlayerCharacter creator) : base(AbilityIds.LightningStrikeGuid, creator)
    {
        this.DisplayName = "Lightning Strike";
        this.Description = "Fast lightning strike. Upgrade 2: lightning jumps to a nearby enemy after hitting.";
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
            CanAffectOwner = false,
            Callbacks = new AoeBaseCallbacks { OnActivation = OnActivation },
        };
        ApplyAoeConfig(stats);
        GlobalAbilitySpawner.SpawnAoe(stats);
    }

    private void OnActivation(List<IEntityComponent> arg1, AoeBase arg2)
    {
        var chainExcludedTargets = new HashSet<IEntityComponent>(arg1);

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
                var hitLanded = dac.ReceiveHit(hit);
                if (hitLanded && chainStrikeEnabled)
                {
                    foreach (var chainedTarget in GetChainLightning().StrikeFrom(playerCharacter, chainExcludedTargets))
                    {
                        chainExcludedTargets.Add(chainedTarget);
                    }
                }
            }
        }
    }

    
    protected override void ApplyUpdate1()
    {
        this._hitModifiers.Add(new ResetCooldownHitModifier());
    }

    protected override void ApplyUpdate2()
    {
        chainStrikeEnabled = true;
    }

    private ChainLightning GetChainLightning()
    {
        if (chainLightning != null)
        {
            return chainLightning;
        }

        var gameManager = ((Node)Caller).GetTree().Root.GetNode<GameManager>("/root/Main/Game");
        chainLightning = new ChainLightning(Caller, gameManager, GlobalAbilitySpawner, GUID, new ChainLightningConfig
        {
            Range = ConfigParam("chainRange", 180f),
            MaxJumps = ConfigParam("chainJumps", 1),
            Damage = (float)BaseDamage,
            DamageMultiplierPerJump = ConfigParam("chainDamageMultiplier", 0.5f),
            DamageType = BaseType,
            AilmentChance = BaseAilmentChance,
            ArcDuration = ConfigParam("chainArcDuration", 0.18f),
            ArcWidth = ConfigParam("chainArcWidth", 5f),
        });

        return chainLightning;
    }
}
