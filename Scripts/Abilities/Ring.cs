using System.Collections.Generic;
using Godot;

namespace CardBase.Scripts.Abilities;

public partial class Ring : Node2D
{
    public float Radius { get; set; } = 50.0f;
    public float RotationSpeed { get; set; } = 1.0f;

    public int MaxStacks
    {
        get => maxStacks;
        set
        {
         maxStacks = value;
         angleStep = Mathf.Tau /  maxStacks;
        }
    }
    
    private int maxStacks = 3;
    
    private Dictionary<int, Node2D> anchoredNodes = new Dictionary<int, Node2D>();
    private float currentRotation = 0.0f;
    private float angleStep = Mathf.Tau / 3;
    public override void _Process(double delta)
    {
        currentRotation += RotationSpeed * (float)delta;
        Rotation = currentRotation % Mathf.Tau;

        updateNodePositions();
    }

    public void AddNode(Node2D node, bool externalParent = false)
    {
        //find next available slot
        var targetSlot = 1;
        for (var i = 0; i < MaxStacks; i++)
        {
            if (!anchoredNodes.ContainsKey(i))
            {
                targetSlot = i;
                break;
            }
        }
        
        if (targetSlot == -1)
        {
            GD.PushWarning($"Ring is at max capacity.");
            return;
        }
        
        anchoredNodes.Add(targetSlot, node);
        if (!externalParent)
        {
            this.AddChild(node);
        }
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
            anchoredNodes.Remove(slotToRemove);
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
            anchoredNodes.Remove(slotIndex);
            node.QueueFree();
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
        foreach (var anchoredNodesValue in anchoredNodes.Values)
        {
            anchoredNodesValue.QueueFree();
        }
        
        anchoredNodes.Clear();
    }

    /**
     * Updates the position of a single node given by the slot index.
     */
    private void updateNodePosition(int slotIndex)
    {
        if (!anchoredNodes.ContainsKey(slotIndex))
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