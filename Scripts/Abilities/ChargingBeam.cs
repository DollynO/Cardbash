using System.Collections.Generic;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.Abilities.TriggerStrategy;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class ChargingBeam : Ability
{
    private Ray _ray;
    private GlobalAbilitySpawner globalAbilitySpawner;
    private float deltaSum = 0;
    private ShockDebuff shockDebuff;
    private float aoeBaseDamage = 20;
    
    public ChargingBeam(PlayerCharacter creator) : base(AbilityIds.ChargingBeamGuid, creator)
    {
        TriggerStrategy = new PressAndReleaseStrategy();
        this.DisplayName = "Charging Beam";
        this.Description = "Charging Beam";
        this.IconPath = "res://Sprites/SkillIcons/Snow/11_Ice_Ray.png";
        this.BaseCooldown = 10;
        this.BaseDamage = 0.5f;
        this.BaseType = DamageType.Lightning;
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
        
        _ray = globalAbilitySpawner.SpawnRay(rayStats);
    }

    private void onHit(IHitableObject obj, float delta)
    {
        this.deltaSum += delta;
        if (deltaSum >= 0.5f)
        {
            var target = (PlayerCharacter)obj;
            var shockBuffCount = Mathf.Max(target.BuffManagerComponent.CountBuff(typeof(ShockDebuff)), 1);
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
                Target = (PlayerCharacter)obj,
            };
            var hit = new Hit(_ray, ctx);
            
            obj.ReceiveHit(hit);
            shockDebuff = new ShockDebuff(ctx.Source, ctx.Target);
            ctx.Target.BuffManagerComponent.ApplyBuff(shockDebuff);

            if (shockBuffCount % 5 == 0)
            {
                if (false)
                {
                    var consumed = target.BuffManagerComponent.ConsumeBuff(typeof(ShockDebuff));
                }

                var aoeStats = new AoeBaseStats
                {
                    Radius = 100,
                    ActivationTime = 0.5f,
                    Duration = 0,
                    IsStationary = true,
                    StationaryPosition = ctx.Target.GlobalPosition,
                    OnActivation = onAoeActivation,
                    OnDeactivation = null,
                    OnTick = null,
                    Owner = Caller,
                };
                globalAbilitySpawner.SpawnAoe(aoeStats);
            }
        }
    }

    private void onAoeActivation(List<PlayerCharacter> obj, AoeBase aoeBase)
    {
        var dict = new Dictionary<DamageType, Damage>();
        var damage = new Damage()
        {
            AilmentChance = BaseAilmentChance,
            Type = BaseType,
            DamageNumber = aoeBaseDamage,
        };
        dict.Add(BaseType, damage);
        
        foreach (var playerCharacter in obj)
        {
            if (playerCharacter.TeamId != Caller.TeamId)
            {
                var hitContext = new HitContext
                {
                    Source = Caller,
                    Target = playerCharacter,
                    AbilityGuid = GUID,
                    Damages = dict
                };
                var hit = new Hit(aoeBase,  hitContext);
                playerCharacter.ReceiveHit(hit);
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