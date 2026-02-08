using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class FastMovement : Buff
{
    public float MovementIncrease
    {
        get => _movementIncrease;
        set
        {
            _movementIncrease = value;
            if (mod != null)
            {
                mod.Value = value;
            }
        }
    }
    private float _movementIncrease;
    private StatModifier mod;
    public FastMovement(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
        mod = new StatModifier(System.Guid.NewGuid().ToString("N"), StatType.MovementSpeed, StatOp.PercentAdd,
            MovementIncrease);
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