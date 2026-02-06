using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class ShockDebuff : Buff
{
    private StatModifier stat_modifier;
    private float base_reduction = 5;
    
    public ShockDebuff(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
        this.Description = $"Reduces the energy shield of the target by {base_reduction}";
        this.DisplayName = "Shock";
        this.IconPath = "res://Sprites/SkillIcons/Lightning/6_Electricshock.png";
        this.Duration = 0.75f;
        this.MaxStacks = 10;
        this.IsStackable = true;
        this.IsRefreshable = true;
        this.Guid = "25C6FDD0-4499-4F9E-AC8D-0655C6965FD3";
        stat_modifier = new StatModifier(System.Guid.NewGuid().ToString("N"), StatType.EnergyShield, StatOp.PercentAdd, -base_reduction);
    }


    protected override void InternalOnActivate() {
        if (Target.TryGetComponent<StatblockComponent>(out var statBlock))
        { 
            statBlock.AddModifiers(stat_modifier);
        }
    }

    protected override void InternalOnTick(float delta)
    {
    }

    protected override void InternalOnDeactivate()
    {
        if (Target.TryGetComponent<StatblockComponent>(out var statBlock))
        {
            statBlock.RemoveModifierSource(stat_modifier.SourceId);
        }
    }
}