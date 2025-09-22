using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class PoisonJab : Ability
{
    public PoisonJab(PlayerCharacter creator) : base("DC432A01-3394-4AF4-BE1B-9289A4E88268", creator)
    {
        this.DisplayName = "Poison Jab";
        this.Description = "Well poison jab";
        this.IconPath = "res://Sprites/SkillIcons/Poison/20_Poison_Bone.png";
        this.BaseCooldown = 5;
        this.BaseDamage = 10;
        this.BaseType = DamageType.Poison;
    }

    public override void InternalUse()
    {
        Caller.RequestMeleeCone(new MeleeConeProperties
        {
            Angle = 120,
            AttackTime = 0.8f,
            Damage = new Damage
            {
                DamageNumber = (float)BaseDamage,
                AilmentChange = Damage.DEFAULT_AILMENT_CHANGE,
                Type = BaseType
            },
            Length = 40,
            Offset = 0,
            Owner = Caller,
            AbilityGUID = GUID,
        });
    }

    protected override void InternalUpdate()
    {
    }

    public override void RegisterSpawnedNode(Node node)
    {
    }
}