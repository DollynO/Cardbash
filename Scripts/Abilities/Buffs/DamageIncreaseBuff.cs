using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class DamageIncreaseBuff : Buff
{
    
    protected StatModifier mod;
    
    public DamageIncreaseBuff(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
    }

    protected override void InternalOnActivate()
    {
        if (Target.TryGetComponent<StatblockComponent>(out var statBlock))
        {
            statBlock.AddModifiers(mod);
        }
    }

    protected override void InternalOnTick(float delta)
    {
    }

    protected override void InternalOnDeactivate()
    {
        if (Target.TryGetComponent<StatblockComponent>(out var statBlock))
        {
            statBlock.RemoveModifierSource(mod.SourceId);
        }
    }
}