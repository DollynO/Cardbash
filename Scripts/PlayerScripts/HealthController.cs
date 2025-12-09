using System;
using System.Collections.Generic;
using Godot;

namespace CardBase.Scripts.PlayerScripts;

[GlobalClass]
public partial class HealthController : Node
{
    public float MaxHealth { get; private set; }
    public float CurrentHealth { get; private set; }
    
    [Export] private PlayerCharacter _playerCharacter;

    private Dictionary<string, float> maxHealthChanges = new();

    public override void _Ready()
    {
        
    }

    public void Reset(float newMaxHealth)
    {
        MaxHealth = newMaxHealth;
        CurrentHealth = MaxHealth;
        maxHealthChanges.Clear();
    }

    public void ApplyDamage(float damage)
    {
        damage = Mathf.Abs(damage);
        CurrentHealth -= damage;
        Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    public void ApplyHeal(float heal)
    {
        heal = Mathf.Abs(heal);
        CurrentHealth += heal;
        Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    public void ApplyMod(StatModifier mod)
    {
        var id = mod.Id;
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
        MaxHealth += value;
        Mathf.Clamp(MaxHealth, 1, MaxHealth);
        if (value < 0)
        {
            ApplyDamage(value);
        }
        else
        {
            ApplyHeal(value);
        }
    }

    public void RemoveMod(string id)
    {
        var value = maxHealthChanges[id];
        maxHealthChanges.Remove(id);
        if (value < 0)
        {
           ApplyHeal(value); 
        }
        else
        {
            ApplyDamage(value);
        }
    }

}