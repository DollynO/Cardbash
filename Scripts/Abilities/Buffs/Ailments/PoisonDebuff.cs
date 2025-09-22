using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class PoisonDebuff : Buff
{
    private Damage poisonDamage;
    private float baseDamage = 10;
    public PoisonDebuff(PlayerCharacter caller, PlayerCharacter target) : base(caller, target)
    {
        this.Description = $"Inflicts the target with poison stack";
        this.DisplayName = "Poison";
        this.IconPath = "res://Sprites/SkillIcons/Poison/19_Infection.png";
        this.Duration = 15;
        this.Guid = "0970FD01-B46F-4817-8157-BC543FBDD3A9";
        this.IsStackable = true;
        this.IsRefreshable = false;
        poisonDamage = new Damage()
        {
            Type = DamageType.Poison,
            AilmentChange = 0,
            DamageNumber = baseDamage,
        };
    }

    protected override void InternalOnActivate()
    {
    }

    protected override void InternalOnTick(float delta)
    {
        poisonDamage.DamageNumber = baseDamage * this.StackCount * delta;
        var ctx = new HitContext
        {
            Source = Caller,
            Target = Target,
            Damages = new System.Collections.Generic.Dictionary<DamageType, Damage>()
            {
                { poisonDamage.Type, poisonDamage },
            }
        };
        Target.ApplyDamage(ctx);
    }

    protected override void InternalOnDeactivate()
    {
    }
}