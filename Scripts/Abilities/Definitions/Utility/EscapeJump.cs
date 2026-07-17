using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities.Utility;

public class EscapeJump : Ability
{
    private CounterLeap dmgIncreaseBuff;
    public EscapeJump(PlayerCharacter creator) : base(AbilityIds.EscapeJumpGuid, creator)
    {
        this.DisplayName = "Escape Jump";
        this.Description = "Jump backwards.";
        this.IconPath = "res://Sprites/SkillIcons/Metal/17_Magnet.png";
    }

    public override void RoundReset()
    {
        return;
    }

    public override void InternalUse()
    {
        if (Caller.TryGetComponent(out MoveComponent moveComponent) && Caller.TryGetComponent(out AimComponent aimComponent))
        {
            moveComponent.Knockback(
                aimComponent.GetProjectileStartPosition(),
                ConfigParam("strength", 1000f),
                ConfigParam("duration", 1.0f));

            if (dmgIncreaseBuff != null && Caller.TryGetComponent(out BuffManagerComponent bmc))
            {
                bmc.ApplyBuff(dmgIncreaseBuff);
            }
        }
    }


    protected override void ApplyUpdate1()
    {
    }

    protected override void ApplyUpdate2()
    {
        dmgIncreaseBuff = new CounterLeap(Caller, Caller);
    }
}
