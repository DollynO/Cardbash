using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts;

public partial class BuffManagerComponent : Node2D, IComponent
{
    public IEntityComponent Parent { get; private set; }
    public void SetParent(IEntityComponent component)
    {
        this.Parent = component;
    }
    private List<Buff> activeBuffs = new();
    private Dictionary<Buff, BuffIconTemplate> buffIcons = new();
    private BuffRow2D buffRow;
    private MultiplayerSpawner spawner;
    private bool processRunning;

    public override void _EnterTree()
    {
        base._EnterTree();
        Name = "BuffManagerComponent";
    }

    public override void _Ready()
    {
        Position = new Vector2(0, 0);
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
        var tmpActiveBuffs = new List<Buff>(activeBuffs);
        foreach (var buff in tmpActiveBuffs)
        {
            if (buff.OnTick((float)delta))
            {
                
                RemoveBuff(buff);
                activeBuffs.Remove(buff);
            }
            else
            {
                if (buffIcons.ContainsKey(buff))
                {
                    buffIcons[buff].UpdateTimer(buff.RemainingDuration, buff.Duration);
                    buffIcons[buff].UpdateStacks(buff.StackCount);
                }
            }
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
            buff.Target ??= Parent;
            activeBuffs.Add(buff);
            var dict = new Godot.Collections.Dictionary<string, Variant>
            {
                ["texture"] = buff.IconPath,
                ["syncGuid"] = Guid.NewGuid().ToString("N")
            };
            var node = spawner.Spawn(dict);       
            buffIcons.Add(buff, (BuffIconTemplate)node);
            buffRow.AddBuffIcon((BuffIconTemplate)node);
            buff.OnActivate();
        }
        else
        {
            existing.OnActivate();
        }
    }

    public int ConsumeBuff(Type consumeType)
    {
        if (!consumeType.IsSubclassOf(typeof(Buff)))
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

    public int CountBuff(Type consumeType)
    {
        if (!consumeType.IsSubclassOf(typeof(Buff)))
        {
            throw new ArgumentException("wrong consume type");
        }
        
        var buffs = this.activeBuffs.Where(b => b.GetType() == consumeType).ToList();
        var count = buffs.Sum(buff => buff.StackCount);
        return  count;
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