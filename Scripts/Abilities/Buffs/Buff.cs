using System;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities.Buffs;

public abstract class Buff : IBuff
{
    public string Guid { get; protected set; }
    public string DisplayName { get; protected set; }
    public string Description { get; protected set; }
    public string IconPath { get; protected set; }
    public float Duration { get; set; }
    public float RemainingDuration { get; set; }
    public IEntityComponent Caller { get; protected set; }
    public IEntityComponent Target { get; set; }

    public DamageType BuffType { get; protected set; }

    protected bool IsStackable { get; set; }
    protected int MaxStacks { get; set; } = 0;
    protected bool StackedDeactivation { get; set; }
    public int StackCount { get; protected set; }

    protected bool IsRefreshable { get; set; }

    public Buff(IEntityComponent caller, IEntityComponent target)
    {
        this.Description = "NONE";
        this.DisplayName = "NONE";
        this.IconPath = "res://Sprites/SkillIcons/Dark/16_Shadow.png";
        this.Duration = 10;
        this.Guid = "E3B8C9CE-5F85-4FDB-B2C4-72797A1AE359";

        Caller = caller;
        Target = target;
    }

    public void OnActivate()
    {
        if (IsRefreshable || RemainingDuration <= 0)
        {
            RemainingDuration = Duration;
        }

        if (IsStackable && StackCount <= MaxStacks || StackCount == 0)
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
