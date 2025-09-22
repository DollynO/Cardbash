using CardBase.Scripts.Abilities.Buffs;

namespace CardBase.Scripts.Abilities.HitMods;

public class ApplyBuffOnHitModifier : IHitModifier
{
    private readonly Buff _buff;
    
    public ApplyBuffOnHitModifier(Buff buff)
    {
        _buff = buff;
    }
    
    public void ApplyBefore(HitContext ctx)
    {
    }

    public void ApplyAfter(HitContext ctx)
    {
        ctx.Target.ApplyBuff(_buff);
    }
}