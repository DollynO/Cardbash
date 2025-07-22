using CardBase.Scripts.Abilities;
using Godot.Collections;

namespace CardBase.Scripts.PlayerScripts;

public interface IHitableObject
{
    public void ApplyDamage(Dictionary<DamageType, float> damage, PlayerCharacter attacker);
}