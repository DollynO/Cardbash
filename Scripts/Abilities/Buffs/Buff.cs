using System;
using System.Collections;
using System.Runtime.CompilerServices;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities.Buffs;

public abstract class Buff : IBuff
{
    public string Guid { get; protected set; }
    public string DisplayName { get; protected set; }
    public string Description { get; protected set; }
    public string IconPath { get; protected set; }
    public float Duration { get; protected set; }
    public float RemainingDuration { get; set; }
    public PlayerCharacter Caller { get; protected set; }
    public PlayerCharacter Target { get; protected set; }
    
    public DamageType BuffType { get; protected set; }

    protected bool IsStackable { get; set; }
    protected bool StackedDeactivation { get; set; }
    public int StackCount { get; private set; }
    
    protected bool IsRefreshable { get; set; }

    public Buff(PlayerCharacter caller, PlayerCharacter target)
    {
        Caller = caller;
        Target = target;
    }

    public void OnActivate()
    {
        if (IsRefreshable || RemainingDuration == 0)
        {
            RemainingDuration = Duration;
        }

        if (IsStackable || StackCount == 0)
        {
            StackCount++;
            InternalOnActivate();
        }
    }
    
    protected abstract void InternalOnActivate();

    public bool OnTick(float delta)
    {
        this.RemainingDuration -= delta;
        this.InternalOnTick(delta);
        if (this.RemainingDuration <= 0)
        {
            if (this.StackedDeactivation && --StackCount > 0)
            {
                this.RemainingDuration = Duration;
            }
            else
            {
                return true;
            }
        }

        return false;
    }
    
    protected abstract void InternalOnTick(float delta);


    public void OnDeactivate()
    {
        InternalOnDeactivate();
        this.StackCount = 0;
    }
    
    protected abstract void InternalOnDeactivate();
}