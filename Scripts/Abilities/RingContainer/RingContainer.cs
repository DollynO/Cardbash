using System.Collections.Generic;
using Godot;

namespace CardBase.Scripts.Abilities;

[GlobalClass]
public partial class RingContainer : Node2D
{
    private List<Ring> rings = new List<Ring>();

    public override void _Ready()
    {
        Name = "RingContainer";
    }

    public Ring AddRing(float radius, float speed, int stackCount = 3)
    {
        var ring = new Ring()
        {
            Radius = radius,
            RotationSpeed = speed,
            MaxStacks = stackCount
        };
        
        rings.Add(ring);
        AddChild(ring);

        return ring;
    }

    public void RemoveRing(Ring ring)
    {
        if (rings.Contains(ring))
        {
            rings.Remove(ring);
            ring.QueueFree();
        }
    }

    public void ClearAllRings()
    {
        while (rings.Count > 0)
        {
         RemoveRing(rings[0]);   
        }
    }
}