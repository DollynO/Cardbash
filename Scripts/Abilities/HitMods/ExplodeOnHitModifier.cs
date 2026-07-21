using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities.HitMods;

public class ExplodeOnHitModifier : IHitModifier
{
    const float EXPLODE_BASE_RANGE = 50;
    const float  EXPLODE_BASE_DAMAGE = 50;
    private ExplodeStats _explodeStats;
    private IEntityComponent _caller;
    private string _guid;
    private readonly List<AoeBase> activeAoes = new();
    
    public ExplodeOnHitModifier(IEntityComponent caller, string guid)
    {
        this._explodeStats = new ExplodeStats();
        this._caller = caller;
        this._guid = string.Empty; //guid;
    }
    
    public void ApplyBefore(HitContext ctx)
    {
    }

    public void ApplyAfter(HitContext ctx)
    {
        update_explode_stats();
        if (ctx.Source is Node2D sourceNode)
        {
            var spawner = sourceNode.GetTree().Root
                .GetNode<GlobalAbilitySpawner>("/root/Main/Game/GlobalAbilitySpawner");
            var aoe = spawner.SpawnAoe(new AoeBaseStats()
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

            if (aoe != null)
            {
                activeAoes.Add(aoe);
            }
        }
    }

    public void CancelActiveAoes()
    {
        foreach (var aoe in activeAoes)
        {
            if (GodotObject.IsInstanceValid(aoe))
            {
                aoe.Cancel();
            }
        }

        activeAoes.Clear();
    }

    void update_explode_stats()
    {        
        
        _explodeStats.Damage = EXPLODE_BASE_DAMAGE;
        _explodeStats.Range = EXPLODE_BASE_RANGE;
        
        if (_caller.TryGetComponent(out StatblockComponent sbc))
        {
            _explodeStats.Range *= (1 + sbc.GetStat(StatType.IncreasedAOERange));
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

public class ExplodeStats
{
    public float Range { get; set; }
    public float Damage { get; set; }
}
