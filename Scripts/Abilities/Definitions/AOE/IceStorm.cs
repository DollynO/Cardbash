using System.Collections.Generic;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities.AOE;

public class IceStorm : Ability
{
    private float baseStunDuration = 3;
    private readonly List<AoeBase> activeAoes = new();

    public IceStorm(PlayerCharacter creator) : base(AbilityIds.IceStormGuid, creator)
    {
        this.DisplayName = "IceStorm";
        this.Description = "IceStorm";
        this.IconPath = "res://Sprites/SkillIcons/Snow/10_Frost_Whirlwind.png";
    }


    protected override void ApplyUpdate1()
    {
    }

    protected override void ApplyUpdate2()
    {
    }

    public override void RoundReset()
    {
        CancelRuntimeObjects();
    }

    public override void ClearAbility()
    {
        CancelRuntimeObjects();
    }

    public override void InternalUse()
    {
        Caller.TryGetComponent(out AimComponent aimComponent);

        var stats = new AoeBaseStats()
        {
            ActivationTime = ConfigParam("activationTime", 1f),
            Radius = ConfigParam("radius", 300f),
            Duration = ConfigParam("duration", 5f),
            Owner = Caller,
            AbilityGUID = GUID,
            StationaryPosition = aimComponent.GetPlayerMouesPosition(ConfigParam("range", 500f)),
            IsStationary = true,
            Callbacks = new AoeBaseCallbacks
            {
                OnTick = OnTick,
                OnDeactivation = (_, aoe) => activeAoes.Remove(aoe),
            },
        };
        ApplyAoeConfig(stats);
        var aoe = GlobalAbilitySpawner.SpawnAoe(stats);
        if (aoe != null)
        {
            activeAoes.Add(aoe);
        }

    }

    private void OnTick(IEntityComponent entity, double arg2, AoeBase source)
    {
        var ailmentChance = UpdateCounter >= 1 ? ConfigParam("ailmentChance", 0.5f) : Damage.DEFAULT_AILMENT_CHANGE;
        var damage = new Damage
        {
            DamageNumber = (float)BaseDamage * (float)arg2,
            AilmentChance = ailmentChance,
            Type = BaseType
        };
        var damageDict = new Dictionary<DamageType, Damage> { { damage.Type, damage } };
        var ctx = new HitContext()
        {
            AbilityGuid = GUID,
            Source = Caller,
            Target = entity,
            Damages = damageDict
        };
        var hit = new Hit(source, ctx);
        if (entity.TryGetComponent(out DamageAbleComponent dac))
        {
            dac.ReceiveHit(hit);
        }
        
        if (UpdateCounter >= 2)
        {
            if (entity.TryGetComponent<BuffManagerComponent>(out var buffManagerComponent))
            {
                if (buffManagerComponent.CountBuff(typeof(Frost)) > 5)
                {
                    buffManagerComponent.ConsumeBuff(typeof(Frost));
                    if (entity.TryGetComponent<MoveComponent>(out var moveComponent))
                    {
                        moveComponent.ApplyStun(ConfigParam("stunDuration", baseStunDuration));
                    }
                }
            }
        }
    }

    private void CancelRuntimeObjects()
    {
        foreach (var aoe in new List<AoeBase>(activeAoes))
        {
            if (GodotObject.IsInstanceValid(aoe))
            {
                aoe.Cancel();
            }
        }

        activeAoes.Clear();
    }
    
    private void OnActivation(List<IEntityComponent> playersHit, AoeBase source)
    {
        if (playersHit.Count > 0)
        {
            foreach (var player in playersHit)
            {
                
            }
        }
    }
}
