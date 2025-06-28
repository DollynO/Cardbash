using System;
using CardBase.Scripts.Abilities.TriggerStrategy;
using CardBase.Scripts.Cards;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public enum DamageType
{
    Physical,
    Bleed,
    Poison,
    Fire,
    Ice,
    Lightning,
}

public enum AbilityState
{
    Ready,
    Active,
    OnCooldown,
}

public partial class Ability : BaseCardableObject
{
    public string Name { get; set; }
    public double BaseCooldown { get; set; }
    public double CurrentCooldown { get; set; }
    public double BaseDamage { get; set; }
    public DamageType DamageType { get; set; }
    
    public int MaxStack { get; set; }
    public int CurrentStack { get; set; }
    protected int UpdateCounter { get; set; }
    protected ITriggerStrategy TriggerStrategy { get; set; }

    protected PlayerCharacter Caller;
    
    protected Ability(string guid) : base(guid)
    {
        TriggerStrategy = new SimpleTriggerStrategy();
        MaxStack = 1;
        BaseCooldown = 1.0;
    }

    public void SetCaller(PlayerCharacter pCaller) => this.Caller = pCaller;
    
    public void UpdateCooldown(double delta)
    {
        if (CurrentStack == MaxStack)
        {
            return;
        }
        
        CurrentCooldown -= delta;
        if (CurrentCooldown <= 0)
        {
            CurrentStack++;
            if (CurrentStack <= MaxStack)
            {
                CurrentCooldown = BaseCooldown;
            }
        }
    }

    public virtual bool Activate()
    {
        if (CurrentStack == 0)
        {
            return false;
        }
        
        CurrentStack--;
        
        return true;
    }

    public virtual void Charge(double delta)
    {
    }

    public virtual void Use()
    {
        
    }

    

    public void HandleInput(AbilityKeyState state, double delta)
    {
        switch (state)
        {
            case AbilityKeyState.ABILITY_PRESSED:
                TriggerStrategy.OnKeyJustPressed(this);
                GD.Print($"{Name} is Pressed");
                break;
            case AbilityKeyState.ABILITY_HOLD:
                TriggerStrategy.OnKeyPressed(this, delta);
                GD.Print($"{Name} charged");
                break;
            case AbilityKeyState.ABILITY_RELEASED:
                TriggerStrategy.OnKeyReleased(this);
                GD.Print($"{Name} released");
                break;
            default:
                break;
        }
    }

    public void ApplyUpdate()
    {
        if (UpdateCounter > 3)
        {
            return;
        }
        
        UpdateCounter++;
        InternalUpdate();
    }

    protected virtual void InternalUpdate()
    {
        
    }
}