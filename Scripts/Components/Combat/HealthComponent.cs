using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts;

public partial class HealthComponent : Node2D, IComponent
{
    private const double DamagePreviewWindowSeconds = 2.0;
    private const int DamagePreviewMaxEntries = 4;

    public IEntityComponent Parent { get; set; }
    [Export]
    public float MaxHealth { get; private set; }

    [Export]
    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    private Godot.Collections.Dictionary<string, float> maxHealthChanges = new();
    private readonly List<DamageHistoryEntry> _recentDamage = new();
    private GameManager gameManager;
    
    public override void _Ready()
    {
        Name = "HealthComponent";
        gameManager = GetNode<GameManager>("/root/Main/Game");
        ReplicationHelper.CreateSynchronizer(this,
            new ReplicationProperties(new NodePath($":{nameof(MaxHealth)}"), SceneReplicationConfig.ReplicationMode.OnChange),
            new ReplicationProperties(new NodePath($":{nameof(CurrentHealth)}"), SceneReplicationConfig.ReplicationMode.OnChange));
    }

    public void Reset(float newMaxHealth)
    {
        MaxHealth = newMaxHealth;
        CurrentHealth = MaxHealth;
        _recentDamage.Clear();
    }

    public void ApplyDamage(Damage damage, IEntityComponent component)
    {
        if (IsDead)
        {
            return;
        }

        var damageValue = damage.DamageNumber;
        damageValue = Mathf.Abs(damageValue);
        var previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Clamp(CurrentHealth - damageValue, 0, MaxHealth);
        TrackDamage(component, damage.Type, previousHealth - CurrentHealth);
        Parent.EventBus.CombatEventBus.EmitDamageTaked(new DamageEventArgs(component, Parent, damage));

        if (IsDead)
        {
            var damagePreview = BuildDamagePreview();
            if (Parent is PlayerCharacter victimPlayer)
            {
                var killerPlayer = component as PlayerCharacter;
                gameManager?.NotifyPlayerDeath(victimPlayer, killerPlayer, damagePreview);
            }
            Parent.EventBus.CombatEventBus.EmitKilled(new KilledEventArgs(component, Parent));
        }
    }

    private void TrackDamage(IEntityComponent source, DamageType damageType, float damageAmount)
    {
        if (damageAmount <= 0f)
        {
            return;
        }

        var now = Godot.Time.GetTicksMsec() / 1000.0;
        _recentDamage.Add(new DamageHistoryEntry(now, source, damageType, damageAmount));
        PruneDamageHistory(now);
    }

    private void PruneDamageHistory(double now)
    {
        _recentDamage.RemoveAll(entry => now - entry.Timestamp > DamagePreviewWindowSeconds);
    }

    private string BuildDamagePreview()
    {
        var now = Godot.Time.GetTicksMsec() / 1000.0;
        PruneDamageHistory(now);

        if (_recentDamage.Count == 0)
        {
            return string.Empty;
        }

        var entries = _recentDamage
            .GroupBy(entry => new DamagePreviewKey(GetEntityDisplayName(entry.Source), entry.DamageType))
            .Select(group => new
            {
                group.Key.SourceName,
                group.Key.DamageType,
                Damage = group.Sum(entry => entry.Amount),
            })
            .Where(entry => entry.Damage > 0f)
            .OrderByDescending(entry => entry.Damage)
            .Take(DamagePreviewMaxEntries)
            .Select(entry => $"{entry.SourceName} {FormatDamageAmount(entry.Damage)} {entry.DamageType}");

        return string.Join("  |  ", entries);
    }

    private static int FormatDamageAmount(float damage)
    {
        return Mathf.Max(1, Mathf.RoundToInt(damage));
    }

    private static string GetEntityDisplayName(IEntityComponent entity)
    {
        return entity switch
        {
            PlayerCharacter player => player.PlayerName,
            Node node when !string.IsNullOrWhiteSpace(node.Name) => node.Name,
            _ => "Unknown",
        };
    }

    public void ApplyHeal(float heal)
    {
        heal = Mathf.Abs(heal);
        CurrentHealth = Mathf.Clamp(CurrentHealth + heal, 0, MaxHealth);
    }

    public void ApplyMod(StatModifier mod)
    {
        var id = mod.Id;
        if (maxHealthChanges.ContainsKey(id))
        {
            RemoveMod(id);
        }

        var value = 0f;
        switch (mod.Op)
        {
            case StatOp.FlatAdd:
                value = mod.Value;
                break;
            case StatOp.PercentAdd:
                value = mod.Value - mod.Value * (1 + mod.Value);
                if (mod.Value < 1)
                {
                    value = -value;
                }
                break;
            case StatOp.PercentMult:
                value = 0;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        maxHealthChanges.Add(id, value);
        MaxHealth = Mathf.Max(1, MaxHealth + value);
        if (value < 0 && Mathf.Abs(value) > CurrentHealth)
        {
            CurrentHealth = 1;
        }
        else
        {
            CurrentHealth += value;
        }

        CurrentHealth = Mathf.Clamp(CurrentHealth, IsDead ? 0 : 1, MaxHealth);
    }

    public void RemoveMod(string id)
    {
        if (!maxHealthChanges.TryGetValue(id, out var value))
        {
            return;
        }

        maxHealthChanges.Remove(id);
        MaxHealth = Mathf.Max(1, MaxHealth - value);
        if (value > 0 && Mathf.Abs(value) > CurrentHealth)
        {
            CurrentHealth = 1;
        }
        else
        {
            CurrentHealth -= value;
        }

        CurrentHealth = Mathf.Clamp(CurrentHealth, IsDead ? 0 : 1, MaxHealth);
    }

    private readonly record struct DamageHistoryEntry(
        double Timestamp,
        IEntityComponent Source,
        DamageType DamageType,
        float Amount);

    private readonly record struct DamagePreviewKey(string SourceName, DamageType DamageType);
}
