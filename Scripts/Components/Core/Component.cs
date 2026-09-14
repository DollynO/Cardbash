using System;
using Godot;

namespace CardBase.Scripts;

public partial class Component : Node2D, IComponent
{
    public IEntityComponent Parent { get; set; }
    public void SetParent(IEntityComponent component)
    {
        this.Parent = component ?? throw new NullReferenceException();
    }

    public override void _EnterTree()
    {
        SetPhysicsProcess(false);
        SetProcess(false);
    }
}