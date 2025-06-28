namespace CardBase.Scripts.Abilities;

public partial class FireballAbility : Ability
{
    public FireballAbility() : base("EE277E3F-A8D2-4AE8-9DE4-01B8158DD000")
    {
       this.DisplayName = "Fireball";
       this.Description = "Fireball Description";
       this.IconPath = "res://Sprites/SkillIcons/Fire/7_Fireball.png";
       this.MaxStack = 2;
    }

    protected override void InternalUpdate()
    {
        
    }
}