using CardBase.Scripts.Abilities.Buffs.DoTs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class CorruptedBolt : ProjectileAbility
{
    private Corruption corruption;
    public CorruptedBolt(PlayerCharacter creator) : base(AbilityIds.CorruptedBoltGuid, creator)
    {
        this.DisplayName = "Corrupted Bolt";
        this.Description = "Applies Corruption debuff.";
        this.IconPath = "res://Sprites/SkillIcons/Dark/17_The power_of_darkness.png";
        this.MaxStack = 1;
        this.BaseCooldown = 10;
        this.BaseDamage = 1;
        this.BaseType = DamageType.Darkness;
        
        corruption = new Corruption(Caller,  null);

    }

    protected override void InternalUpdate()
    {
    }

    private void applyCorruption(IHitableObject hitableObject, Projectile source)
    {
        if (hitableObject is not PlayerCharacter character)
        {
            return;
        }
        
        character.BuffManagerComponent.ApplyBuff(corruption);
}
    
    protected override ProjectileStats GetProjectileStats()
    {
        return new ProjectileStats
        {
            Caller = Caller,
            Direction = Vector2.Zero,
            Speed = 300,
            TimeToBeALive = 4,
            AnimationResourcePath = "res://Sprites/Projectiles/fireBallProjectile.png",
            OnHit = applyCorruption,
        };
    }
}