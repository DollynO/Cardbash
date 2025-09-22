using System;

namespace CardBase.Scripts.Abilities.HitMods;

public class ConsumeBuffTypeHitModifier :IHitModifier
{
    private Action<int, HitContext> onConsumeAction;
    private DamageType consumeType;
    private bool beforeAfter;

    public ConsumeBuffTypeHitModifier(Action<int, HitContext> onConsumeAction, DamageType consumeType, bool beforeAfter = true)
    {
        this.onConsumeAction = onConsumeAction ??  throw new ArgumentNullException(nameof(onConsumeAction));
        this.consumeType = consumeType;
        this.beforeAfter = beforeAfter;
    }
    
    public void ApplyBefore(HitContext ctx)
    {
        if (beforeAfter)
        {
            consume(ctx);
        }
    }

    public void ApplyAfter(HitContext ctx)
    {
        if (!beforeAfter)
        {
            consume(ctx);
        }
    }

    private void consume(HitContext ctx)
    {
        var consumedBuffs = ctx.Target.ConsumeBuffType(consumeType);
        if (consumedBuffs == 0) return;
        
        onConsumeAction(consumedBuffs, ctx);  
    }
}