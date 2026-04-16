using Godot;

namespace CardBase.Scripts.Abilities;

public readonly struct Hit
{
    public readonly Node Source;
    public readonly HitContext  Context;

    public Hit(Node source, HitContext context)
    {
        this.Source = source;
        this.Context = context;
    }
}

public interface IHitInterceptor
{
    bool TryBlock(in Hit hit);
}