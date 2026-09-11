using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities.Buffs;

public class Darkness : Buff
{
    private const int MaxDarknessStacks = 10;
    private StatModifier modifer;

    public Darkness(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
        this.Description = $"Reduces the vision range of the target.";
        this.DisplayName = "Darkness";
        this.IconPath = "res://Sprites/SkillIcons/Dark/19_Eclipse.png";
        this.Duration = 5;
        this.Guid = "6B0039DA-6E85-4659-829B-599D6D0BB43A";
        this.IsStackable = true;
        this.MaxStacks = MaxDarknessStacks - 1;
        this.IsRefreshable = true;
        this.BuffType = DamageType.Darkness;
        modifer = new StatModifier(System.Guid.NewGuid().ToString("n"), StatType.Darkness, StatOp.FlatAdd,
            1);
    }

    protected override void InternalOnActivate()
    {
        if (Target.TryGetComponent<StatblockComponent>(out var statBlock))
        {
            statBlock.AddModifiers(modifer);
        }
    }

    protected override void InternalOnTick(float delta)
    {
    }

    protected override void InternalOnDeactivate()
    {
        if (Target.TryGetComponent<StatblockComponent>(out var statBlock))
        {
            statBlock.RemoveModifierSource(modifer.SourceId);
        }
    }
}
