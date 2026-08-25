using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Utility;

public class EscapeJump : Ability
{
    private CounterLeap dmgIncreaseBuff;
    private bool smokeJumpEnabled;
    private SmokeAoe smokeAoe;

    public EscapeJump(PlayerCharacter creator) : base(AbilityIds.EscapeJumpGuid, creator)
    {
        this.DisplayName = "Escape Jump";
        this.Description = "Jump backwards. Upgrade 1: leave smoke that slows enemies. Upgrade 2: boost your next attack.";
        this.IconPath = "res://Sprites/SkillIcons/Metal/17_Magnet.png";
    }

    public override void RoundReset()
    {
        smokeAoe?.Clear();
    }

    public override void InternalUse()
    {
        if (Caller.TryGetComponent(out MoveComponent moveComponent) && Caller.TryGetComponent(out AimComponent aimComponent))
        {
            var jumpOrigin = aimComponent.GetCharacterCenterPosition();
            if (smokeJumpEnabled)
            {
                GetSmokeAoe().Spawn(jumpOrigin);
            }

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

    public override void ClearAbility()
    {
        smokeAoe?.Clear();
    }

    private SmokeAoe GetSmokeAoe()
    {
        return smokeAoe ??= new SmokeAoe(Caller, GlobalAbilitySpawner, GUID, new SmokeAoeConfig
        {
            Radius = ConfigParam("smokeRadius", 120f),
            ActivationTime = ConfigParam("smokeActivationTime", 0.05f),
            Duration = ConfigParam("smokeDuration", 3f),
            Slow = ConfigParam("smokeSlow", 0.35f),
        });
    }

    protected override void ApplyUpdate1()
    {
        smokeJumpEnabled = true;
    }

    protected override void ApplyUpdate2()
    {
        dmgIncreaseBuff = new CounterLeap(Caller, Caller);
    }
}
