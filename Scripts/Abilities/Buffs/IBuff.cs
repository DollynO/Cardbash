using System;
using CardBase.Scripts.Cards;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public interface IBuff : IBaseProperty
{
    public float Duration { get; }
    public float RemainingDuration { get; }
    public IEntityComponent Caller { get; }
    public IEntityComponent Target { get; }
    public DamageType BuffType { get; }

    public void OnActivate();
    public bool OnTick(float delta);
    public void OnDeactivate();
}