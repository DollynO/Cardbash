using System.Collections.Generic;
using CardBase.Scripts.Abilities.TriggerStrategy;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public enum AbilityState
{
    Ready,
    Active,
    OnCooldown,
}

public abstract class Ability : BaseCardableObject
{
    /**
     * @brief Base cooldown
     */
    public double BaseCooldown { get; set; }
    
    /**
     * @brief Current cooldown. Is set to the @ref BaseCooldown.
     */
    public double CurrentCooldown { get; set; }
    
    /**
     * @brief Base damage of the ability.
     */
    public double BaseDamage { get; set; }
    
    /**
     * @brief Base type of the ability. Can be changed through upgrades.
     */
    public DamageType BaseType { get; set; }
    
    /**
     * @brief Maximum stacks. Has to be at least one.
     */
    public int MaxStack { get; set; }
    
    /**
     * @brief Current stack count. If count == 0 ability is disabled.
     */
    public int CurrentStack { get; set; }
    
    /**
     * @brief Internal update counter. tracks the current state.
     */
    protected int UpdateCounter { get; set; }
    
    /**
     * @brief Strategy how the ability is triggered. Can be changed.
     */
    protected ITriggerStrategy TriggerStrategy { get; set; }

    /**
     * @brief The caller of the ability.
     */
    protected PlayerCharacter Caller;
    
    /**
     * @brief  The on hit modifier assigned to the ability.
     */
    protected List<IHitModifier> _hitModifiers = new List<IHitModifier>();
    
    /**
     * @brief The base alignment chance of the ability. 
     */
    protected float BaseAilmentChance;

    protected float ChargeAmount;

    protected float ChargeTime;

    protected float ChargePower;

    private bool activated;
    
    protected Ability(string guid, PlayerCharacter creator) : base(guid)
    {
        TriggerStrategy = new SimpleTriggerStrategy();
        MaxStack = 1;
        BaseCooldown = 1.0;
        Caller = creator;
    }

    public virtual List<IHitModifier> GetHitModifiers()
    {
        return _hitModifiers;
    }
    
    /**
     * @brief Updates the cooldown of the ability. Updates the stack count.
     */
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
        this.activated = true;
        
        return true;
    }

    public void Charge(double delta)
    {
        if (ChargeAmount < 1)
        {
            ChargeAmount += (float)delta / ChargeTime;
        }
    }

    public void Use()
    {
        ChargeAmount = 0;
        if (this.activated)
        {
            InternalUse();
            this.activated = false;
        }
    }
    
    public virtual void InternalUse()
    {
        
    }

    public void HandleInput(AbilityKeyState state, double delta)
    {
        switch (state)
        {
            case AbilityKeyState.ABILITY_PRESSED:
                TriggerStrategy.OnKeyJustPressed(this);
                break;
            case AbilityKeyState.ABILITY_HOLD:
                TriggerStrategy.OnKeyPressed(this, delta);
                break;
            case AbilityKeyState.ABILITY_RELEASED:
                TriggerStrategy.OnKeyReleased(this);
                break;
            default:
                break;
        }
    }

    public void ApplyUpdate()
    {
        if (UpdateCounter > 2)
        {
            return;
        }
        
        UpdateCounter++;
        InternalUpdate();
    }

    protected abstract void InternalUpdate();

    public abstract void RegisterSpawnedNode(Node node);

    public void CancleAbility()
    {
        
    }
}