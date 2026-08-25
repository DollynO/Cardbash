using System.Linq;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class ToxicWound : Buff, IHitModifier
{
    private readonly float poisonDamageTakenIncrease;

    public ToxicWound(IEntityComponent caller, IEntityComponent target, float damageIncrease, float duration) : base(caller, target)
    {
        Description = "Increases incoming poison damage.";
        DisplayName = "Toxic Wound";
        IconPath = "res://Sprites/SkillIcons/Poison/19_Infection.png";
        Duration = duration;
        Guid = "75D8E4CE-92C9-4439-823F-D7292BA97778";
        BuffType = DamageType.Poison;
        IsRefreshable = true;
        poisonDamageTakenIncrease = damageIncrease;
    }

    public void ApplyBefore(HitContext ctx)
    {
        foreach (var damage in ctx.Damages.Values.Where(damage => damage.Type == DamageType.Poison))
        {
            damage.DamageNumber *= 1f + poisonDamageTakenIncrease;
        }
    }

    public void ApplyAfter(HitContext ctx)
    {
    }

    protected override void InternalOnActivate()
    {
    }

    protected override void InternalOnTick(float delta)
    {
    }

    protected override void InternalOnDeactivate()
    {
    }
}
