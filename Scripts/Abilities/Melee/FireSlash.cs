using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class FireSlash : Ability
{
    public FireSlash(PlayerCharacter creator) : base("1DD05202-6BE7-489E-9411-CC968BF5BCB5", creator)
    {
        this.DisplayName = "Fire Slash";
        this.Description = "Melee fire strike. ";
        this.IconPath = "res://Sprites/SkillIcons/Fire/10_Fire_Tongue.png";
        this.BaseCooldown = 1;
        this.BaseDamage = 50;
        this.BaseType = DamageType.Fire;
    }

    public override void InternalUse()
    {
        Caller.RequestMeleeCone(new MeleeConeProperties
        {
            Angle = 120,
            AttackTime = 1.0f,
            Damage = new Damage
            {
                DamageNumber = (float)BaseDamage,
                AilmentChange = Damage.DEFAULT_AILMENT_CHANGE,
                Type = BaseType
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