using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs.DoTs;

public class Agony : BaseDoTBuff
{
    public Agony(PlayerCharacter caller, PlayerCharacter target) : base(caller, target)
    {
        this.Description = "Stackable debuff.";
        this.DisplayName = "Agony";
        this.IconPath = "res://Sprites/SkillIcons/Dark/7_Black_Label.png";
        this.Duration = 15;
        this.BaseDamage = 10;
        this.BaseDamageType = DamageType.Darkness;
        this.Guid = "AD034034-42A9-4163-A448-5998214CB7ED";
        this.IsStackable = true;
        this.IsRefreshable = true;
        this.MaxStacks = 10;
    }
}