using System;
using System.Collections.Generic;
using Godot;
using GodotDictionary = Godot.Collections.Dictionary<string, Godot.Variant>;

namespace CardBase.Scripts.Abilities;

public class StationaryTowerMinionConfig
{
    public int BulletCount { get; set; } = 3;
    public float AttackRadius { get; set; } = 220f;
    public float AttackInterval { get; set; } = 0.5f;
    public float ProjectileSpeed { get; set; } = 450f;
    public float ProjectileLifetime { get; set; } = 3f;
    public float ProjectileDamage { get; set; } = 10f;
    public DamageType ProjectileDamageType { get; set; } = DamageType.Physical;
    public float ProjectileAilmentChance { get; set; }
    public string ProjectileAnimationPath { get; set; } = "res://AnimationRes/Projectile/MagicMissile/mm_lrage_blue.tres";
    public Vector2 ProjectileAnimationOffset { get; set; } = Vector2.Zero;
    public Vector2 ProjectileScale { get; set; } = Vector2.One;
    public float ProjectileHealth { get; set; } = 1f;

    public GodotDictionary ToDict()
    {
        return new GodotDictionary
        {
            { nameof(BulletCount), BulletCount },
            { nameof(AttackRadius), AttackRadius },
            { nameof(AttackInterval), AttackInterval },
            { nameof(ProjectileSpeed), ProjectileSpeed },
            { nameof(ProjectileLifetime), ProjectileLifetime },
            { nameof(ProjectileDamage), ProjectileDamage },
            { nameof(ProjectileDamageType), (int)ProjectileDamageType },
            { nameof(ProjectileAilmentChance), ProjectileAilmentChance },
            { nameof(ProjectileAnimationPath), ProjectileAnimationPath ?? string.Empty },
            { nameof(ProjectileAnimationOffset), ProjectileAnimationOffset },
            { nameof(ProjectileScale), ProjectileScale },
            { nameof(ProjectileHealth), ProjectileHealth },
        };
    }

    public static StationaryTowerMinionConfig FromDict(GodotDictionary dict)
    {
        var config = new StationaryTowerMinionConfig();
        if (dict == null)
        {
            return config;
        }

        config.BulletCount = GetInt(dict, nameof(BulletCount), config.BulletCount);
        config.AttackRadius = GetFloat(dict, nameof(AttackRadius), config.AttackRadius);
        config.AttackInterval = GetFloat(dict, nameof(AttackInterval), config.AttackInterval);
        config.ProjectileSpeed = GetFloat(dict, nameof(ProjectileSpeed), config.ProjectileSpeed);
        config.ProjectileLifetime = GetFloat(dict, nameof(ProjectileLifetime), config.ProjectileLifetime);
        config.ProjectileDamage = GetFloat(dict, nameof(ProjectileDamage), config.ProjectileDamage);
        config.ProjectileDamageType = (DamageType)GetInt(dict, nameof(ProjectileDamageType), (int)config.ProjectileDamageType);
        config.ProjectileAilmentChance = GetFloat(dict, nameof(ProjectileAilmentChance), config.ProjectileAilmentChance);
        config.ProjectileAnimationPath = GetString(dict, nameof(ProjectileAnimationPath), config.ProjectileAnimationPath);
        config.ProjectileAnimationOffset = GetVector2(dict, nameof(ProjectileAnimationOffset), config.ProjectileAnimationOffset);
        config.ProjectileScale = GetVector2(dict, nameof(ProjectileScale), config.ProjectileScale);
        config.ProjectileHealth = GetFloat(dict, nameof(ProjectileHealth), config.ProjectileHealth);
        return config;
    }

    private static int GetInt(GodotDictionary dict, string key, int fallback)
    {
        return dict.TryGetValue(key, out var value) ? (int)value : fallback;
    }

    private static float GetFloat(GodotDictionary dict, string key, float fallback)
    {
        return dict.TryGetValue(key, out var value) ? (float)value : fallback;
    }

    private static string GetString(GodotDictionary dict, string key, string fallback)
    {
        return dict.TryGetValue(key, out var value) ? (string)value : fallback;
    }

    private static Vector2 GetVector2(GodotDictionary dict, string key, Vector2 fallback)
    {
        return dict.TryGetValue(key, out var value) ? (Vector2)value : fallback;
    }
}

public partial class StationaryTowerMinion : Minion
{
    private StationaryTowerMinionConfig config;
    private int remainingBullets;
    private float attackCooldown;
    private PhysicsDirectSpaceState2D spaceState;

    protected override MinionKind DefaultKind => MinionKind.StationaryTower;

    protected override void AddBehaviorComponents()
    {
        config = StationaryTowerMinionConfig.FromDict(Stats.BehaviorData);
        remainingBullets = Math.Max(config.BulletCount, 0);
        attackCooldown = 0f;
        spaceState = GetWorld2D().DirectSpaceState;

        if (Multiplayer.IsServer() && remainingBullets <= 0)
        {
            Destroy();
        }
    }

    protected override void ProcessMinion(double delta)
    {
        if (!Multiplayer.IsServer() || remainingBullets <= 0 || Spawner == null)
        {
            return;
        }

        attackCooldown -= (float)delta;
        if (attackCooldown > 0)
        {
            return;
        }

        var target = FindNearestVisibleTarget();
        if (target == null)
        {
            return;
        }

        FireAt(target);
        remainingBullets--;
        attackCooldown = Math.Max(config.AttackInterval, 0f);

        if (remainingBullets <= 0)
        {
            Destroy();
        }
    }

    private IEntityComponent FindNearestVisibleTarget()
    {
        var origin = GetEntityPosition(this);
        var attackRadiusSquared = config.AttackRadius * config.AttackRadius;
        IEntityComponent nearestTarget = null;
        var nearestDistanceSquared = float.PositiveInfinity;

        foreach (var candidate in GetTargetCandidates())
        {
            if (candidate == this || !CombatTargeting.ShouldAbilityAffect(this, candidate))
            {
                continue;
            }

            var targetPosition = GetEntityPosition(candidate);
            var distanceSquared = origin.DistanceSquaredTo(targetPosition);
            if (distanceSquared > attackRadiusSquared || distanceSquared >= nearestDistanceSquared)
            {
                continue;
            }

            if (!HasLineOfSight(origin, targetPosition))
            {
                continue;
            }

            nearestTarget = candidate;
            nearestDistanceSquared = distanceSquared;
        }

        return nearestTarget;
    }

    private IEnumerable<IEntityComponent> GetTargetCandidates()
    {
        var seen = new HashSet<IEntityComponent>();

        if (GameManager != null)
        {
            foreach (var player in GameManager.GetPlayers())
            {
                if (seen.Add(player))
                {
                    yield return player;
                }
            }
        }

        foreach (var node in GetTree().GetNodesInGroup("Minion"))
        {
            if (node is IEntityComponent entity && seen.Add(entity))
            {
                yield return entity;
            }
        }
    }

    private bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        var query = new PhysicsRayQueryParameters2D
        {
            From = from,
            To = to,
            CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.Wall,
            CollideWithAreas = false,
            CollideWithBodies = true,
        };

        return spaceState.IntersectRay(query).Count == 0;
    }

    private void FireAt(IEntityComponent target)
    {
        var targetPosition = GetEntityPosition(target);
        var direction = targetPosition - GlobalPosition;
        if (direction == Vector2.Zero)
        {
            return;
        }

        var request = new ProjectileSpawnRequest
        {
            Caller = MinionOwner ?? this,
            StartPosition = GlobalPosition,
            Movement = new ProjectileMovementConfig
            {
                Direction = direction.Normalized(),
                Speed = config.ProjectileSpeed,
            },
            Collision = new ProjectileCollisionConfig
            {
                PierceCount = 1,
                CollisionMask = CombatCollisionLayers.ProjectileTargets,
            },
            Lifetime = new ProjectileLifetimeConfig
            {
                Seconds = config.ProjectileLifetime,
            },
            Visual = new ProjectileVisualConfig
            {
                AnimationPath = config.ProjectileAnimationPath,
                AnimationOffset = config.ProjectileAnimationOffset,
                Scale = config.ProjectileScale,
            },
            Health = new ProjectileHealthConfig
            {
                Life = config.ProjectileHealth,
            },
        };

        Spawner.SpawnProjectile(request, new ProjectileRuntime
        {
            OnHit = OnProjectileHit,
        });
    }

    private void OnProjectileHit(IEntityComponent target, Projectile projectile)
    {
        if (!target.TryGetComponent(out DamageAbleComponent damageAbleComponent))
        {
            return;
        }

        var damage = new Damage
        {
            DamageNumber = config.ProjectileDamage,
            AilmentChance = config.ProjectileAilmentChance,
            Type = config.ProjectileDamageType,
        };
        var ctx = new HitContext
        {
            AbilityGuid = Stats.AbilityGUID,
            Source = MinionOwner ?? this,
            Target = target,
            Damages = new System.Collections.Generic.Dictionary<DamageType, Damage>
            {
                { damage.Type, damage },
            },
        };

        damageAbleComponent.ReceiveHit(new Hit(projectile, ctx));
    }

    private static Vector2 GetEntityPosition(IEntityComponent entity)
    {
        if (entity.TryGetComponent(out AimComponent aimComponent))
        {
            return aimComponent.GetCharacterCenterPosition();
        }

        return entity is Node2D node ? node.GlobalPosition : Vector2.Zero;
    }
}
