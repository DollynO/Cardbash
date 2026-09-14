using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs.DoTs;

public class Agony : BaseDoTBuff
{
    public Agony(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
        this.Description = "Stackable debuff.";
        this.DisplayName = "Agony";
        this.IconPath = "res://Sprites/SkillIcons/Dark/7_Black_Label.png";
        this.Duration = 15;
        this.BaseDamage = 10;
        this.BaseDamageType = DamageType.Darkness;
        this.Guid = "AD034034-42A9-4163-A448-5998214CB7ED";
        this.BuffType = DamageType.Darkness;
        this.IsStackable = true;
        this.IsRefreshable = true;
        this.MaxStacks = 10;
    }

    public void SetStartingStacks(int amount)
    {
        this.StackCount = amount > 0 ? amount - 1 : 0;
    }
}
