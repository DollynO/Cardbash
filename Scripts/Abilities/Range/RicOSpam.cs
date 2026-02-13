using System;
using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class RicOSpam: ProjectileAbility
{
    private Dictionary<string, List<Projectile>> projectileLists = new();
    private Random rnd = new();
    private string currentCcastGuid = string.Empty;
    
    public RicOSpam(PlayerCharacter creator) : base(AbilityIds.RicOSpamGuid, creator)
    {
        this.DisplayName = "Ric-O-Spam";
        this.Description = "SPAAAM";
        this.IconPath = "res://Sprites/SkillIcons/Snow/8_Ice_Arrow.png";
        this.BaseCooldown = 5;
        this.BaseDamage = 5;
        this.BaseType = DamageType.Physical;
        this.SpawnDelay = 0.5f;
    }

    protected override ProjectileStats GetProjectileStats()
    {
        var offset = rnd.NextInt64(-15, 15);
        var direction = Vector2.Zero;
        if (Caller.TryGetComponent(out AimComponent aimComponent))
        {
            direction = aimComponent.GetLookAtDirection();
        }
        return new ProjectileStats
        {
            CastGuid = currentCcastGuid,
            Caller = Caller,
            Direction = direction,
            AngleOffset = offset * Mathf.Pi / 180 ,
            Speed = 500,
            TimeToBeALive = 10,
            AnimationResourcePath = "res://AnimationRes/Projectile/RicOSpam/P_RicOSpam.tres",
            Scale = new Vector2(0.5f, 0.5f),
            BouncingCount = 2,
            PiercingCount = 0,
            OnHit = OnHit,
        };
    }

    private void OnHit(IEntityComponent arg1, Projectile arg2)
    {
        if (arg1.TryGetComponent(out DamageAbleComponent dac))
        {
            var damageDict = new Dictionary<DamageType, Damage>();
            var dmg = new Damage
            {
                AilmentChance = BaseAilmentChance,
                DamageNumber = (float)BaseDamage,
                Type =  BaseType
            };
            damageDict.Add(BaseType, dmg);
            var ctx = new HitContext
            {
                Target = (PlayerCharacter)arg1,
                Source = Caller,
                AbilityGuid = GUID,
                Damages = damageDict
            };
            var hit = new Hit(arg2, ctx);
            dac.ReceiveHit(hit);
        }
    }


    protected override void InternalUpdate()
    {
        
    }

    protected override void _onProjectileCollided(Vector2 position, Projectile projectile)
    {
        if (projectileLists.TryGetValue(projectile.Stats.CastGuid, out var _projectiles))
        {
            if (_projectiles.Count <= 10)
            {
                var stats = GetProjectileStats();
                stats.CastGuid = projectile.Stats.CastGuid;
                stats.StartPosition = position;
                var offset = rnd.NextInt64(-15, 15);
                stats.Direction = projectile.Stats.Direction.Rotated(Mathf.DegToRad(offset));
                stats.Caller = Caller;
                var add_proj = GlobalAbilitySpawner.SpawnProjectile(stats);
                add_proj.OnCollision += _onProjectileCollided;
                add_proj.OnPiercing += _onProjectilePierced;
                add_proj.OnDestroyed += _onProjectileDestroyed;
                _projectiles.Add(add_proj);
            }
        }
    }

    protected override void _onProjectileDestroyed(Vector2 position, Projectile projectile)
    {
        if (projectileLists.TryGetValue(projectile.Stats.CastGuid, out var _projectiles))
        {
            if (_projectiles.Contains(projectile))
            {
                _projectiles.Remove(projectile);
            }

            if (_projectiles.Count == 0)
            {
                projectileLists.Remove(projectile.Stats.CastGuid);
            }
        }
    }

    protected override void PreSpawnProjectile()
    {
        currentCcastGuid = Guid.NewGuid().ToString();
        projectileLists.Add(currentCcastGuid, new List<Projectile>());
    }

    protected override void PostSpawnProjectile(Projectile projectile)
    {
        if (projectileLists.TryGetValue(projectile.Stats.CastGuid, out var _projectiles))
        {
            _projectiles.Add(projectile);
        }
    }
}