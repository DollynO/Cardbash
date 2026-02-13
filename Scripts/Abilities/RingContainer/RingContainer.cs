using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CardBase.Scripts.Abilities;

[GlobalClass]
public partial class RingContainer : Node2D
{
    private List<Ring> rings = new List<Ring>();
    private int index = 0;

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
        index++;
        ring.Name = generateName(index);
        ring.Index = index;
        rings.Add(ring);
        Rpc(MethodName.addRingClient, index, radius, speed, stackCount);
        AddChild(ring);

        return ring;
    }

    private string generateName(int number)
    {
        return $"ring_{number}";
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void addRingClient(int index, float radius, float speed, int stackCount)
    {
        var ring = new Ring()
        {
            Radius = radius,
            RotationSpeed = speed,
            MaxStacks = stackCount
        };
        ring.Index = index;
        
        ring.Name = generateName(index);
        rings.Add(ring);
        AddChild(ring);  
    }

    public void RemoveRing(Ring ring)
    {
        if (rings.Contains(ring))
        {
            
            rings.Remove(ring);
            Rpc(MethodName.removeRingClient, ring.Index);
            ring.QueueFree();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,  CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void removeRingClient(int number)
    {
        var ring = rings.FirstOrDefault(r => r.Name == generateName(number));
        if (ring != null)
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