using System.Collections.Generic;
using CardBase.Scripts.Abilities;

namespace CardBase.Scripts;

public partial class DamageAbleComponent : Component
{
    private HealthComponent _health;
    public List<IHitInterceptor> _HitInterceptors = new();

    public DamageAbleComponent()
    {
        Name = "DamageAbleComponent";
    }

    public bool ReceiveHit(in Hit hit)
    {
        if (!CombatTargeting.ShouldAbilityAffect(hit.Context.Source, hit.Context.Target))
        {
            return false;
        }

        foreach (var interceptor in _HitInterceptors)
        {
            if (interceptor.TryBlock(hit))
            {
                return false;
            }
        }
        HitResolver.ApplyDamage(hit.Context);
        return true;
    }
}
