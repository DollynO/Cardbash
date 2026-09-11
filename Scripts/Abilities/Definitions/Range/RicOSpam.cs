using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class RicOSpam : ProjectileAbility
{
    private Dictionary<string, List<Projectile>> projectileLists = new();
    private Random rnd = new();
    private string currentCastGuid = string.Empty;
    private bool speedUpClonedProjectiles;

    public RicOSpam(PlayerCharacter creator) : base(AbilityIds.RicOSpamGuid, creator)
    {
        this.DisplayName = "Ric-O-Spam";
        this.Description = "SPAAAM. Upgrade 1: increases maximum stacks. Upgrade 2: cloned projectiles gain 20% speed per generation, up to 3x.";
        this.IconPath = "res://Sprites/SkillIcons/Snow/8_Ice_Arrow.png";
        this.SpawnDelay = 0.5f;
    }

    protected override ProjectileRuntime GetProjectileRuntime()
    {
        return CreateProjectileRuntime(OnHit);
    }

    protected override ProjectileSpawnRequest GetProjectileSpawnRequest()
    {
        var spreadDegrees = Math.Max(1, ConfigParam("spreadDegrees", 15));
        var offset = rnd.NextInt64(-spreadDegrees, spreadDegrees);
        var request = AimedProjectile(
            "res://AnimationRes/Projectile/RicOSpam/P_RicOSpam.tres",
            ConfigParam("projectileSpeed", 500f),
            ConfigParam("projectileLifetime", 10f));
        ApplyProjectileConfig(request);
        request.CastGuid = currentCastGuid;
        request.Collision.AllowCallerCollision = true;
        request.Movement.AngleOffset = ConfigParam("angleOffset", 0f) + offset * Mathf.Pi / 180;
        return request;
    }

    private void OnHit(IEntityComponent arg1, Projectile arg2)
    {
        if (ReferenceEquals(arg1, Caller))
        {
            ApplySelfDamage();
            return;
        }

        if (arg1.TryGetComponent(out DamageAbleComponent dac))
        {
            var damageDict = new Dictionary<DamageType, Damage>();
            var dmg = new Damage
            {
                AilmentChance = BaseAilmentChance,
                DamageNumber = (float)BaseDamage,
                Type = BaseType
            };
            damageDict.Add(BaseType, dmg);
            var ctx = new HitContext
            {
                Target = arg1,
                Source = Caller,
                AbilityGuid = GUID,
                Damages = damageDict
            };
            var hit = new Hit(arg2, ctx);
            dac.ReceiveHit(hit);
        }
    }


    public override void RoundReset()
    {
        return;
    }


    protected override void ApplyUpdate1()
    {
        this.MaxStack += Math.Max(1, ConfigParam("stackIncrease", 1));
    }

    protected override void ApplyUpdate2()
    {
        speedUpClonedProjectiles = true;
    }

    protected override void _onProjectileCollided(Vector2 position, Projectile projectile)
    {
        if (projectileLists.TryGetValue(projectile.SpawnRequest.CastGuid, out var _projectiles))
        {
            if (_projectiles.Count <= ConfigParam("maxSplitProjectiles", 10))
            {
                var request = GetProjectileSpawnRequest();
                request.CastGuid = projectile.SpawnRequest.CastGuid;
                request.StartPosition = position;
                var spreadDegrees = Math.Max(1, ConfigParam("spreadDegrees", 15));
                var offset = rnd.NextInt64(-spreadDegrees, spreadDegrees);
                request.Movement.Direction = projectile.SpawnRequest.Movement.Direction.Rotated(Mathf.DegToRad(offset));
                request.CloneGeneration = projectile.SpawnRequest.CloneGeneration + 1;
                request.Movement.Speed = GetClonedProjectileSpeed(request.Movement.Speed, request.CloneGeneration);
                request.Movement.AngleOffset = 0;
                request.Caller = Caller;
                var add_proj = GlobalAbilitySpawner.SpawnProjectile(request, GetProjectileRuntime());
                add_proj.OnCollision += _onProjectileCollided;
                add_proj.OnPiercing += _onProjectilePierced;
                add_proj.OnDestroyed += _onProjectileDestroyed;
                _projectiles.Add(add_proj);
            }
        }
    }

    private float GetClonedProjectileSpeed(float baseSpeed, int cloneGeneration)
    {
        if (!speedUpClonedProjectiles)
        {
            return baseSpeed;
        }

        var cloneSpeedMultiplier = ConfigParam("cloneSpeedMultiplier", 1.2f);
        var maxCloneSpeedMultiplier = ConfigParam("maxCloneSpeedMultiplier", 3f);
        var maxCloneSpeedGeneration = Math.Max(1, ConfigParam("maxCloneSpeedGeneration", 6));
        var speedMultiplier = cloneGeneration >= maxCloneSpeedGeneration
            ? maxCloneSpeedMultiplier
            : (float)Math.Pow(cloneSpeedMultiplier, cloneGeneration);

        return baseSpeed * Mathf.Min(speedMultiplier, maxCloneSpeedMultiplier);
    }

    private void deleteProjectileList(List<Projectile> projectiles)
    {
        foreach (var projectile in projectiles.ToList())
        {
            projectile.OnCollision -= _onProjectileCollided;
            projectile.OnPiercing -= _onProjectilePierced;
            projectile.OnDestroyed -= _onProjectileDestroyed;
            projectile.DestroyProjectile();
        }

        projectiles.Clear();
    }
    
    protected override void _onProjectileDestroyed(Vector2 position, Projectile projectile)
    {
        if (projectileLists.TryGetValue(projectile.SpawnRequest.CastGuid, out var _projectiles))
        {
            if (_projectiles.Contains(projectile))
            {
                _projectiles.Remove(projectile);
            }

            if (_projectiles.Count == 0)
            {
                projectileLists.Remove(projectile.SpawnRequest.CastGuid);
            }
        }
    }

    protected override bool PreSpawnProjectile()
    {
        var maxActiveCasts = Math.Max(1, ConfigParam("maxActiveCasts", 3));
        while (projectileLists.Count >= maxActiveCasts)
        {
            var kvp = projectileLists.First();
            deleteProjectileList(kvp.Value);
            projectileLists.Remove(kvp.Key);
        }
        currentCastGuid = Guid.NewGuid().ToString();
        projectileLists.Add(currentCastGuid, new List<Projectile>());
        return true;
    }

    protected override void PostSpawnProjectile(Projectile projectile)
    {
        if (projectileLists.TryGetValue(projectile.SpawnRequest.CastGuid, out var _projectiles))
        {
            _projectiles.Add(projectile);
        }
    }

    private void ApplySelfDamage()
    {
        var selfDamage = ConfigParam("selfDamage", 5f);
        if (selfDamage <= 0f || !Caller.TryGetComponent(out HealthComponent healthComponent))
        {
            return;
        }

        healthComponent.ApplyDamage(new Damage
        {
            AilmentChance = 0f,
            DamageNumber = selfDamage,
            Type = DamageType.Physical,
        }, Caller);
    }

    protected override void InternalCancel()
    {
        foreach (var kvp in projectileLists)
        {
            deleteProjectileList(kvp.Value);
        }
        
        projectileLists.Clear();
    }
}
