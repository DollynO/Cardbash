using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities.Utility;

public class Blink : Ability
{
    public Blink(PlayerCharacter creator) : base("ECD03AF9-850B-457C-A799-2D9E99D29B7F", creator)
    {
        this.DisplayName = "Blink";
        this.Description = "Teleports to the target position.";
        this.IconPath = "res://Sprites/SkillIcons/Lightning/9_Lightning_Strike.png";
        this.BaseCooldown = 1;
        this.BaseDamage = 10;
        this.BaseType = DamageType.Lightning;
    }

    public override void InternalUse()
    {
        var mousePosition = this.Caller.GetGlobalMousePosition();
        this.Caller.RequestReposition(mousePosition);
    }

    protected override void InternalUpdate()
    {
    }

    public override void RegisterSpawnedNode(Node node)
    {
    }
}