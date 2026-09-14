using System;

namespace CardBase.Scripts;

public interface IEntityComponent
{
    public EventBus EventBus { get; }
    public bool TryGetComponent<T>(out T component) where T : IComponent;
    public void AddComponent(IComponent component);
    public void RemoveComponent(Type type);
}

public interface IComponent
{
    public IEntityComponent Parent { get; set; }

    public void SetParent(IEntityComponent component)
    {
        Parent = component;
    }
}
