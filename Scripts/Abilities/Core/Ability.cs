using System.Collections.Generic;
using CardBase.Scripts.Abilities.TriggerStrategy;
using CardBase.Scripts.GameSettings;
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
    public int UpdateCounter { get; protected set; }

    /**
     * @brief Strategy how the ability is triggered. Can be changed.
     */
    protected ITriggerStrategy TriggerStrategy { get; set; }

    /**
     * @brief The caller of the ability.
     */
    protected IEntityComponent Caller;

    /**
     * @brief  The on hit modifier assigned to the ability.
     */
    protected List<IHitModifier> _hitModifiers = new List<IHitModifier>();

    /**
     * @brief The base alignment chance of the ability. 
     */
    protected float BaseAilmentChance;


    protected float ChargeAmount;

    public float ChargeTime { get; protected set; }

    protected float ChargePower;

    private bool activated;
    private bool useSucceeded = true;

    protected bool AutoCast = false;

    protected virtual bool UsesStandardCooldown => true;

    private AbilityKeyState lastInputState = AbilityKeyState.ABILITY_NONE;

    protected GlobalAbilitySpawner GlobalAbilitySpawner => globalAbilitySpawner ??= ((Node2D)Caller).GetTree().Root
        .GetNode<GlobalAbilitySpawner>("/root/Main/Game/GlobalAbilitySpawner");

    private GlobalAbilitySpawner globalAbilitySpawner;

    protected Ability(string guid, IEntityComponent creator) : base(guid)
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

    public virtual void ProcessAbility(float delta)
    {

    }

    public virtual void ClearAbility()
    {

    }

    public virtual void PrepareForCardDraw()
    {
        CurrentStack = 0;
        CurrentCooldown = GetCooldownDuration();
    }

    public virtual void BeginCombat()
    {

    }

    public abstract void RoundReset();

    /**
     * @brief Updates the cooldown of the ability. Updates the stack count.
     */
    public void UpdateCooldown(double delta)
    {
        ProcessPassive(delta);
        if (!UsesStandardCooldown)
        {
            return;
        }

        if (AutoCast)
        {
            if (!preventAutoCast())
            {
                if (CurrentCooldown > 0)
                {
                    CurrentCooldown -= delta;
                }
                else
                {
                    CurrentStack++;
                    Activate();
                    Use();
                    if (Caller.TryGetComponent(out StatblockComponent statblock))
                    {
                        CurrentCooldown = BaseCooldown * statblock.GetStat(StatType.CooldownReduction);
                    }
                    else
                    {
                        CurrentCooldown = BaseCooldown;
                    }
                }
            }
        }
        else
        {
            if (CurrentStack == MaxStack)
            {
                return;
            }

            CurrentCooldown -= delta;
            if (CurrentCooldown <= 0)
            {
                CurrentStack++;
                if (Caller.TryGetComponent(out StatblockComponent statblock))
                {
                    CurrentCooldown = BaseCooldown * statblock.GetStat(StatType.CooldownReduction);
                }
                else
                {
                    CurrentCooldown = BaseCooldown;
                }
            }
        }
    }

    protected virtual void ProcessPassive(double delta)
    {
    }

    protected virtual bool preventAutoCast()
    {
        return false;
    }

    protected double GetCooldownDuration()
    {
        return Caller.TryGetComponent(out StatblockComponent statblock)
            ? BaseCooldown * statblock.GetStat(StatType.CooldownReduction)
            : BaseCooldown;
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

    protected bool ActivateWithoutStack()
    {
        this.activated = true;
        return true;
    }

    public void Charge(double delta)
    {
        if (!activated)
        {
            ChargeAmount = 0;
            return;
        }

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
            useSucceeded = true;
            InternalUse();
            this.activated = false;
            if (useSucceeded && Caller.TryGetComponent(out AbilityComponent ac))
            {
                ac.NotifyAbilityCasted(this);
            }
        }
    }

    protected void MarkUseFailed()
    {
        useSucceeded = false;
    }

    public virtual void InternalUse()
    {

    }

    public virtual void ApplyGameplayConfig(GameplayConfigEntry config)
    {
        if (config == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(config.DisplayName)) DisplayName = config.DisplayName;
        if (!string.IsNullOrWhiteSpace(config.Description)) Description = config.Description;
        if (!string.IsNullOrWhiteSpace(config.IconPath)) IconPath = config.IconPath;
        if (config.Cooldown.HasValue) BaseCooldown = config.Cooldown.Value;
        if (config.BaseDamage.HasValue) BaseDamage = config.BaseDamage.Value;
        if (config.MaxStack.HasValue) MaxStack = config.MaxStack.Value;
        if (config.BaseAilmentChance.HasValue) BaseAilmentChance = (float)config.BaseAilmentChance.Value;
        if (config.DamageType.HasValue
            && System.Enum.IsDefined(typeof(DamageType), config.DamageType.Value))
        {
            BaseType = (DamageType)config.DamageType.Value;
        }
    }

    protected float ConfigParam(string paramName, float fallback)
    {
        return GameplayConfigManager.GetAbilityParam(GUID, paramName, fallback);
    }

    protected int ConfigParam(string paramName, int fallback)
    {
        return GameplayConfigManager.GetAbilityParam(GUID, paramName, fallback);
    }

    protected void ApplyProjectileConfig(ProjectileSpawnRequest request)
    {
        if (request == null)
        {
            return;
        }

        request.Movement.Speed = ConfigParam("projectileSpeed", request.Movement.Speed);
        var movementMode = ConfigParam("movementMode", (int)request.Movement.Mode);
        if (System.Enum.IsDefined(typeof(MovementMode), movementMode))
        {
            request.Movement.Mode = (MovementMode)movementMode;
        }

        request.Movement.BounceCount = ConfigParam("bounceCount", request.Movement.BounceCount);
        request.Movement.AngleOffset = ConfigParam("angleOffset", request.Movement.AngleOffset);
        request.Collision.PierceCount = ConfigParam("pierceCount", request.Collision.PierceCount);
        request.Collision.CollisionMask = (uint)ConfigParam("collisionMask", (int)request.Collision.CollisionMask);
        request.Lifetime.Seconds = ConfigParam("projectileLifetime", request.Lifetime.Seconds);
        request.Pull.Radius = ConfigParam("pullRadius", request.Pull.Radius);
        request.Pull.Strength = ConfigParam("pullStrength", request.Pull.Strength);
        request.Health.Life = ConfigParam("projectileHealth", request.Health.Life);

        var uniformScale = ConfigParam("projectileScale", request.Visual.Scale.X);
        request.Visual.Scale = new Vector2(
            ConfigParam("projectileScaleX", uniformScale),
            ConfigParam("projectileScaleY", uniformScale));
        request.Visual.AnimationOffset = new Vector2(
            ConfigParam("animationOffsetX", request.Visual.AnimationOffset.X),
            ConfigParam("animationOffsetY", request.Visual.AnimationOffset.Y));
    }

    protected void ApplyAoeConfig(AoeBaseStats stats, bool applyAngleOffset = true)
    {
        if (stats == null)
        {
            return;
        }

        stats.Radius = ConfigParam("radius", stats.Radius);
        stats.Angle = ConfigParam("angle", stats.Angle);
        if (applyAngleOffset)
        {
            stats.AngleOffset = ConfigParam("angleOffset", stats.AngleOffset);
        }
        stats.ActivationTime = ConfigParam("activationTime", stats.ActivationTime);
        stats.Duration = ConfigParam("duration", stats.Duration);
        stats.TickInterval = ConfigParam("tickInterval", stats.TickInterval);
        stats.ShapeUpdateInterval = ConfigParam("shapeUpdateInterval", stats.ShapeUpdateInterval);
        ApplyAoeDamagePreview(stats);
    }

    protected void ApplyAoeDamagePreview(AoeBaseStats stats)
    {
        if (stats == null)
        {
            return;
        }

        stats.BaseDamageType = BaseType;
        IEnumerable<DamageModifier> damageModifiers = System.Array.Empty<DamageModifier>();
        if (Caller != null && Caller.TryGetComponent(out StatblockComponent statblock))
        {
            damageModifiers = statblock.GetDamageModifiers();
        }

        stats.DamageTypePercentages = DamageCalculator.PreviewDamageTypeMix(BaseType, damageModifiers);
    }

    protected void ApplyRayConfig(RayStats stats)
    {
        if (stats == null)
        {
            return;
        }

        stats.Range = ConfigParam("rayRange", stats.Range);
        stats.CollisionMask = (uint)ConfigParam("rayCollisionMask", (int)stats.CollisionMask);
        stats.CenterLoopCount = ConfigParam("rayCenterLoopCount", stats.CenterLoopCount);
        stats.PierceCount = ConfigParam("rayPierceCount", stats.PierceCount);
    }

    public void HandleInput(AbilityKeyState state, double delta)
    {
        switch (state)
        {
            case AbilityKeyState.ABILITY_PRESSED:
                if (lastInputState is AbilityKeyState.ABILITY_PRESSED or AbilityKeyState.ABILITY_HOLD)
                {
                    TriggerStrategy.OnKeyPressed(this, delta);
                }
                else
                {
                    TriggerStrategy.OnKeyJustPressed(this);
                }
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

        lastInputState = state;
    }

    public void ApplyUpdate()
    {
        switch (UpdateCounter)
        {
            case 0:
                ApplyUpdate1();
                break;
            case 1:
                ApplyUpdate2();
                break;
            default:
                return;
        }
        UpdateCounter++;
    }

    protected abstract void ApplyUpdate1();
    protected abstract void ApplyUpdate2();

    public void CancelAbility()
    {
        this.activated = false;
        this.InternalCancel();
    }

    protected virtual void InternalCancel()
    {

    }
}
