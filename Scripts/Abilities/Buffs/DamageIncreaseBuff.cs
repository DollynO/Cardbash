using CardBase.Scripts.PlayerScripts;
using System.Collections.Generic;

namespace CardBase.Scripts.Abilities.Buffs;

public class DamageIncreaseBuff : Buff
{
    protected readonly List<StatModifier> mods = new();
    
    public DamageIncreaseBuff(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
    }

    protected void AddAllDamageTypeModifiers(float value)
    {
        foreach (var statType in StatblockComponent.DamageBonusStats.Values)
        {
            mods.Add(new StatModifier(Guid, statType, StatOp.FlatAdd, value));
        }
    }

    protected override void InternalOnActivate()
    {
        if (Target.TryGetComponent<StatblockComponent>(out var statBlock))
        {
            statBlock.AddModifiers(mods);
        }
    }

    protected override void InternalOnTick(float delta)
    {
    }

    protected override void InternalOnDeactivate()
    {
        if (Target.TryGetComponent<StatblockComponent>(out var statBlock))
        {
            statBlock.RemoveModifierSource(Guid);
        }
    }
}
