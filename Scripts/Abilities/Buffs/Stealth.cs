using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;

public class Stealth : Buff
{
    public Stealth(IEntityComponent caller, IEntityComponent target) : base(caller, target)
    {
        this.Description = "Get invisible. Upgrade 1: increase movement speed. Upgrade 2:";
        this.DisplayName = "Stealth";
        this.IconPath = "res://Sprites/SkillIcons/Dark/16_Shadow.png";
        this.Duration = 10;
        this.Guid = "4D6D88EC-3CFC-4230-9445-05A4F782D467";
    }

    protected override void InternalOnActivate()
    {
        ((PlayerCharacter)Target).EnterStealth();
    }

    protected override void InternalOnTick(float delta)
    {
        return;
    }

    protected override void InternalOnDeactivate()
    {
        ((PlayerCharacter)Target).ExitStealth();
    }
}