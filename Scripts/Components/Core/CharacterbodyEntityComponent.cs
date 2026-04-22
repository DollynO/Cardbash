using System;
using Godot;

namespace CardBase.Scripts;

public partial class CharacterbodyEntityComponent : CharacterBody2D, IEntityComponent
{
    private System.Collections.Generic.Dictionary<Type, IComponent> components = new();
    public bool TryGetComponent<T>(out T component) where T : IComponent
    {
        if (components.TryGetValue(typeof(T), out var cmp))
        {
            component = (T)cmp;
            return true;
        }

        component = default;
        return false;
    }

    public void AddComponent(IComponent component)
    {
        component.SetParent(this);
        components.Add(component.GetType(), component);
        if (component is Node node)
        {
            this.AddChild(node);
        }
    }

    public void RemoveComponent(Type type)
    {
        if (components.TryGetValue(type, out var cmp))
        {
            if (cmp is Node node)
            {
                RemoveChild(node);
            }
            components.Remove(type);
        }
    }
}