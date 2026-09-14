using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class CounterLeap : DamageIncreaseBuff
{
    public CounterLeap(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
        this.Guid = System.Guid.NewGuid().ToString("N");
        AddAllDamageTypeModifiers(0.20f);
        this.IsRefreshable = true;
        this.Description = "Boosts the damage of the next damage ability by 20%";
        this.DisplayName = "CounterLeap";
        this.IconPath = "res://Sprites/SkillIcons/Dark/16_Shadow.png";
        this.Duration = 10;
        this.BuffType = DamageType.Physical;
    }
}
