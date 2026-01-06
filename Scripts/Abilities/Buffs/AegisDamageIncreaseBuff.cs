using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class AegisDamageIncreaseBuff : Buff
{
    private StatModifier mod;
    public AegisDamageIncreaseBuff(PlayerCharacter caller, PlayerCharacter target) : base(caller, target)
    {
        this.Guid = System.Guid.NewGuid().ToString("N");
        mod = new StatModifier(this.Guid, StatType.DmgBonus, StatOp.PercentAdd, 0.10f);
        this.IsRefreshable =  true;
        this.Description = "Short damage buff on a aegis proc.";
        this.DisplayName = "Impact surge";
        this.IconPath = "res://Sprites/SkillIcons/Holy/15_Holy_Shield.png";
        this.Duration = 3;
        this.BuffType = DamageType.Holy;
    }

    protected override void InternalOnActivate()
    {
        Target.StatBlock.AddModifiers(mod);
    }

    protected override void InternalOnTick(float delta)
    {
    }

    protected override void InternalOnDeactivate()
    {
        Target.StatBlock.RemoveModifierSource(mod.SourceId);
    }
}