using System;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts;

public class CombatEventBus
{
    public event EventHandler<KilledEventArgs> KilledEventHandler;

    public void EmitKilled(KilledEventArgs args)
    {
        this.KilledEventHandler?.Invoke(this, args);
    }
    
    public event EventHandler<DamageEventArgs> DamageTakeEventHandler;

    public void EmitDamageTaked(DamageEventArgs args)
    {
        this.DamageTakeEventHandler?.Invoke(this, args);
    }
    
    public event EventHandler<DamageEventArgs> DamageMitigatedEventHandler;

    public void EmitDamageMitigated(DamageEventArgs args)
    {
        this.DamageMitigatedEventHandler?.Invoke(this, args);
    }
    
    public event EventHandler<DamageEventArgs> DamageHealedEventHandler;

    public void EmitDamageHealed(DamageEventArgs args)
    {
        this.DamageHealedEventHandler?.Invoke(this, args);
    }
    
    public event EventHandler<BuffEventArgs> BuffAddedEventHandler;

    public void EmitBuffAdded(BuffEventArgs args)
    {
        this.BuffAddedEventHandler?.Invoke(this, args);
    }

    public event EventHandler<BuffEventArgs> BuffRemovedEventHandler;

    public void EmitBuffRemoved(BuffEventArgs args)
    {
        this.BuffRemovedEventHandler?.Invoke(this, args);
    }

    public event EventHandler<AbilityEventArgs> AbilityCastedEventHandler;

    public void EmitAbilityCasted(AbilityEventArgs args)
    {
        this.AbilityCastedEventHandler?.Invoke(this, args);
    }
}

public class DamageEventArgs
{
    public IEntityComponent Source { get; init; }
    public IEntityComponent Target { get; init; }
    public Damage Damage { get; init; }

    public DamageEventArgs(IEntityComponent sourcePlayerId, IEntityComponent targetPlayerId, Damage damage)
    {
        Source = sourcePlayerId;
        Target = targetPlayerId;
        Damage = damage;
    }
}

public class KilledEventArgs
{
    public IEntityComponent Source { get; init; }
    public IEntityComponent Target { get; init; }

    public KilledEventArgs(IEntityComponent sourcePlayer, IEntityComponent targetPlayer)
    {
        Source = sourcePlayer;
        Target = targetPlayer;
    }
}

public class BuffEventArgs
{
    public Buff Buff { get; init; }

    public BuffEventArgs(Buff buff)
    {
        Buff = buff;
    }
}

public class AbilityEventArgs : EventArgs
{
    public readonly Ability Ability;
    public readonly IEntityComponent Source;
    
    public AbilityEventArgs(Ability ability, IEntityComponent source)
    {
        Ability = ability;
        Source = source;
    }
}