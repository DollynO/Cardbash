using System;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public record ProjectileStats
{
    public float Speed;
    public Dictionary<DamageType, float> Damage;
    public Vector2 StartPosition;
    public Vector2? Direction;
    public float TimeToBeALive;
    public GodotObject Caller;
    public string SpritePath;
    public int PiercingCount;
    public int BouncingCount;
}

public partial class Projectile : Node2D, ICustomSpawnObject
{
    public const float MAX_SPEED = 1000;
    public long CreatorId { get; set; }
    public string AbilityGuid { get; set; }

    ProjectileStats stats;

    [Export] private Timer timer;
    [Export] private Sprite2D sprite;
    [Export] private CollisionShape2D collisionShape;
    private IHitableObject _lastCollider;
    private PhysicsDirectSpaceState2D _state;
    private uint collisionMask = 1 + 2;

    [Signal]
    public delegate void OnDestroyedEventHandler(Vector2 position);

    [Signal]
    public delegate void OnPiercingEventHandler(Vector2 position);
    
    [Signal]
    public delegate void OnCollisionEventHandler(Vector2 position);

    public void SetStats(ProjectileStats pStats)
    {
        stats = pStats;
        sprite.Texture = GD.Load<Texture2D>(stats.SpritePath);
    }
    
    public override void _Ready()
    {
        timer.Start(stats.TimeToBeALive);
        SetMultiplayerAuthority(1);
        _state = GetWorld2D().GetDirectSpaceState();
    }
    
    public override void _PhysicsProcess(double delta)
    {
        var from = GlobalPosition;
        var to = GlobalPosition +  (stats.Direction ?? Vector2.Zero) * stats.Speed * (float)delta;

        if (Multiplayer.IsServer())
        {

            var query = new PhysicsRayQueryParameters2D
            {
                From = from,
                To = to,
                CollisionMask = collisionMask,
                Exclude = new Array<Rid> { ((PlayerCharacter)stats.Caller).GetRid() },
            };

            var results = _state.IntersectRay(query);
            if (results.Count > 0)
            {
                var collider = (GodotObject)results["collider"];
                switch (collider)
                {
                    case TileMapLayer layer when stats.BouncingCount-- > 0:
                        stats.Direction = (stats.Direction ?? Vector2.Zero).Bounce((Vector2)results["normal"]);
                        Rpc(MethodName.SyncDirectionChange, to, (stats.Direction ?? Vector2.Zero));
                        break;
                    case TileMapLayer layer:
                        DestroyProjectile();
                        break;
                    case IHitableObject hitObject:
                    {
                        var attacker = (PlayerCharacter)stats.Caller;
                        hitObject.ApplyDamage(stats.Damage, attacker);
                        if (stats.PiercingCount-- <= 0)
                        {
                            DestroyProjectile();
                        }

                        break;
                    }
                }
                GD.Print(results);
            }
        }

        GlobalPosition = to;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncDirectionChange(Vector2 pos, Vector2 dir)
    {
        this.GlobalPosition = pos;
        stats.Direction = dir;
    }

    private void _on_timer_timeout()
    {
        if (Multiplayer.IsServer())
        {
            DestroyProjectile();
        }
    }

    private void DestroyProjectile()
    {
        EmitSignal(SignalName.OnDestroyed, Position);
        QueueFree();
    }
}