using System;
using CardBase.Scripts.GameSettings;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

[GlobalClass]
public partial class GlobalAbilitySpawner : Node
{
    [Export] private GameManager GameManager;
    public Ray SpawnRay(RayStats props)
    {
        var ray = new Ray(props);
        var name = generateName(SpawnType.RAY);
        ray.Name = name;
        props.Caster.GetCharacterCenterPoint().AddChild(ray);
        Rpc(MethodName.spawnRayOnClient, props.ToDict(), name);
        return ray;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void spawnRayOnClient(Variant dict, string name)
    {
        var props = RayStats.FromDict((Dictionary<string, Variant>)dict, GameManager);
        var ray = new Ray(props);
        ray.Name = name;
        props.Caster.GetCharacterCenterPoint().AddChild(ray);
    }

    public AoeBase SpawnAoe(AoeBaseStats aoeStats)
    {
        var aoe = new AoeBase(aoeStats);
        var name = generateName(SpawnType.AOE);
        aoe.Name = name;
        var dict = aoeStats.ToDict();
        Rpc(MethodName.spawnAoeOnClient, dict, name);
        return aoe;
    }

    private void spawnAoeOnClient(Variant dict, string name)
    {
        var stats = AoeBaseStats.FromDict((Dictionary<string, Variant>)dict, GameManager);
    }

    private string generateName(SpawnType type)
    {
        return $"{nameof(type)}_{Guid.NewGuid()}";
    }
    
    
    private enum SpawnType
    {
        RAY,
        AOE,
        MELEE,
        AURA,
    }
}