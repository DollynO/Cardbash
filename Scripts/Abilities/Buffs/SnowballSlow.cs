using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities.Buffs;

public class SnowballSlow : Buff
{
    private float slow;
    private StatModifier modifier;
    public SnowballSlow(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
    }

    public void SetSnowballScale(Vector2 scale)
    {
        slow = 0.8f * (scale.Length() / (scale.Length() + 1));
    }

    protected override void InternalOnActivate()
    {

        if (Target.TryGetComponent<StatblockComponent>(out var statBlock))
        {
            modifier = new StatModifier(System.Guid.NewGuid().ToString(), StatType.MovementSpeed, StatOp.PercentMult,
                1 - slow);
            statBlock.AddModifiers(modifier);
        }
    }

    protected override void InternalOnTick(float delta)
    {
    }

    protected override void InternalOnDeactivate()
    {
        if (Target.TryGetComponent<StatblockComponent>(out var statBlock))
        {
            statBlock.RemoveModifierSource(modifier.SourceId);
        }
    }
}