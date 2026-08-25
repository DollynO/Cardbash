using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class ChainLightningConfig
{
    public float Range { get; set; } = 180f;
    public int MaxJumps { get; set; } = 1;
    public float Damage { get; set; }
    public float DamageMultiplierPerJump { get; set; } = 0.5f;
    public DamageType DamageType { get; set; } = DamageType.Lightning;
    public float AilmentChance { get; set; }
    public float ArcDuration { get; set; } = 0.18f;
    public float ArcWidth { get; set; } = 5f;
    public Color ArcColor { get; set; } = new(0.45f, 0.85f, 1f, 0.95f);
}

public class ChainLightning
{
    private readonly IEntityComponent source;
    private readonly GameManager gameManager;
    private readonly GlobalAbilitySpawner spawner;
    private readonly string abilityGuid;
    private readonly ChainLightningConfig config;

    public ChainLightning(
        IEntityComponent source,
        GameManager gameManager,
        GlobalAbilitySpawner spawner,
        string abilityGuid,
        ChainLightningConfig config)
    {
        this.source = source;
        this.gameManager = gameManager;
        this.spawner = spawner;
        this.abilityGuid = abilityGuid;
        this.config = config ?? new ChainLightningConfig();
    }

    public IReadOnlyList<IEntityComponent> StrikeFrom(IEntityComponent origin, ISet<IEntityComponent> excluded = null)
    {
        var hitTargets = new List<IEntityComponent>();
        if (origin == null || config.MaxJumps <= 0 || gameManager == null || spawner == null)
        {
            return hitTargets;
        }

        var ignoredTargets = excluded != null
            ? new HashSet<IEntityComponent>(excluded)
            : new HashSet<IEntityComponent>();
        ignoredTargets.Add(origin);

        var currentOrigin = origin;
        for (var jump = 0; jump < config.MaxJumps; jump++)
        {
            var nextTarget = FindNearestTarget(currentOrigin, ignoredTargets);
            if (nextTarget == null)
            {
                break;
            }

            SpawnArc(currentOrigin, nextTarget);
            DealDamage(nextTarget, jump);
            hitTargets.Add(nextTarget);
            ignoredTargets.Add(nextTarget);
            currentOrigin = nextTarget;
        }

        return hitTargets;
    }

    private IEntityComponent FindNearestTarget(IEntityComponent origin, HashSet<IEntityComponent> ignoredTargets)
    {
        var originPosition = GetPosition(origin);

        return gameManager.GetPlayers()
            .Where(candidate => !ignoredTargets.Contains(candidate))
            .Where(candidate => CombatTargeting.ShouldAbilityAffect(source, candidate))
            .Where(candidate => GetPosition(candidate).DistanceSquaredTo(originPosition) <= config.Range * config.Range)
            .OrderBy(candidate => GetPosition(candidate).DistanceSquaredTo(originPosition))
            .FirstOrDefault();
    }

    private void DealDamage(IEntityComponent target, int jumpIndex)
    {
        if (!target.TryGetComponent(out DamageAbleComponent damageAbleComponent))
        {
            return;
        }

        var damageNumber = config.Damage * Mathf.Pow(config.DamageMultiplierPerJump, jumpIndex + 1);
        var damage = new Damage
        {
            DamageNumber = damageNumber,
            AilmentChance = config.AilmentChance,
            Type = config.DamageType,
        };
        var hitContext = new HitContext
        {
            AbilityGuid = abilityGuid,
            Source = source,
            Target = target,
            Damages = new Dictionary<DamageType, Damage>
            {
                { damage.Type, damage },
            },
        };

        damageAbleComponent.ReceiveHit(new Hit(source as Node, hitContext));
    }

    private void SpawnArc(IEntityComponent from, IEntityComponent to)
    {
        spawner.SpawnVisualConnection(new VisualConnectionStats
        {
            FromNode = from as Node2D,
            ToNode = to as Node2D,
            FromPosition = GetPosition(from),
            ToPosition = GetPosition(to),
            Duration = config.ArcDuration,
            Width = config.ArcWidth,
            Color = config.ArcColor,
        });
    }

    private static Vector2 GetPosition(IEntityComponent entity)
    {
        if (entity.TryGetComponent(out AimComponent aimComponent))
        {
            return aimComponent.GetCharacterCenterPosition();
        }

        return entity is Node2D node ? node.GlobalPosition : Vector2.Zero;
    }
}
