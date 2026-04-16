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

    protected override ProjectileRuntime GetProjectileRuntime()
    {
        return CreateProjectileRuntime(OnHit);
    }

    protected override ProjectileSpawnRequest GetProjectileSpawnRequest()
    {
        var offset = rnd.NextInt64(-15, 15);
        var request = AimedProjectile("res://AnimationRes/Projectile/RicOSpam/P_RicOSpam.tres", 500, 10);
        request.CastGuid = currentCcastGuid;
        request.Movement.AngleOffset = offset * Mathf.Pi / 180;
        request.Movement.BounceCount = 2;
        request.Visual.Scale = new Vector2(0.5f, 0.5f);
        return request;
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


    public override void RoundReset()
    {
        return;
    }

    protected override void InternalUpdate()
    {
        
    }

    protected override void _onProjectileCollided(Vector2 position, Projectile projectile)
    {
        if (projectileLists.TryGetValue(projectile.SpawnRequest.CastGuid, out var _projectiles))
        {
            if (_projectiles.Count <= 10)
            {
                var request = GetProjectileSpawnRequest();
                request.CastGuid = projectile.SpawnRequest.CastGuid;
                request.StartPosition = position;
                var offset = rnd.NextInt64(-15, 15);
                request.Movement.Direction = projectile.SpawnRequest.Movement.Direction.Rotated(Mathf.DegToRad(offset));
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

    protected override void PreSpawnProjectile()
    {
        currentCcastGuid = Guid.NewGuid().ToString();
        projectileLists.Add(currentCcastGuid, new List<Projectile>());
    }

    protected override void PostSpawnProjectile(Projectile projectile)
    {
        if (projectileLists.TryGetValue(projectile.SpawnRequest.CastGuid, out var _projectiles))
        {
            _projectiles.Add(projectile);
        }
    }
}
