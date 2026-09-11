using System.Linq;

namespace CardBase.Scripts.Abilities.HitMods;

public class ResetCooldownHitModifier : IHitModifier
{
    public void ApplyBefore(HitContext ctx)
    {

    }

    public void ApplyAfter(HitContext ctx)
    {
        if (ctx.Source.TryGetComponent<AbilityComponent>(out var abilityComponent))
        {
            var ability = abilityComponent.Abilities.FirstOrDefault(a => a.Value.GUID == ctx.AbilityGuid);
            if (ability.Value != null)
            {
                ability.Value.CurrentCooldown = 0.1f;
            }
        }
    }
}
