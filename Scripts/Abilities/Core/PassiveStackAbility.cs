using System;
using CardBase.Scripts.GameSettings;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities;

public abstract class PassiveStackAbility : Ability
{
    private bool passiveGenerationStarted;
    private double passiveCooldown;

    protected int PassiveStackCount { get; private set; }
    protected int MaxPassiveStacks { get; private set; } = 1;
    protected override bool UsesStandardCooldown => false;

    protected PassiveStackAbility(string guid, IEntityComponent creator) : base(guid, creator)
    {
        if (creator != null)
        {
            creator.EventBus.MatchEventBus.GamePhaseStartedEventHandler += OnRoundStarted;
        }
    }

    protected void InitializePassiveStacks(int stackCount)
    {
        MaxPassiveStacks = Math.Max(1, stackCount);
        MaxStack = MaxPassiveStacks;
        SyncPassiveStackCount();
    }

    public override bool Activate()
    {
        return CanManualCast() && ActivateWithoutStack();
    }

    public override void ApplyGameplayConfig(GameplayConfigEntry config)
    {
        base.ApplyGameplayConfig(config);
        SyncPassiveStackCount();
    }

    public override void RoundReset()
    {
        passiveGenerationStarted = false;
        passiveCooldown = GetPassiveCooldown();
        CurrentCooldown = passiveCooldown;
        ClearPassiveStacks();
    }

    public override void ClearAbility()
    {
        if (Caller != null)
        {
            Caller.EventBus.MatchEventBus.RoundStartEventHandler -= OnRoundStarted;
        }

        ClearPassiveStacks();
    }

    protected override void ProcessPassive(double delta)
    {
        if (!passiveGenerationStarted || !CanCreatePassiveStack())
        {
            return;
        }

        passiveCooldown -= delta;
        CurrentCooldown = Math.Max(0, passiveCooldown);
        if (passiveCooldown > 0)
        {
            return;
        }

        if (CreatePassiveStack())
        {
            PassiveStackCount = Math.Min(PassiveStackCount + 1, MaxPassiveStacks);
            SyncPassiveStackCount();
        }

        passiveCooldown = GetPassiveCooldown();
        CurrentCooldown = passiveCooldown;
    }

    protected virtual bool CanManualCast()
    {
        return false;
    }

    protected virtual bool CanCreatePassiveStack()
    {
        return PassiveStackCount < MaxPassiveStacks;
    }

    protected bool ConsumePassiveStack()
    {
        if (PassiveStackCount <= 0)
        {
            return false;
        }

        PassiveStackCount--;
        SyncPassiveStackCount();
        return true;
    }

    protected int ConsumeAllPassiveStacks()
    {
        var stackCount = PassiveStackCount;
        PassiveStackCount = 0;
        SyncPassiveStackCount();
        return stackCount;
    }

    protected virtual void ClearPassiveStacks()
    {
        ConsumeAllPassiveStacks();
    }

    protected virtual double GetPassiveCooldown()
    {
        if (Caller != null && Caller.TryGetComponent(out StatblockComponent statblock))
        {
            return BaseCooldown * statblock.GetStat(StatType.CooldownReduction);
        }

        return BaseCooldown;
    }

    protected abstract bool CreatePassiveStack();

    private void OnRoundStarted(object sender, MatchEventArgs e)
    {
        passiveGenerationStarted = true;
        passiveCooldown = GetPassiveCooldown();
        CurrentCooldown = passiveCooldown;
    }

    private void SyncPassiveStackCount()
    {
        CurrentStack = PassiveStackCount;
        MaxStack = MaxPassiveStacks;
    }
}
