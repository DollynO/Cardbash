using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class BaseDoTBuff : Buff
{
    protected float BaseDamage;
    protected DamageType BaseDamageType;
    
    public BaseDoTBuff(PlayerCharacter caller, PlayerCharacter target) : base(caller, target)
    {
    }

    protected override void InternalOnActivate()
    {
    }

    protected override void InternalOnTick(float delta)
    {
        var damage = new Damage { DamageNumber = BaseDamage * StackCount * delta, AilmentChange = 0, Type = BaseDamageType };
        var ctx = new HitContext
        {
            Source = Caller,
            Target = Target,
            Damages = new System.Collections.Generic.Dictionary<DamageType, Damage>()
            {
                { damage.Type, damage },
            }
        }; 
        Target.ApplyDamage(ctx);
    }

    protected override void InternalOnDeactivate()
    {
    }
}