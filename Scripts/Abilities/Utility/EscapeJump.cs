using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities.Utility;

public class EscapeJump : Ability
{
    public EscapeJump(PlayerCharacter creator) : base(AbilityIds.EscapeJumpGuid, creator)
    {
        this.DisplayName = "Escape Jump";
        this.Description = "Jump backwards.";
        this.IconPath = "res://Sprites/SkillIcons/Metal/17_Magnet.png";
        this.BaseCooldown = 5;
        this.BaseDamage = 0;
        this.BaseType = DamageType.Physical;
    }

    public override void InternalUse()
    {
        if (Caller.TryGetComponent(out MoveComponent moveComponent) && Caller.TryGetComponent(out AimComponent aimComponent))
        {
            moveComponent.Knockback(
                aimComponent.GetProjectileStartPosition(), 
                1000f, 
                1.0f);
        }
    }

    protected override void InternalUpdate()
    {
        return;
    }
}