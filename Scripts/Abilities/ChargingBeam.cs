using System.Collections.Generic;
using System.Xml;
using CardBase.Scripts;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.Abilities.TriggerStrategy;
using Godot;

namespace CardBase.Scripts.Abilities;

public class ChargingBeam : Ability
{
    private Ray _ray;
    private float deltaSum = 0;
    private ShockDebuff shockDebuff;
    private float aoeBaseDamage = 20;
    
    public ChargingBeam(IEntityComponent creator) : base(AbilityIds.ChargingBeamGuid, creator)
    {
        TriggerStrategy = new PressAndReleaseStrategy();
        this.DisplayName = "Charging Beam";
        this.Description = "Charging Beam";
        this.IconPath = "res://Sprites/SkillIcons/Snow/11_Ice_Ray.png";
        this.BaseCooldown = 10;
        this.BaseDamage = 0.5f;
        this.BaseType = DamageType.Lightning;
    }

    public override void RoundReset()
    {
        return;
    }

    public override void InternalUse()
    {
        var rayStats = new RayStats()
        {
            Caster = Caller,
            Range = 400,
            CollisionTick = onHit,
            AnimationResource = "res://AnimationRes/OrangeBeam.tres",
            CenterLoopCount = 8,
            CenterLoopFolder = "res://Sprites/Projectiles/laser_beam_A_large_orange/center_loop",
            PierceCount = 1,
        };
        
        _ray = GlobalAbilitySpawner.SpawnRay(rayStats);
    }

    private void onHit(IEntityComponent entityComponent, float delta)
    {
        if (entityComponent.TryGetComponent<BuffManagerComponent>(out var buffManagerComponent) && entityComponent.TryGetComponent(out DamageAbleComponent dac))
        {
            this.deltaSum += delta;
            if (deltaSum >= 0.5f)
            {
                var shockBuffCount = Mathf.Max(buffManagerComponent.CountBuff(typeof(ShockDebuff)), 1);
                var dmg = new Damage() { AilmentChance = this.BaseAilmentChance, DamageNumber = (float)this.BaseDamage * shockBuffCount * deltaSum, Type = this.BaseType };
                deltaSum = 0;
                
                var dict = new Dictionary<DamageType, Damage>
                {
                    [dmg.Type] = dmg
                };
                
                var ctx = new HitContext()
                {
                    AbilityGuid = GUID,
                    Damages = dict,
                    Source = Caller,
                    Target = entityComponent,
                };
                var hit = new Hit(_ray, ctx);
                
                
                dac.ReceiveHit(hit);
                shockDebuff = new ShockDebuff(ctx.Source, ctx.Target);
                buffManagerComponent.ApplyBuff(shockDebuff);

                if (shockBuffCount % 5 == 0)
                {
                    if (false)
                    {
                        var consumed = buffManagerComponent.ConsumeBuff(typeof(ShockDebuff));
                    }

                    var aoeStats = new AoeBaseStats
                    {
                        Radius = 100,
                        ActivationTime = 0.5f,
                        Duration = 0,
                        IsStationary = true,
                        StationaryPosition = ((Node2D)ctx.Target).GlobalPosition,
                        Callbacks = new AoeBaseCallbacks {
                            OnActivation = onAoeActivation,
                            OnDeactivation = null,
                            OnTick = null,
                        },
                        Owner = Caller,
                    };
                    GlobalAbilitySpawner.SpawnAoe(aoeStats);
                }
            }
        
        }
    }

    private void onAoeActivation(List<IEntityComponent> obj, AoeBase aoeBase)
    {
        var dict = new Dictionary<DamageType, Damage>();
        var damage = new Damage()
        {
            AilmentChance = BaseAilmentChance,
            Type = BaseType,
            DamageNumber = aoeBaseDamage,
        };
        dict.Add(BaseType, damage);
        
        foreach (var entity in obj)
        {
            if (Caller is not ITeamAffiliation callerTeam || entity is not ITeamAffiliation targetTeam)
            {
                continue;
            }

            if (targetTeam.TeamId != callerTeam.TeamId)
            {
                var hitContext = new HitContext
                {
                    Source = Caller,
                    Target = entity,
                    AbilityGuid = GUID,
                    Damages = dict
                };
                var hit = new Hit(aoeBase, hitContext);
                if (entity.TryGetComponent(out DamageAbleComponent dac))
                {
                    dac.ReceiveHit(hit);
                }
            }
        }
    }

    protected override void InternalCancel()
    {
        if (_ray == null) return;
        _ray.Destroy();
        _ray = null;
    }

    protected override void InternalUpdate()
    {
    }
}
