using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts;

public partial class StatblockComponent : Node2D, IComponent
{
    public IEntityComponent Parent { get; set; }
    public void SetParent(IEntityComponent component)
    {
        this.Parent = component;
    }
    
    public Dictionary ReplicatedCurrent = new();
    public Dictionary DamageModsReplicated = new();

    private readonly StatBlock _stats = new();
    
    
    public readonly List<DamageModifier> DamageModifier = new();
    public void AddDamageModifier(DamageModifier  modifier)
    {
        DamageModifier.Add(modifier);
    }

    public override void _Ready()
    {
        Name = "StatblockComponent";
    }

    public void Define(StatType stat, float baseValue, float? minValue = null, float? maxValue = null)
    {
        if (!Multiplayer.IsServer()) return;
        
        _stats.Define(stat, baseValue, minValue, maxValue);
        var dict = new Godot.Collections.Dictionary<int, float>()
        {
            {(int)stat, baseValue}
        };
        Rpc(MethodName.updateStat, dict);
    }

    public float GetStat(StatType stat)
    {
        if (!ReplicatedCurrent.ContainsKey((int)stat)) return 0;
        
        return (float)ReplicatedCurrent[(int)stat];
    }
    
    public void AddModifiers(StatModifier modifier)
    {
        if (!Multiplayer.IsServer()) return;

        var affectedKeys = _stats.AddSourceMods(
            modifier.SourceId,
            modifier.Stat, 
            new []{(modifier.Op, modifier.Value)});
        var dict = new Godot.Collections.Dictionary<int, float>(affectedKeys.ToDictionary(kvp => (int)kvp.Key, kvp => kvp.Value));
        Rpc(MethodName.updateStat, dict);
    }
    
    public void RemoveModifierSource(string sourceId)
    {
        if (!Multiplayer.IsServer()) return;

        var affectedKeys = _stats.RemoveSource(sourceId);
        var dict = new Godot.Collections.Dictionary<int, float>(affectedKeys.ToDictionary(kvp => (int)kvp.Key, kvp => kvp.Value));
        Rpc(MethodName.updateStat, dict);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void updateStat(Godot.Collections.Dictionary<int, float> stats)
    {
        foreach (var (key, value) in stats)
        {
            if (ReplicatedCurrent.ContainsKey(key))
            {
                ReplicatedCurrent[key] = value;
            }
            else
            {
                ReplicatedCurrent.Add(key, value);
            }
        }
    }

    public string GetStatDebugText()
    {
        var sb = new StringBuilder();
        foreach (var value in Enum.GetValues<StatType>())
        {
            if (ReplicatedCurrent.ContainsKey((int)value))
            {
                sb.AppendLine($"{value} : {ReplicatedCurrent[(int)value]}");
            }
        }
        
        return sb.ToString();
    }
}