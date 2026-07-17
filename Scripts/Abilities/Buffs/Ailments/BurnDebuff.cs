using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities.Buffs;

public class BurnDebuff : BaseDoTBuff
{
    public BurnDebuff(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
        this.Description = "Burns the target";
        this.DisplayName = "Burn";
        this.IconPath = "res://Sprites/SkillIcons/Fire/19_Ignition.png";
        this.Duration = 10;
        this.BaseDamage = 10;
        this.BaseDamageType = DamageType.Fire;
        this.Guid = "088DF86D-7101-4BBF-A331-17B625D11379";
    }
}