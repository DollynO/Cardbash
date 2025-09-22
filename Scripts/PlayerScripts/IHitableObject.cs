using System.Collections.Generic;
using CardBase.Scripts.Abilities;

namespace CardBase.Scripts.PlayerScripts;

public interface IHitableObject
{
    public void ApplyDamage(HitContext ctx);
}