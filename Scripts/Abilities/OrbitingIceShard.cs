using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class OrbitingIceShard : Ability
{
    private List<Projectile> projectiles = new ();
    private int maxProjectiles = 3;
    private OrbitingIceShardHitModifier hitModifier;
    private float baseStunDuration = 5;
    
    public OrbitingIceShard(PlayerCharacter creator) : base(AbilityIds.OrbitingIceShard, creator)
    {
        this.DisplayName = "Orbiting Ice Shard";
        this.Description = "";
        this.IconPath = "res://Sprites/SkillIcons/Snow/16_Ice_Ball.png";
        this.MaxStack = 1;
        this.BaseCooldown = 5;
        this.BaseDamage = 1;
        this.BaseType = DamageType.Ice;
        this.AutoCast = true;
        hitModifier = new OrbitingIceShardHitModifier();
        _hitModifiers.Add(hitModifier);
        
        if (creator != null)
        {
            creator.NewRoundStarted += CreatorOnNewRoundStarted;
        }
    }

    private void CreatorOnNewRoundStarted(object sender, EventArgs e)
    {
        CurrentStack = 0;
        CurrentCooldown = BaseCooldown;
    }

    protected override bool preventAutoCast()
    {
        
        projectiles.RemoveAll(x => !GodotObject.IsInstanceValid(x));
        return projectiles.Count >= maxProjectiles;
    }

    public override void InternalUse()
    {
        var newAngle = Projectile.EvenDistributedAngle(projectiles, maxProjectiles, 2);
        var color = ColorPlate.GetColor((int)ColorPlateName.LightBlue);
        hitModifier.StunDuration = baseStunDuration;
        var projectile_stats = new ProjectileStats()
        {
            Caller = Caller,
            Direction = Vector2.Zero,
            Speed = 1,
            TimeToBeALive = -1,
            SpritePath = "res://Sprites/Projectiles/fireBallProjectile.png",
            BouncingCount = 0,
            PiercingCount = 0,
            Scale = new Vector2(0.33f, 0.33f),
            Color = new Vector3(color.R, color.G, color.B),
            MovementMode = MovementMode.Orbit,
            Distance = 100,
            CustomCollisionMask = 4,
            AngleOffset = Mathf.DegToRad((float)newAngle),
        };
        var projectileManager = Caller.GetTree().Root.GetNode<ProjectileManager>("/root/Main/Game/ProjectileManager");

        var damage = new Damage { DamageNumber = (float)BaseDamage, Type = BaseType, AilmentChange = BaseAilmentChance };
        projectile_stats.Damage = damage;
        projectile_stats.StartPosition = Caller.GetProjectileStartPosition();
        projectile_stats.Caller = Caller;
        projectileManager.CreateProjectile(projectile_stats, this);
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

    public override void RegisterSpawnedNode(Node node)
    {
        if (node is Projectile projectile)
        {
            projectile.OnDestroyed += ProjectileOnOnDestroyed;
            projectiles.Add(projectile);
        }
    }

    private void ProjectileOnOnDestroyed(Vector2 position)
    {
        projectiles.RemoveAll(x => !GodotObject.IsInstanceValid(x));
    }
}

public class OrbitingIceShardHitModifier : IHitModifier
{
    public float StunDuration { get; set; } = 5;
    
    public void ApplyBefore(HitContext ctx)
    {
        return;
    }

    public void ApplyAfter(HitContext ctx)
    {
        if (ctx.Target.BuffManagerComponent.CountBuff(typeof(Frost)) > 5)
        {
            ctx.Target.BuffManagerComponent.ConsumeBuff(typeof(Frost));
            ctx.Target.MoveController.ApplyStun(StunDuration);
        }
    }
}