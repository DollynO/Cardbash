using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs.DoTs;

public class Corruption : BaseDoTBuff
{

    public Corruption(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
        this.Description = ".";
        this.DisplayName = "Corruption";
        this.IconPath = "res://Sprites/SkillIcons/Dark/17_The power_of_darkness.png";
        this.Duration = 10;
        this.BaseDamage = 30;
        this.BaseDamageType = DamageType.Darkness;
        this.Guid = "899B90A4-5502-4C87-83C8-003706AC9B03";
        this.IsStackable = false;
        this.IsRefreshable = false;
    }
}