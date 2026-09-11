using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class BaseDoTBuff : Buff
{
    protected const float DotTickInterval = 0.5f;

    protected float BaseDamage;
    protected DamageType BaseDamageType;
    protected DamageAbleComponent dac;
    private float tickAccumulator;

    public BaseDoTBuff(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
    }

    protected override void InternalOnActivate()
    {
        Target.TryGetComponent(out dac);
    }

    protected override void InternalOnTick(float delta)
    {
        if (dac == null)
        {
            return;
        }

        tickAccumulator += delta;
        while (tickAccumulator >= DotTickInterval)
        {
            tickAccumulator -= DotTickInterval;
            ApplyDamageTick(DotTickInterval);
        }
    }

    protected virtual float GetDamageForTick(float tickDelta)
    {
        return (BaseDamage / Duration) * StackCount * tickDelta;
    }

    private void ApplyDamageTick(float tickDelta)
    {
        var damagePoint = GetDamageForTick(tickDelta);
        if (damagePoint <= 0f)
        {
            return;
        }

        var damage = new Damage { DamageNumber = damagePoint, AilmentChance = 0, Type = BaseDamageType };
        var ctx = new HitContext
        {
            Source = Caller,
            Target = Target,
            Damages = new System.Collections.Generic.Dictionary<DamageType, Damage>()
            {
                { damage.Type, damage },
            }
        };
        var hit = new Hit(null, ctx);
        dac.ReceiveHit(hit);
    }

    protected override void InternalOnDeactivate()
    {
        tickAccumulator = 0f;
    }
}
