using System.Linq;

namespace CardBase.Scripts.Abilities.HitMods;

public class ResetCooldownHitModifier : IHitModifier
{
    public void ApplyBefore(HitContext ctx)
    {
        
    }

    public void ApplyAfter(HitContext ctx)
    {
        var ability = ctx.Source.Abilities.FirstOrDefault(a => a.GUID == ctx.AbilityGuid);
        if (ability != null)
        {
            ability.CurrentCooldown = 0.1f;
        }
    }
}