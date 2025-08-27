using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class Frost : Buff
{
    private StatModifier slow_modifier;
    private float base_reduction = 30;
    
    public Frost(PlayerCharacter caller, PlayerCharacter target) : base(caller, target)
    {
        this.Description = $"Reduces the movement speed of the target by {base_reduction}";
        this.DisplayName = "Frost";
        this.IconPath = "res://Sprites/SkillIcons/Snow/2_Frost_Waves.png";
        this.Duration = 10;
        this.Guid = "44069072-354D-482A-97BE-C14BABCC8966";
        slow_modifier = new StatModifier(System.Guid.NewGuid().ToString("N"), StatType.MovementSpeed, StatOp.PercentAdd, -base_reduction);
        this.IsStackable = false;
        this.IsRefreshable = true;
    }

    protected override void InternalOnActivate()
    {
        Target.StatBlock.AddModifiers(slow_modifier);
    }

    protected override void InternalOnTick(float delta)
    {
    }

    protected override void InternalOnDeactivate()
    {
        Target.StatBlock.RemoveModifierSource(slow_modifier.SourceId);
    }
}