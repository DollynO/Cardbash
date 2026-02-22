using System;
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

    private void applyCorruption(IEntityComponent ec, Projectile source)
    {
        if (ec.TryGetComponent<BuffManagerComponent>(out var buffManager))
        {
            buffManager.ApplyBuff(corruption);
        }
    }
    
    protected override ProjectileStats GetProjectileStats()
    {
        var direction = Vector2.Zero;
        if (Caller.TryGetComponent(out AimComponent aimComponent))
        {
            direction = aimComponent.GetLookAtDirection();
        }
        return new ProjectileStats
        {
            Caller = Caller,
            Direction = direction,
            Speed = 300,
            TimeToBeALive = 4,
            AnimationResourcePath = "res://AnimationRes/Projectile/MagicMissile/mm_lrage_blue.tres",
            OnHit = applyCorruption,
        };
    }
}