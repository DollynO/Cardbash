using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class SmokeAoeConfig
{
    public float Radius { get; set; } = 120f;
    public float ActivationTime { get; set; } = 0.05f;
    public float Duration { get; set; } = 3f;
    public float Slow { get; set; } = 0.35f;
    public bool CanAffectOwner { get; set; }
    public Color FillColor { get; set; } = new(0.38f, 0.43f, 0.46f, 0.45f);
}

public class SmokeAoe
{
    private readonly IEntityComponent owner;
    private readonly GlobalAbilitySpawner spawner;
    private readonly string abilityGuid;
    private readonly SmokeAoeConfig config;
    private readonly List<AoeBase> activeAreas = new();
    private readonly Dictionary<AoeBase, Dictionary<IEntityComponent, string>> slowSources = new();

    public SmokeAoe(
        IEntityComponent owner,
        GlobalAbilitySpawner spawner,
        string abilityGuid,
        SmokeAoeConfig config = null)
    {
        this.owner = owner;
        this.spawner = spawner;
        this.abilityGuid = abilityGuid;
        this.config = config ?? new SmokeAoeConfig();
    }

    public AoeBase Spawn(Vector2 position)
    {
        var stats = new AoeBaseStats
        {
            Radius = config.Radius,
            Angle = 360f,
            ActivationTime = config.ActivationTime,
            Duration = config.Duration,
            Owner = owner,
            AbilityGUID = abilityGuid,
            IsStationary = true,
            StationaryPosition = position,
            CanAffectOwner = config.CanAffectOwner,
            Callbacks = new AoeBaseCallbacks
            {
                OnActivation = OnActivation,
                OnDeactivation = OnDeactivation,
                OnEntityEnter = OnEntityEnter,
                OnEntityExit = OnEntityExit,
            },
        };

        var smoke = spawner.SpawnAoe(stats);
        if (smoke != null)
        {
            activeAreas.Add(smoke);
        }

        return smoke;
    }

    public void Clear()
    {
        foreach (var smoke in new List<AoeBase>(activeAreas))
        {
            RemoveAreaModifiers(smoke);
            if (GodotObject.IsInstanceValid(smoke))
            {
                smoke.Cancel();
            }
        }

        activeAreas.Clear();
    }

    private void OnActivation(List<IEntityComponent> targets, AoeBase smoke)
    {
        smoke.ChangeFillColor(config.FillColor);
        foreach (var target in targets)
        {
            ApplySlow(target, smoke);
        }
    }

    private void OnEntityEnter(IEntityComponent target, AoeBase smoke)
    {
        ApplySlow(target, smoke);
    }

    private void OnEntityExit(IEntityComponent target, AoeBase smoke)
    {
        RemoveSlow(target, smoke);
    }

    private void OnDeactivation(List<IEntityComponent> targets, AoeBase smoke)
    {
        RemoveAreaModifiers(smoke);
        activeAreas.Remove(smoke);
    }

    private void ApplySlow(IEntityComponent target, AoeBase smoke)
    {
        if (target == null || !target.TryGetComponent(out StatblockComponent statBlock))
        {
            return;
        }

        if (!slowSources.TryGetValue(smoke, out var targetSources))
        {
            targetSources = new Dictionary<IEntityComponent, string>();
            slowSources[smoke] = targetSources;
        }

        if (targetSources.ContainsKey(target))
        {
            return;
        }

        var sourceId = System.Guid.NewGuid().ToString("N");
        var slow = Mathf.Clamp(config.Slow, 0f, 0.95f);
        statBlock.AddModifiers(new StatModifier(sourceId, StatType.MovementSpeed, StatOp.PercentMult, -slow));
        targetSources[target] = sourceId;
    }

    private void RemoveSlow(IEntityComponent target, AoeBase smoke)
    {
        if (target == null
            || !slowSources.TryGetValue(smoke, out var targetSources)
            || !targetSources.TryGetValue(target, out var sourceId))
        {
            return;
        }

        if (target.TryGetComponent(out StatblockComponent statBlock))
        {
            statBlock.RemoveModifierSource(sourceId);
        }

        targetSources.Remove(target);
        if (targetSources.Count == 0)
        {
            slowSources.Remove(smoke);
        }
    }

    private void RemoveAreaModifiers(AoeBase smoke)
    {
        if (!slowSources.TryGetValue(smoke, out var targetSources))
        {
            return;
        }

        foreach (var (target, sourceId) in targetSources)
        {
            if (target.TryGetComponent(out StatblockComponent statBlock))
            {
                statBlock.RemoveModifierSource(sourceId);
            }
        }

        slowSources.Remove(smoke);
    }
}
