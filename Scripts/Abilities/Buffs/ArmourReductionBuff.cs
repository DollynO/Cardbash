using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class ArmourReductionBuff : Buff
{
    private StatModifier mod;
    public ArmourReductionBuff(PlayerCharacter caller, PlayerCharacter target) : base(caller, target)
    {
        mod = new StatModifier(System.Guid.NewGuid().ToString("N"), StatType.Armor, StatOp.PercentAdd, -0.2f);
    }

    protected override void InternalOnActivate()
    {
        this.Target.StatBlock.AddModifiers(mod);
    }

    protected override void InternalOnTick(float delta)
    {
    }

    protected override void InternalOnDeactivate()
    {
        this.Target.StatBlock.RemoveModifierSource(mod.SourceId);
    }
}