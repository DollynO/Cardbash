using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class AegisDamageIncreaseBuff : DamageIncreaseBuff
{
    public AegisDamageIncreaseBuff(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
        this.Guid = System.Guid.NewGuid().ToString("N");
        AddAllDamageTypeModifiers(0.10f);
        this.IsRefreshable = true;
        this.Description = "Short damage buff on a aegis proc.";
        this.DisplayName = "Impact surge";
        this.IconPath = "res://Sprites/SkillIcons/Holy/15_Holy_Shield.png";
        this.Duration = 3;
        this.BuffType = DamageType.Holy;
    }
}
