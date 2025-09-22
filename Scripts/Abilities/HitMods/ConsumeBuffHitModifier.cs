using System;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.HitMods;

public class ConsumeBuffHitModifier : IHitModifier
{
    private Action<int> onBuffConsume;
    private Type consumeType;
    private bool beforeAfter;
    
    public ConsumeBuffHitModifier(Action<int> on_buff_consume, Type type, bool beforeAfter)
    {
        onBuffConsume = on_buff_consume ?? throw new ArgumentNullException(nameof(on_buff_consume));
        if (type.BaseType != typeof(Buff))
        {
            throw new ArgumentException("Wrong consumable type");
        }
        consumeType = type;
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
        var consumed_buff_count = ctx.Target.ConsumeBuff(consumeType);
        if (consumed_buff_count > 0)
        {
            onBuffConsume(consumed_buff_count);
        } 
    }
}