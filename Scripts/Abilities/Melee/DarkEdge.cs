using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.Abilities.HitMods;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class DarkEdge : Ability
{
    public DarkEdge(PlayerCharacter creator) : base("92DF5DEB-48D7-48A7-B884-9DF358573157", creator)
    {
        this.DisplayName = "Dark Edge";
        this.Description = "Consumes all darkness / active debuffs on the target and deals damage for each consumed.";
        this.IconPath = "res://Sprites/SkillIcons/Dark/10_Dark_Blade.png";
        this.BaseCooldown = 1;
        this.BaseDamage = 10;
        this.BaseType = DamageType.Darkness;
        this._hitModifiers.Add(new ConsumeBuffTypeHitModifier(onBuffConsumed, DamageType.Darkness));
    }

    private void onBuffConsumed(int count, HitContext context)
    {
        context.Damages[DamageType.Darkness].DamageNumber = (float)(count * BaseDamage);
    }

    public override void InternalUse()
    {
        Caller.RequestMeleeCone(new MeleeConeProperties
        {
            Angle = 160,
            AttackTime = 0.8f,
            Damage = new Damage
            {
                DamageNumber = (float)BaseDamage,
                AilmentChange = Damage.DEFAULT_AILMENT_CHANGE,
                Type = BaseType
            },
            Length = 60,
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