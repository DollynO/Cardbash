using System.Collections.Generic;
using CardBase.Scripts;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.Abilities.TriggerStrategy;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class ChargingBeam : Ability
{
    private Ray _ray;
    private float deltaSum = 0;
    private float aoeBaseDamage = 20;
    private readonly string castSlowModifierSourceId = System.Guid.NewGuid().ToString("N");
    private bool _isBeamActive;
    private bool _cooldownRunning;
    private float _beamActiveTime;

    protected override bool UsesStandardCooldown => false;

    public ChargingBeam(IEntityComponent creator) : base(AbilityIds.ChargingBeamGuid, creator)
    {
        TriggerStrategy = new BeamTriggerStrategy();
        this.DisplayName = "Charging Beam";
        this.Description = "Charging Beam.";
        this.IconPath = "res://Sprites/SkillIcons/Snow/11_Ice_Ray.png";
    }

    public override void RoundReset()
    {
        CancelAbility();
    }

    public override void ProcessAbility(float delta)
    {
        if (!_isBeamActive)
        {
            return;
        }

        _beamActiveTime += delta;
        var maxDuration = ConfigParam("maxDuration", 3f);
        if (maxDuration > 0f && _beamActiveTime >= maxDuration)
        {
            ReleaseBeam();
        }
    }

    protected override void ProcessPassive(double delta)
    {
        if (_isBeamActive || CurrentStack >= MaxStack)
        {
            return;
        }

        if (!_cooldownRunning)
        {
            StartCooldown();
        }

        CurrentCooldown -= delta;
        if (CurrentCooldown > 0)
        {
            return;
        }

        CurrentStack++;
        if (CurrentStack < MaxStack)
        {
            StartCooldown();
            return;
        }

        _cooldownRunning = false;
        CurrentCooldown = GetCooldownDuration();
    }

    public override void InternalUse()
    {
        if (_isBeamActive)
        {
            return;
        }

        _isBeamActive = true;
        _beamActiveTime = 0f;
        _cooldownRunning = false;
        CurrentCooldown = 0;
        ApplyCastSlow();

        var rayStats = new RayStats()
        {
            Caster = Caller,
            Range = ConfigParam("rayRange", 400f),
            CollisionTick = onHit,
            AnimationResource = "res://AnimationRes/OrangeBeam.tres",
            CenterLoopCount = ConfigParam("rayCenterLoopCount", 8),
            CenterLoopFolder = "res://Sprites/Projectiles/laser_beam_A_large_orange/center_loop",
            PierceCount = ConfigParam("rayPierceCount", 1),
        };
        ApplyRayConfig(rayStats);

        var spawnData = new SpawnData()
        {
            SpawnType = SpawnType.RAY,
            SpawnObjectData = rayStats.ToDict()
        };

        _ray = (Ray)GlobalAbilitySpawner.Spawn(spawnData);
        _ray?.SetCollisionTick(onHit);
    }

    public void ReleaseBeam()
    {
        if (!_isBeamActive)
        {
            return;
        }

        CancelAbility();
        if (CurrentStack < MaxStack)
        {
            StartCooldown();
        }
    }

    private void onHit(IEntityComponent entityComponent, float delta)
    {
        if (entityComponent.TryGetComponent<BuffManagerComponent>(out var buffManagerComponent) && entityComponent.TryGetComponent(out DamageAbleComponent dac))
        {
            this.deltaSum += delta;
            if (deltaSum >= ConfigParam("damageTickInterval", 0.5f))
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
                buffManagerComponent.ApplyBuff(new ShockDebuff(ctx.Source, ctx.Target));

                if (shockBuffCount % Mathf.Max(1, ConfigParam("shockAoeStackInterval", 5)) == 0)
                {
                    var aoeStats = new AoeBaseStats
                    {
                        Radius = ConfigParam("shockAoeRadius", 100f),
                        ActivationTime = ConfigParam("shockAoeActivationTime", 0.5f),
                        Duration = ConfigParam("shockAoeDuration", 0f),
                        IsStationary = true,
                        StationaryPosition = ((Node2D)ctx.Target).GlobalPosition,
                        Callbacks = new AoeBaseCallbacks
                        {
                            OnActivation = onAoeActivation,
                            OnDeactivation = null,
                            OnTick = null,
                        },
                        Owner = Caller,
                    };
                    ApplyAoeDamagePreview(aoeStats);
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
            DamageNumber = ConfigParam("shockAoeDamage", aoeBaseDamage),
        };
        dict.Add(BaseType, damage);

        foreach (var entity in obj)
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

    protected override void InternalCancel()
    {
        _isBeamActive = false;
        _beamActiveTime = 0f;
        RemoveCastSlow();

        if (_ray != null)
        {
            _ray.Destroy();
            _ray = null;
        }
    }

    private void ApplyCastSlow()
    {
        if (Caller.TryGetComponent<StatblockComponent>(out var statblock))
        {
            var slow = ConfigParam("castMoveSlow", 0.5f);
            statblock.RemoveModifierSource(castSlowModifierSourceId);
            statblock.AddModifiers(new[]
            {
                new StatModifier(
                    castSlowModifierSourceId,
                    StatType.MovementSpeed,
                    StatOp.PercentAdd,
                    -slow),
                new StatModifier(
                    castSlowModifierSourceId,
                    StatType.AimRotationSpeed,
                    StatOp.PercentAdd,
                    -slow),
            });
        }
    }

    private void RemoveCastSlow()
    {
        if (Caller.TryGetComponent<StatblockComponent>(out var statblock))
        {
            statblock.RemoveModifierSource(castSlowModifierSourceId);
        }
    }

    protected override void ApplyUpdate1()
    {
    }

    protected override void ApplyUpdate2()
    {
    }

    private void StartCooldown()
    {
        _cooldownRunning = true;
        CurrentCooldown = GetCooldownDuration();
    }

    private class BeamTriggerStrategy : ITriggerStrategy
    {
        public void OnKeyJustPressed(Ability ability)
        {
            if (ability.Activate())
            {
                ability.Use();
            }
        }

        public void OnKeyPressed(Ability ability, double delta)
        {
        }

        public void OnKeyReleased(Ability ability)
        {
            if (ability is ChargingBeam chargingBeam)
            {
                chargingBeam.ReleaseBeam();
            }
        }
    }
}
