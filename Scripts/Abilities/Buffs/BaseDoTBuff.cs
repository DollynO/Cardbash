using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class BaseDoTBuff : Buff
{
    protected float BaseDamage;
    protected DamageType BaseDamageType;
    protected DamageAbleComponent dac;
    
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
        
        var damage = new Damage { DamageNumber = BaseDamage * StackCount * delta, AilmentChance = 0, Type = BaseDamageType };
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
    }
}