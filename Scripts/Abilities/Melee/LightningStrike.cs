using CardBase.Scripts.Abilities.HitMods;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class LightningStrike : Ability
{
    public LightningStrike(PlayerCharacter creator) : base("2970A4C5-0C86-4EB2-8CB3-067750622083", creator)
    {
        this.DisplayName = "Lightning Strike";
        this.Description = "Fast lightning strike";
        this.IconPath = "res://Sprites/SkillIcons/Lightning/3_Electric_Boom.png";
        this.BaseCooldown = 10;
        this.BaseDamage = 20;
        this.BaseType = DamageType.Lightning;
    }
    
    public override void InternalUse()
    {
        Caller.RequestMeleeCone(new MeleeConeProperties
        {
            Angle = 15,
            AttackTime = 0.3f,
            Damage = new Damage
            {
                DamageNumber = (float)BaseDamage,
                AilmentChange = Damage.DEFAULT_AILMENT_CHANGE,
                Type = BaseType
            },
            Length = 150,
            Offset = 0,
            Owner = Caller,
            AbilityGUID = GUID,
        });
    }

    protected override void InternalUpdate()
    {
        if (UpdateCounter == 1)
        {
            this._hitModifiers.Add(new ResetCooldownHitModifier());
        }

        if (UpdateCounter == 2)
        {
            this.BaseCooldown = 5;
        }
    }

    public override void RegisterSpawnedNode(Node node)
    {
        
    }
}