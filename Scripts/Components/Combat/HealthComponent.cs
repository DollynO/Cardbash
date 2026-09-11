using System;
using System.Collections.Generic;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts;

public partial class HealthComponent : Node2D, IComponent
{
    public IEntityComponent Parent { get; set; }
    [Export]
    public float MaxHealth { get; private set; }

    [Export]
    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    private Godot.Collections.Dictionary<string, float> maxHealthChanges = new();
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
    }

    public void ApplyDamage(Damage damage, IEntityComponent component)
    {
        if (IsDead)
        {
            return;
        }

        var damageValue = damage.DamageNumber;
        damageValue = Mathf.Abs(damageValue);
        CurrentHealth = Mathf.Clamp(CurrentHealth - damageValue, 0, MaxHealth);
        Parent.EventBus.CombatEventBus.EmitDamageTaked(new DamageEventArgs(component, Parent, damage));

        if (IsDead)
        {
            if (Parent is PlayerCharacter victimPlayer)
            {
                var killerPlayer = component as PlayerCharacter;
                gameManager?.NotifyPlayerDeath(victimPlayer, killerPlayer);
            }
            Parent.EventBus.CombatEventBus.EmitKilled(new KilledEventArgs(component, Parent));
        }
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
}
