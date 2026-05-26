using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities.Utility;

public class ShadowWalk : Ability
{
    private Stealth stealth;
    private FastMovement fastMovement;

    public ShadowWalk(IEntityComponent creator) : base(AbilityIds.ShadowWalkGuid, creator)
    {
        this.Description = "Get invisible, get revealed if damage taken. Upgrade 1: increase movement speed. Upgrade 2: not revealed on damage taken";
        this.DisplayName = "Stealth walk";
        this.IconPath = "res://Sprites/SkillIcons/Dark/16_Shadow.png";
        this.BaseCooldown = 15;
        this.BaseType = DamageType.Darkness;
        
        if (creator != null)
        {
            creator.EventBus.CombatEventBus.DamageTakeEventHandler += CreatorOnDamageTaken;
            creator.EventBus.CombatEventBus.AbilityCastedEventHandler += CreatorOnAbilityCasted;
        }

        stealth = new Stealth(creator, creator)
        {
            Duration = 5
        };
        fastMovement = new FastMovement(creator, creator);
    }

    private void CreatorOnAbilityCasted(object sender, AbilityEventArgs e)
    {
        if (e.Source != Caller 
            || e.Ability.GUID == AbilityIds.ShadowWalkGuid)
        {
            return;
        }

        removeBuff();
    }

    private void CreatorOnDamageTaken(object sender, DamageEventArgs e)
    {
        if (e.Target != Caller) return;
        
        removeBuff();
    }

    public override void RoundReset()
    {
        return;
    }

    public override void ClearAbility()
    {
        if (Caller == null)
        {
            return;
        }

        Caller.EventBus.CombatEventBus.DamageTakeEventHandler -= CreatorOnDamageTaken;
        Caller.EventBus.CombatEventBus.AbilityCastedEventHandler -= CreatorOnAbilityCasted;
    }

    public override void InternalUse()
    {
        if (Caller.TryGetComponent(out BuffManagerComponent bmc))
        {
            bmc.ApplyBuff(stealth);
            if (UpdateCounter >= 1)
            {
                bmc.ApplyBuff(fastMovement);
            }
        }
    }

    protected override void InternalUpdate()
    {
        switch (UpdateCounter)
        {
            case 1:
                break;
            case 2:
                Caller.EventBus.CombatEventBus.DamageTakeEventHandler -= CreatorOnDamageTaken;
                break;
        }
    }

    private void removeBuff()
    {
        stealth.RemainingDuration = 0;
        if (UpdateCounter >= 1)
        {
            fastMovement.RemainingDuration = 0;
        }
    }
}
