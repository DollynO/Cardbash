using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.PlayerScripts;

[GlobalClass]
public partial class StatBlockComponent : Node
{
    [Export] private MultiplayerSynchronizer sync;

    [Export]
    public Dictionary ReplicatedCurrent = new();

    private readonly StatBlock _stats = new();
    public override void _EnterTree()
    {
    }

    public override void _Ready()
    {
        sync.SetMultiplayerAuthority(1);   
    }

    public void Define(StatType stat, float baseValue, float? minValue = null, float? maxValue = null, IEnumerable<StatType> linkedStats = null)
    {
        _stats.Define(stat, baseValue, minValue, maxValue,linkedStats);
        pushCurrent();
    }

    public float GetStat(StatType stat)
    {
        return (float)ReplicatedCurrent[(int)stat];
    }
    
    public void AddModifiers(StatModifier modifier)
    {
        RpcId(1, MethodName.addModifierServer, new Dictionary()
        {
            {"sourceId", modifier.SourceId},
            {"stat", (int)modifier.Stat},
            {"op", (int)modifier.Op},
            {"value", modifier.Value},
        });
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void addModifierServer(Dictionary dict)
    {
        _stats.AddSourceMods(
            (string)dict["sourceId"],
            (StatType)(int)dict["stat"], 
            new []{((StatOp)(int)dict["op"], (float)dict["value"])});
        pushCurrent();
    }
    
    public void RemoveModifierSource(string sourceId)
    {
        _stats.RemoveSource(sourceId);
        pushCurrent();
    }

    private void pushCurrent()
    {
        ReplicatedCurrent.Clear();
        foreach (var stat in _stats.Current)
        {
            ReplicatedCurrent.Add((int)stat.Key, stat.Value);
        }
    }
    
}