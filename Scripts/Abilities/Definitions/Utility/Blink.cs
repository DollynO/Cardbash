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
    }

    public override void RoundReset()
    {
        return;
    }

    public override void InternalUse()
    {
        if (Caller.TryGetComponent(out AimComponent aimComponent))
        {
            var mousePosition = aimComponent.GetPlayerMouesPosition(ConfigParam("range", 400f));
            if (Caller.TryGetComponent(out MoveComponent moveComponent))
            {
                moveComponent.RequestReposition(mousePosition, ConfigParam("repositionDelay", 0f));
            }
        }
    }


    protected override void ApplyUpdate1()
    {
    }

    protected override void ApplyUpdate2()
    {
    }
}
