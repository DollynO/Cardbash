using System;
using CardBase.Scripts.Cards;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public interface IBuff : IBaseProperty
{
    public float Duration { get; }
    public float RemainingDuration { get; }
    public PlayerCharacter Caller { get; }
    public PlayerCharacter Target { get; }
    public DamageType BuffType { get; }

    public void OnActivate();
    public bool OnTick(float delta);
    public void OnDeactivate();
}