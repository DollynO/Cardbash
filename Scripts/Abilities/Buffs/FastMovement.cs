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
    public FastMovement(PlayerCharacter caller, PlayerCharacter target) : base(caller, target)
    {
        mod = new StatModifier(System.Guid.NewGuid().ToString("N"), StatType.MovementSpeed, StatOp.PercentAdd,
            MovementIncrease);
    }

    protected override void InternalOnActivate()
    {
        this.Caller.StatBlock.AddModifiers(mod);
    }

    protected override void InternalOnTick(float delta)
    {
    }

    protected override void InternalOnDeactivate()
    {
        this.Caller.StatBlock.RemoveModifierSource(mod.SourceId);
    }
}