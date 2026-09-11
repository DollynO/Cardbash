using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class PoisonDebuff : Buff
{
    private Damage poisonDamage;
    private float baseDamage = 2;
    private DamageAbleComponent dac;
    public PoisonDebuff(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
        this.Description = $"Inflicts the target with poison stack";
        this.DisplayName = "Poison";
        this.IconPath = "res://Sprites/SkillIcons/Poison/19_Infection.png";
        this.Duration = 5;
        this.Guid = "0970FD01-B46F-4817-8157-BC543FBDD3A9";
        this.IsStackable = true;
        this.IsRefreshable = false;
        poisonDamage = new Damage()
        {
            Type = DamageType.Poison,
            AilmentChance = 0,
            DamageNumber = baseDamage,
        };
    }

    protected override void InternalOnActivate()
    {
        Target.TryGetComponent(out dac);
    }

    protected override void InternalOnTick(float delta)
    {
        if (dac == null)
        {
            return;
        }

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
        var hit = new Hit(null, ctx);
        dac.ReceiveHit(hit);
    }

    protected override void InternalOnDeactivate()
    {
    }
}
