using System.Collections.Generic;
using Godot;

namespace CardBase.Scripts.Abilities.HitMods;

public class ExplodeOnHitModifier : IHitModifier
{
    private ExplodeStats _explodeStats;
    private IEntityComponent _caller;
    private string _guid;
    
    public ExplodeOnHitModifier(ExplodeStats stats, IEntityComponent caller, string guid)
    {
        this._explodeStats = stats;
        this._caller = caller;
        this._guid = guid;
    }
    
    public void ApplyBefore(HitContext ctx)
    {
    }

    public void ApplyAfter(HitContext ctx)
    {
        if (ctx.Source is Node2D sourceNode)
        {
            var spawner = sourceNode.GetTree().Root
                .GetNode<GlobalAbilitySpawner>("/root/Main/Game/GlobalAbilitySpawner");
            spawner.SpawnAoe(new AoeBaseStats()
            {
                AbilityGUID = ctx.AbilityGuid,
                ActivationTime = 3,
                Radius = this._explodeStats.Range,
                Owner =  ctx.Source,
                IsStationary = true,
                StationaryPosition = ((Node2D)ctx.Target).GlobalPosition,
                Callbacks = new AoeBaseCallbacks()
                {
                    OnActivation = OnActivation,
                }
            });
        }
    }

    private void OnActivation(List<IEntityComponent> obj, AoeBase aoeBase)
    {
        var dict = new Dictionary<DamageType, Damage>();
        var damage = new Damage()
        {
            AilmentChance = 0,
            Type = DamageType.Fire,
            DamageNumber = _explodeStats.Damage,
        };
        dict.Add(DamageType.Fire, damage);

        foreach (var entity in obj)
        {
            if (_caller is not ITeamAffiliation callerTeam || entity is not ITeamAffiliation targetTeam)
            {
                continue;
            }

            if (targetTeam.TeamId != callerTeam.TeamId)
            {
                var hitContext = new HitContext
                {
                    Source = _caller,
                    Target = entity,
                    AbilityGuid = _guid,
                    Damages = dict
                };
                var hit = new Hit(aoeBase, hitContext);
                if (entity.TryGetComponent(out DamageAbleComponent dac))
                {
                    dac.ReceiveHit(hit);
                }
            }
        }
    }
}

public class ExplodeStats
{
    public float Range { get; set; }
    public float Damage { get; set; }
}