using Godot;

namespace CardBase.Scripts;

public readonly record struct PlayerMoveInput(
    int Tick,
    Vector2 Direction);

public readonly record struct MovementSnapshot(
    int Tick,
    Vector2 Position,
    Vector2 Velocity,
    bool Stun,
    float StunTime,
    float MaxStunTime,
    bool Hitstun,
    bool Rooted,
    bool MovementDisabled,
    Vector2 ImpulseVector,
    Vector2 ForceVector,
    float ForceDecayTime,
    Vector2 RepositionPosition,
    bool RepositionBlock);
