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

        corruption = new Corruption(Caller, null);

    }

    public override void RoundReset()
    {
        return;
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

    protected override ProjectileRuntime GetProjectileRuntime()
    {
        return CreateProjectileRuntime(applyCorruption);
    }

    protected override ProjectileSpawnRequest GetProjectileSpawnRequest()
    {
        return AimedProjectile("res://AnimationRes/Projectile/MagicMissile/mm_lrage_blue.tres", 300, 4);
    }
}
