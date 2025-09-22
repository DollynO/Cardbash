using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities.Buffs;

public class BurnDebuff : Buff
{
    private float base_damage;
    private DamageType base_damage_type;
    
    public BurnDebuff(PlayerCharacter caller, PlayerCharacter target) : base(caller, target)
    {
        this.Description = "Burns the target";
        this.DisplayName = "Burn";
        this.IconPath = "res://Sprites/SkillIcons/Fire/19_Ignition.png";
        this.Duration = 10;
        this.base_damage = 1 / this.Duration;
        this.base_damage_type = DamageType.Fire;
        this.Guid = "088DF86D-7101-4BBF-A331-17B625D11379";
    }

    protected override void InternalOnActivate()
    {
    }

    protected override void InternalOnTick(float delta)
    {
        var damage = new Damage { DamageNumber = base_damage * delta, AilmentChange = 0, Type = base_damage_type };
        var ctx = new HitContext
        {
            Source = Caller,
            Target = Target,
            Damages = new System.Collections.Generic.Dictionary<DamageType, Damage>()
            {
                { damage.Type, damage },
            }
        }; 
        Target.ApplyDamage(ctx);
    }

    protected override void InternalOnDeactivate()
    {
    }
}