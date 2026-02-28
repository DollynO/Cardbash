using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities.Utility;

public class Blink : Ability
{
    public Blink(PlayerCharacter creator) : base(AbilityIds.BlinkGuid, creator)
    {
        this.DisplayName = "Blink";
        this.Description = "Teleports to the target position.";
        this.IconPath = "res://Sprites/SkillIcons/Lightning/9_Lightning_Strike.png";
        this.BaseCooldown = 1;
        this.BaseDamage = 10;
        this.BaseType = DamageType.Lightning;
    }

    public override void RoundReset()
    {
        return;
    }

    public override void InternalUse()
    {
        if (Caller.TryGetComponent(out AimComponent aimComponent))
        {
            var mousePosition = aimComponent.GetPlayerMouesPosition(400);
            if (Caller.TryGetComponent(out MoveComponent moveComponent))
            {
                moveComponent.RequestReposition(mousePosition, 0);
            }
        }
    }

    protected override void InternalUpdate()
    {
    }
}