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

        corruption = new Corruption(Caller, null);

    }

    public override void RoundReset()
    {
        return;
    }


    protected override void ApplyUpdate1()
    {
    }

    protected override void ApplyUpdate2()
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
        var request = AimedProjectile(
            "res://AnimationRes/Projectile/L_LargeViolet.tres",
            ConfigParam("projectileSpeed", 300f),
            ConfigParam("projectileLifetime", 4f));
        ApplyProjectileConfig(request);
        return request;
    }
}
