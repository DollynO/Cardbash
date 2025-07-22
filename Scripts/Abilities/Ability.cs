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

public partial class Ability : BaseCardableObject
{
    /**
     * @brief Name of the ability
     */
    public string AbilityName { get; set; }
    
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
    
    protected Ability(string guid) : base(guid)
    {
        TriggerStrategy = new SimpleTriggerStrategy();
        MaxStack = 1;
        BaseCooldown = 1.0;
    }

    public void SetCaller(PlayerCharacter pCaller) => this.Caller = pCaller;
    
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
                GD.Print($"{AbilityName} is Pressed");
                break;
            case AbilityKeyState.ABILITY_HOLD:
                TriggerStrategy.OnKeyPressed(this, delta);
                GD.Print($"{AbilityName} charged");
                break;
            case AbilityKeyState.ABILITY_RELEASED:
                TriggerStrategy.OnKeyReleased(this);
                GD.Print($"{AbilityName} released");
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