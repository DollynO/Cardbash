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
            activeBuffs.Remove(buff);
            if (buffIcons.ContainsKey(buff))
            {
                buffRow.RemoveBuffIcon(buffIcons[buff]);
                buffIcons.Remove(buff);
            }
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
}
