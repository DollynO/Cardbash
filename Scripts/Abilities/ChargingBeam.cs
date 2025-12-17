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
            InnerColor = ColorPlate.GetColor((int)ColorPlateName.LightBlue),
            OuterColor = ColorPlate.GetColor((int)ColorPlateName.DarkBlue),
            Range = 400,
            CollisionTick = onHit,
        };
        
        _ray = (globalAbilitySpawner ??= this.Caller.GetTree().Root.GetNode<GlobalAbilitySpawner>("/root/Main/Game/GlobalAbilitySpawner"))
            .SpawnRay(rayStats);
    }

    private void onHit(IHitableObject obj, float delta)
    {
        this.deltaSum += delta;
        if (deltaSum >= 0.5f)
        {
            var target = (PlayerCharacter)obj;
            var shockBuffCount = Mathf.Max(target.BuffManagerComponent.CountBuff(typeof(ShockDebuff)), 1);
            var dmg = new Damage() { AilmentChange = this.BaseAilmentChance, DamageNumber = (float)this.BaseDamage * shockBuffCount * deltaSum, Type = this.BaseType };
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
            
            obj.ApplyDamage(ctx);
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
                    OnActivation = null,
                    OnDeactivation = null,
                    OnTick = null,
                    Owner = Caller,
                };
                globalAbilitySpawner.SpawnAoe(aoeStats);
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

    public override void RegisterSpawnedNode(Node node)
    {
        return;
    }
}