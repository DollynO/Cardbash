using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using CardBase.Scripts.Abilities.Buffs;
using Godot;

namespace CardBase.Scripts.Abilities;

public class Agony : ProjectileAbility
{
    private readonly List<Projectile> activeProjectiles = new();
    private readonly List<AoeBase> activeAoes = new();

    public Agony(PlayerCharacter creator) : base(AbilityIds.AgnoyAbilitGuid, creator)
    {
        this.DisplayName = "Agony";
        this.Description = "Applies Agony debuff.";
        this.IconPath = "res://Sprites/SkillIcons/Dark/7_Black_Label.png";
    }

    public override void RoundReset()
    {
        CancelRuntimeObjects();
    }

    public override void ClearAbility()
    {
        CancelRuntimeObjects();
    }

    protected override void PostSpawnProjectile(Projectile projectile)
    {
        activeProjectiles.Add(projectile);
    }

    protected override void _onProjectileDestroyed(Vector2 position, Projectile projectile)
    {
        activeProjectiles.Remove(projectile);
    }


    protected override void ApplyUpdate1()
    {
    }

    protected override void ApplyUpdate2()
    {
    }

    private void onHit(IEntityComponent ec, Projectile source)
    {
        if (ec.TryGetComponent<BuffManagerComponent>(out var buffManager))
        {
            ApplyAgony(buffManager, ec);
        }

        if (UpdateCounter != 2) return;
        
        var stats = new AoeBaseStats()
        {
            Angle = 360f,
            ActivationTime = ConfigParam("activationTimeWildfire", 1f),
            Radius = ConfigParam("radiusWildfire", 300f),
            AngleOffset = ConfigParam("angleOffsetWildfire", 0f),
            Owner = Caller,
            Callbacks = new AoeBaseCallbacks
            {
                OnActivation = OnActivation,
                OnDeactivation = (_, aoe) => activeAoes.Remove(aoe),
            },
        };
        ApplyAoeConfig(stats);
        var aoe = GlobalAbilitySpawner.SpawnAoe(stats);
        if (aoe != null)
        {
            activeAoes.Add(aoe);
        }
    }

    private void OnActivation(List<IEntityComponent> arg1, AoeBase arg2)
    {
        foreach (var ec in arg1)
        {
            if (ec.TryGetComponent<BuffManagerComponent>(out var buffManager))
            {
                ApplyAgony(buffManager, ec);
            }
        }
    }

    private void ApplyAgony(BuffManagerComponent buffManager, IEntityComponent target)
    {
        var stackCount = UpdateCounter >= 1 ? 4 : 1;
        for (var i = 0; i < stackCount; i++)
        {
            buffManager.ApplyBuff(new CardBase.Scripts.Abilities.Buffs.DoTs.Agony(Caller, target));
        }
    }

    protected override ProjectileRuntime GetProjectileRuntime()
    {
        return CreateProjectileRuntime(onHit);
    }

    protected override ProjectileSpawnRequest GetProjectileSpawnRequest()
    {
        var request = AimedProjectile(
            "res://AnimationRes/Projectile/MagicMissile/MM_LargeViolet.tres",
            ConfigParam("projectileSpeed", 300f),
            ConfigParam("projectileLifetime", 4f));
        ApplyProjectileConfig(request);
        return request;
    }

    private void CancelRuntimeObjects()
    {
        foreach (var projectile in new List<Projectile>(activeProjectiles))
        {
            if (GodotObject.IsInstanceValid(projectile))
            {
                projectile.DestroyProjectile();
            }
        }
        activeProjectiles.Clear();

        foreach (var aoe in new List<AoeBase>(activeAoes))
        {
            if (GodotObject.IsInstanceValid(aoe))
            {
                aoe.Cancel();
            }
        }
        activeAoes.Clear();
    }
    
    
}
