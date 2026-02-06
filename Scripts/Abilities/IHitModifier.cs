using System.Collections.Generic;
using CardBase.Scripts.GameSettings;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public interface IHitModifier
{
    public void ApplyBefore(HitContext ctx);
    public void ApplyAfter(HitContext ctx);
}

public class HitContext
{
    public IEntityComponent Source { get; set; }
    public string AbilityGuid { get; set; }
    public IEntityComponent Target { get; set; }
    public Dictionary<DamageType, Damage> Damages { get; set; }
}