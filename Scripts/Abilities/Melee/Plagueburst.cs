using CardBase.Scripts.Abilities.Buffs.DoTs;
using CardBase.Scripts.Abilities.HitMods;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class Plagueburst : Ability
{
    public Plagueburst(PlayerCharacter creator) : base(AbilityIds.PlagueburstGuid, creator)
    {
        this.DisplayName = "Plagueburst";
        this.Description = "Bursts all corruption and agony buffs to cause damage.";
        this.IconPath = "res://Sprites/SkillIcons/Dark/15_Rod_of_Darkness.png";
        this.BaseCooldown = 5;
        this.BaseDamage = 10;
        this.BaseType = DamageType.Darkness;
        this._hitModifiers.Add(new ConsumeBuffHitModifier(onBuffConsumed, typeof(Corruption), true));
        this._hitModifiers.Add(new ConsumeBuffHitModifier(onBuffConsumed, typeof(Agony), true));
    }

    private void onBuffConsumed(int count, HitContext context)
    {
        context.Damages[DamageType.Darkness].DamageNumber += (float)(count * BaseDamage);
    }

    public override void InternalUse()
    {
        Caller.RequestMeleeCone(new MeleeConeProperties
        {
            Angle = 360,
            AttackTime = 0.2f,
            Damage = new Damage
            {
                DamageNumber = (float)BaseDamage,
                AilmentChange = Damage.DEFAULT_AILMENT_CHANGE,
                Type = BaseType
            },
            Length = 100,
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
        return;
    }
}