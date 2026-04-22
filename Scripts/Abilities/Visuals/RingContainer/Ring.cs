using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CardBase.Scripts.Abilities;

public partial class Ring : Node2D
{
    public float Radius { get; set; } = 50.0f;
    public float RotationSpeed { get; set; } = 1.0f;
    public int Index { get; set; } = 0;

    public int MaxStacks
    {
        get => maxStacks;
        set
        {
            maxStacks = value;
            angleStep = Mathf.Tau / maxStacks;
        }
    }

    private int maxStacks = 3;

    private readonly ConcurrentDictionary<int, Node2D> anchoredNodes = new();
    private float currentRotation = 0.0f;
    private float angleStep = Mathf.Tau / 3;

    private Area2D radiusArea;
    private GlobalAbilitySpawner globalAbilitySpawner;

    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        currentRotation += RotationSpeed * (float)delta;
        Rotation = currentRotation % Mathf.Tau;

        updateNodePositions();
    }

    public void AddNode(Node2D node)
    {
        //find next available slot
        var targetSlot = getNextAvailableSlot();

        if (targetSlot == -1)
        {
            GD.PushWarning($"Ring is at max capacity.");
            return;
        }

        anchoredNodes.GetOrAdd(targetSlot, k => node);
    }

    public void AddTextureNode(string iconPath, Vector2 scale)
    {
        var spawnData = new SpawnData()
        {
            Name = GlobalAbilitySpawner.GenerateSpawnName(SpawnType.RING_TEXTURE_NODE),
            SpawnType = SpawnType.RING_TEXTURE_NODE,
            SpawnObjectData = new SpriteStats()
            {
                Parent = this,
                Scale = scale,
                TexturePath = iconPath
            }.ToDict()
        };

        var node = (globalAbilitySpawner ??= GetTree().Root
            .GetNode<GlobalAbilitySpawner>("/root/Main/Game/GlobalAbilitySpawner")).Spawn(spawnData);

        if (node is RingTextureNode ringTextureNode)
        {
            this.AddNode(ringTextureNode);
        }
    }

    private int getNextAvailableSlot()
    {
        var targetSlot = -1;
        for (var i = 0; i < MaxStacks; i++)
        {
            if (!anchoredNodes.ContainsKey(i))
            {
                targetSlot = i;
                break;
            }
        }

        return targetSlot;
    }


    public void RemoveNode(Node2D node, bool freeObject = true)
    {
        int slotToRemove = -1;
        foreach (var kvp in anchoredNodes)
        {
            if (kvp.Value == node)
            {
                slotToRemove = kvp.Key;
                break;
            }
        }

        if (slotToRemove != -1)
        {
            anchoredNodes.TryRemove(new KeyValuePair<int, Node2D>(slotToRemove, anchoredNodes[slotToRemove]));
            if (freeObject)
            {
                node.QueueFree();
            }
        }
    }

    public void RemoveNodeAtSlot(int slotIndex)
    {
        if (anchoredNodes.ContainsKey(slotIndex))
        {
            var node = anchoredNodes[slotIndex];
            anchoredNodes.TryRemove(new KeyValuePair<int, Node2D>(slotIndex, node));
            if (node is RingTextureNode ringNode)
            {
                ringNode.QueueFree();
            }
            else
            {
                node.QueueFree();
            }
        }
    }

    public int GetStackCount()
    {
        return anchoredNodes.Count;
    }

    public Node2D GetNodeAtSlot(int slotIndex)
    {
        anchoredNodes.TryGetValue(slotIndex, out var node);
        return node;
    }

    public void ClearNodes()
    {
        while (!anchoredNodes.IsEmpty)
        {
            var node = anchoredNodes.First();
            anchoredNodes.TryRemove(node);
            node.Value.QueueFree();
        }
    }

    /**
     * Updates the position of a single node given by the slot index.
     */
    private void updateNodePosition(int slotIndex)
    {
        if (!anchoredNodes.TryGetValue(slotIndex, out var anchoredNode) || !IsInstanceValid(anchoredNode))
            return;

        var angle = slotIndex * angleStep;
        var pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Radius;
        if (anchoredNodes.TryGetValue(slotIndex, out var node))
        {
            node.GlobalPosition = ToGlobal(pos);
            node.GlobalRotation = GlobalRotation + angle;
        }
    }

    /**
     * Updates the position of all nodes relative to the parent (this ring).
     */
    private void updateNodePositions()
    {
        var nodes = new int[anchoredNodes.Count];
        anchoredNodes.Keys.CopyTo(nodes, 0);

        foreach (var node in nodes)
        {
            updateNodePosition(node);
        }
    }
}