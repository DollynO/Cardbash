using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities.Buffs;

[GlobalClass]
public partial class BuffManagerComponent : Node2D
{
    private List<Buff> activeBuffs = new();
    private System.Collections.Generic.Dictionary<Buff, BuffIconTemplate> buffIcons = new();
    private System.Collections.Generic.Dictionary<string, Buff> queuedBuffs = new();
    private BuffRow2D buffRow;
    private MultiplayerSpawner spawner;
    private bool processRunning;


    public override void _Ready()
    {
        if (!Multiplayer.IsServer())
        {
            SetProcess(false);
        }
        
        buffRow = new BuffRow2D();
        buffRow.Name = "buffRow";
        AddChild(buffRow);
        buffRow.Position = new Vector2(0, -90);
        
        spawner = new MultiplayerSpawner();
        AddChild(spawner);
        spawner.SpawnPath = buffRow.GetPath();
        spawner.SpawnFunction = new Callable(this, MethodName.CustomSpawner);
        spawner.Spawned += OnIconSpawned;
    }

    public override void _Process(double delta)
    {
        var expiredBuffs = new List<Buff>();
        foreach (var buff in activeBuffs)
        {
            if (buff.OnTick((float)delta))
            {
                expiredBuffs.Add(buff);
            }
            else
            {
                buffIcons[buff].UpdateTimer(buff.RemainingDuration, buff.Duration);
            }
        }

        foreach (var buff in expiredBuffs)
        {
            RemoveBuff(buff);
            activeBuffs.Remove(buff);
        }
    }

    private void RemoveBuff(Buff buff)
    {
        buff.OnDeactivate();
        if (buffIcons.ContainsKey(buff))
        {
            buffRow.RemoveBuffIcon(buffIcons[buff]);
            buffIcons.Remove(buff);
        }
    }

    public void ApplyBuff(Buff buff)
    {
        var existing = activeBuffs
            .FirstOrDefault(b => b.GetType() == buff.GetType() && b.Caller == buff.Caller);
        if (existing == null)
        {
            activeBuffs.Add(buff);
            var dict = new Godot.Collections.Dictionary<string, Variant>
            {
                ["texture"] = buff.IconPath,
                ["syncGuid"] = Guid.NewGuid().ToString("N")
            };
            queuedBuffs.Add((string)dict["syncGuid"], buff);
            var node = spawner.Spawn(dict);       
            buffIcons.Add(queuedBuffs[node.Name], (BuffIconTemplate)node);
            queuedBuffs.Remove(node.Name);
            buffRow.AddBuffIcon((BuffIconTemplate)node);

        }
        
        buff.OnActivate();
    }

    public int ConsumeBuff(Type consumeType)
    {
        if (consumeType.BaseType != typeof(Buff))
        {
            throw new ArgumentException("wrong consume type");
        }
        
        var buffCounts = this.activeBuffs.Where(b => b.GetType() == consumeType).ToList();
        var count = buffCounts.Count;
        foreach (var buff in buffCounts)
        {
            buff.RemainingDuration = 0;
        }

        return count;  
    }
    
    public int ConsumeBuffType(DamageType type)
    {
        var buffCounts = this.activeBuffs.Where(b => b.BuffType == type).ToList();
        var count = buffCounts.Count;
        foreach (var buff in buffCounts)
        {
            buff.RemainingDuration = 0;
        }

        return count;  
    }
    
    private Node CustomSpawner(Variant data)
    {
        var dic = data.AsGodotDictionary<string, Variant>();
        var node = new BuffIconTemplate();
        node.SetBuff(IconLoader.Instance.LoadImage((string)dic["texture"]));
        node.Name = (string)dic["syncGuid"];
        node.SetMultiplayerAuthority(1);
       return node;
    }
    
    private void OnIconSpawned(Node node)
    {
        buffRow.AddBuffIcon((BuffIconTemplate)node);
    }

    public void ClearAllBuffs()
    {
        if (!Multiplayer.IsServer())
            return;
        
        SetProcess(false);

        foreach (var activeBuff in activeBuffs)
        {
            RemoveBuff(activeBuff);
        }

        activeBuffs.Clear();
        
        SetProcess(true);
    }
}
