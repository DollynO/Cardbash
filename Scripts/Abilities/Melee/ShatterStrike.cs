using CardBase.Scripts.Abilities.HitMods;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class ShatterStrike : Ability
{
    public ShatterStrike(PlayerCharacter creator) : base(AbilityIds.ShatterStrikeGuid, creator)
    {
        this.DisplayName = "Shatter Strike";
        this.Description = "A brutal melee blow that consumes all stacks of Frost on the target, detonating the icy buildup. The chill explodes into shards, stunning the enemy briefly while dealing heavy cold-infused damage.";
        this.IconPath = "res://Sprites/SkillIcons/Snow/18_Ice_Sword.png";
        this.BaseCooldown = 5;
        this.BaseDamage = 10;
        this.BaseType = DamageType.Ice;
        this.BaseAilmentChance = 0.33f;
        
        this._hitModifiers.Add(new FrostShatterHitModifier());
    }

    public override void InternalUse()
    {
        Caller.RequestMeleeCone(new MeleeConeProperties
        {
            Angle = 45,
            AttackTime = 0.5f,
            Damage = new Damage
            {
                DamageNumber = (float)BaseDamage,
                AilmentChange = BaseAilmentChance,
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
    }

    public override void RegisterSpawnedNode(Node node)
    {
    }
}