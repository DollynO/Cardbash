using Godot;

namespace CardBase.Scripts;

public partial class OverHeadUiComponent : Node, IComponent
{
    public IEntityComponent Parent { get; private set; }
    public void SetParent(IEntityComponent component)
    {
        this.Parent = component;
    }
}