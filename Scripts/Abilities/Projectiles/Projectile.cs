using System;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public record ProjectileStats
{
    public float Speed;
    public float Damage;
    public Vector2 Direction;
    public float TimeToBeALive;
    public GodotObject Caller;
    public Texture2D SpriteTexture;
}

public partial class Projectile : CharacterBody2D
{
    ProjectileStats stats;

    [Export] private Timer timer;
    [Export] private Sprite2D sprite;
    [Export] private CollisionShape2D collisionShape;
    
    [Signal]
    public delegate void OnHitEventHandler(GodotObject hitableObject);

    [Signal]
    public delegate void OnDestroyedEventHandler(Vector2 position);

    public void SetStats(ProjectileStats pStats)
    {
        stats = pStats;
        sprite.Texture = stats.SpriteTexture;
    }
    
    public override void _Ready()
    {
        timer.Start(stats.TimeToBeALive);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Multiplayer.IsServer())
        {
            var collision = MoveAndCollide(stats.Direction * stats.Speed * (float)delta);
            if (collision != null && collision.GetCollider() != stats.Caller)
            {
                if (collision.GetCollider() is IHitableObject hitObject)
                {
                    var attacker = stats.Caller as PlayerCharacter;
                    hitObject.ApplyDamage(stats.Damage, attacker);
                }
                GD.Print("I collided with ", ((Node)collision.GetCollider()).Name);
                DestroyProjectile();
            }
        }
    }

    [Rpc]
    private void SyncPosition(Vector2 pos)
    {
        this.GlobalPosition = pos;
    }

    private void _on_timer_timeout()
    {
        DestroyProjectile();
    }

    private void DestroyProjectile()
    {
        EmitSignal(SignalName.OnDestroyed, Position);
        QueueFree();
    }
}