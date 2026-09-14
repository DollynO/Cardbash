using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class PoisonDebuff : BaseDoTBuff
{
    private float baseDamage = 2;

    public PoisonDebuff(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
        this.Description = $"Inflicts the target with poison stack";
        this.DisplayName = "Poison";
        this.IconPath = "res://Sprites/SkillIcons/Poison/19_Infection.png";
        this.Duration = 5;
        this.Guid = "0970FD01-B46F-4817-8157-BC543FBDD3A9";
        this.IsStackable = true;
        this.IsRefreshable = false;
        this.BaseDamage = baseDamage;
        this.BaseDamageType = DamageType.Poison;
        this.BuffType = DamageType.Poison;
    }

    protected override float GetDamageForTick(float tickDelta)
    {
        return BaseDamage * StackCount * tickDelta;
    }
}
