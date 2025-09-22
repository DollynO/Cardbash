using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class ConsecratedSlash : Ability
{
    public ConsecratedSlash(PlayerCharacter creator) : base("C9BA2A80-C3D3-4EB7-B6E9-92D6020B62CE", creator)
    {
        this.DisplayName = "Consecrated Slash";
        this.Description = "Let the crusade begin";
        this.IconPath = "res://Sprites/SkillIcons/Holy/11_Holy_Wave.png";
        this.BaseCooldown = 1;
        this.BaseDamage = 50;
        this.BaseType = DamageType.Holy;
    }

    public override void InternalUse()
    {
        for (var i = 0; i < 4; i++)
        {
            Caller.RequestMeleeCone(new MeleeConeProperties
            {
                Angle = 45,
                AttackTime = 0.8f,
                Damage = new Damage
                {
                    DamageNumber = (float)BaseDamage,
                    AilmentChange = Damage.DEFAULT_AILMENT_CHANGE,
                    Type = BaseType
                },
                Length = 60,
                Offset = 0 + i * 90,
                Owner = Caller,
                AbilityGUID = GUID,
            });
        }
    }

    protected override void InternalUpdate()
    {
        
    }

    public override void RegisterSpawnedNode(Node node)
    {
        
    }
}