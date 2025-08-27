using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class FireSlash : Ability
{
    public FireSlash(PlayerCharacter creator) : base("1DD05202-6BE7-489E-9411-CC968BF5BCB5", creator)
    {
        this.DisplayName = "Fire Slash";
        this.Description = "Melee fire strike";
        this.IconPath = "res://Sprites/SkillIcons/Fire/10_Fire_Tongue.png";
        this.BaseCooldown = 1;
        this.BaseDamage = 50;
        this.BaseType = DamageType.Fire;
    }

    public override void Use()
    {
        Caller.ApplyBuff(new Darkness(Caller, Caller));
        Caller.ApplyBuff(new Darkness(Caller, Caller));
        Caller.ApplyBuff(new Darkness(Caller, Caller));
        Caller.ApplyBuff(new Darkness(Caller, Caller));
        Caller.RequestMeleeCone(new MeleeConeProperties
        {
            Angle = 120,
            AttackTime = 2,
            Damage = new System.Collections.Generic.Dictionary<DamageType, Damage>
            {
                { DamageType.Fire, new Damage() {DamageNumber = 1f, AilmentChange = Damage.DEFAULT_AILMENT_CHANGE + 1, Type = DamageType.Fire}}
            },
            Length = 80,
            Offset = 0,
            Owner = Caller,
        });
    }

    protected override void InternalUpdate()
    {
    }

    public override void RegisterSpawnedNode(Node node)
    {
        return;
    }
}