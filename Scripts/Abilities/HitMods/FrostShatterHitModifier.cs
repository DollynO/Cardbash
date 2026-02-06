using System.Linq;
using CardBase.Scripts.Abilities.Buffs;

namespace CardBase.Scripts.Abilities.HitMods;

public class FrostShatterHitModifier : IHitModifier
{
    public void ApplyBefore(HitContext ctx)
    {
        if (ctx.Target.TryGetComponent<BuffManagerComponent>(out var buffManagerComponent))
        {
            var consumed = buffManagerComponent.ConsumeBuff(typeof(Frost));
            if (consumed <= 0) return;
            if (ctx.Damages.TryGetValue(DamageType.Ice, out var damage))
            {
                damage.DamageNumber *= (1 + consumed * 0.1f);
            }
        }
    }

    public void ApplyAfter(HitContext ctx)
    {
    }
}