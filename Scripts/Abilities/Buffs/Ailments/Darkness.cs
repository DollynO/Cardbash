using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class Darkness : Buff
{
    private StatModifier modifer;
    public Darkness(PlayerCharacter caller, PlayerCharacter target) : base(caller, target)
    {
        this.Description = $"Reduces the vision range of the target.";
        this.DisplayName = "Darkness";
        this.IconPath = "res://Sprites/SkillIcons/Dark/19_Eclipse.png";
        this.Duration = 15;
        this.Guid = "6B0039DA-6E85-4659-829B-599D6D0BB43A";
        this.IsStackable = true;
        this.IsRefreshable = true;
        modifer = new StatModifier(System.Guid.NewGuid().ToString("n"), StatType.Darkness, StatOp.FlatAdd,
            1);
    }

    protected override void InternalOnActivate()
    {
        Target.StatBlock.AddModifiers(modifer);
    }

    protected override void InternalOnTick(float delta)
    {
    }

    protected override void InternalOnDeactivate()
    {
        Target.StatBlock.RemoveModifierSource(modifer.SourceId);
    }
}