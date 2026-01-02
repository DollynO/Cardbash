using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class OrbitingIceShard : Ability
{
    private int maxProjectiles = 3;
    private float baseStunDuration = 5;
    private Ring ring;
    
    public OrbitingIceShard(PlayerCharacter creator) : base(AbilityIds.OrbitingIceShard, creator)
    {
        this.DisplayName = "Orbiting Ice Shard";
        this.Description = "";
        this.IconPath = "res://Sprites/SkillIcons/Snow/16_Ice_Ball.png";
        this.MaxStack = 1;
        this.BaseCooldown = 5;
        this.BaseDamage = 1;
        this.BaseType = DamageType.Ice;
        this.BaseAilmentChance = 1f;
        this.AutoCast = true;
        
        if (creator != null)
        {
            creator.NewRoundStarted += CreatorOnNewRoundStarted;
            ring = creator.RingContainer.AddRing(100, 1);
        }
    }

    private void CreatorOnNewRoundStarted(object sender, EventArgs e)
    {
        CurrentStack = 0;
        CurrentCooldown = BaseCooldown;
    }

    protected override bool preventAutoCast()
    {
        return ring.GetStackCount() >= maxProjectiles;
    }

    public override void InternalUse()
    {
        var color = ColorPlate.GetColor((int)ColorPlateName.LightBlue);
        var projectile_stats = new ProjectileStats()
        {
            Caller = Caller,
            Speed = 0,
            TimeToBeALive = -1,
            AnimationResourcePath = "res://AnimationRes/Projectile/Ice/I_RockLargeBlue.tres"
        };

        projectile_stats.OnHit = OnHit;
        projectile_stats.StartPosition = Caller.GetProjectileStartPosition();
        projectile_stats.Caller = Caller;
        var projectile = globalAbilitySpawner.SpawnProjectile(projectile_stats);
        
        projectile.OnDestroyed += ProjectileOnOnDestroyed;
        ring.AddNode(projectile, true);
    }

    private void OnHit(IHitableObject hitObject, Projectile source)
    {
        var ctx = new HitContext();
        var target = (PlayerCharacter)hitObject;

        ctx.Target = target;
        ctx.Source = Caller;
        ctx.AbilityGuid = GUID;
        var damage = new Damage { DamageNumber = (float)BaseDamage, Type = BaseType, AilmentChance = BaseAilmentChance };
        var damageDict = new Dictionary<DamageType, Damage>()
        {
            { damage.Type, damage }
        };
        ctx.Damages = damageDict;

        var hit = new Hit(source, ctx);
        target.ReceiveHit(hit);
        
        if (ctx.Target.BuffManagerComponent.CountBuff(typeof(Frost)) > 5)
        {
            ctx.Target.BuffManagerComponent.ConsumeBuff(typeof(Frost));
            ctx.Target.MoveController.ApplyStun(baseStunDuration);
        }
    }

    protected override void InternalUpdate()
    {
        switch (UpdateCounter)
        {
            case 1:
                this.BaseAilmentChance = 0.3f;
                return;
            case 2:
                return;
            default:
                return;
        }
    }

    private void ProjectileOnOnDestroyed(Vector2 position, Projectile projectile)
    {
        ring.RemoveNode(projectile, false);
    }
}

public class OrbitingIceShardHitModifier : IHitModifier
{
    
    public void ApplyBefore(HitContext ctx)
    {
        return;
    }

    public void ApplyAfter(HitContext ctx)
    {

    }
}