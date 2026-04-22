using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities.Buffs;

public class SnowballSlow : Buff
{
    private float slow;
    private StatModifier modifier;
    public SnowballSlow(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
        this.Description = "SnowballSlow";
        this.DisplayName = $"Slows based on snowball size. {slow}";
        this.IconPath = "res://Sprites/SkillIcons/Snow/2_Frost_Waves.png";
        this.Duration = 10;
        this.Guid = "7191365D-FB73-49A3-99F4-17493430DDB4";
    }

    public void SetSnowballScale(Vector2 scale)
    {
        slow = 0.7f / (0.9f + Mathf.Pow((float)Mathf.E, -0.5f * (scale.X - 6)));
    }

    protected override void InternalOnActivate()
    {

        if (Target.TryGetComponent<StatblockComponent>(out var statBlock))
        {
            modifier = new StatModifier(System.Guid.NewGuid().ToString(), StatType.MovementSpeed, StatOp.PercentMult,
                -slow);
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
