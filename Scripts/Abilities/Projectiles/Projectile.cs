using System;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public class ProjectileStats
{
    public float Speed;
    public Damage Damage;
    public Vector2 StartPosition;
    public Vector2? Direction;
    public float TimeToBeALive;
    public GodotObject Caller;
    public string SpritePath;
    public int PiercingCount;
    public int BouncingCount;
    public Vector2 Scale;
    public Vector3 Color;

    public Dictionary<string, Variant> ToDict()
    {
        return new Dictionary<string, Variant>()
        {
            { nameof(Speed), Speed },
            { nameof(Damage), Damage.ToDict() },
            { nameof(StartPosition), ((PlayerCharacter)Caller).GetProjectileStartPosition()},
            { nameof(Direction), Direction ?? ((PlayerCharacter)Caller).GetLookAtDirection() },
            { nameof(TimeToBeALive), TimeToBeALive },
            { nameof(Caller), ((PlayerCharacter)Caller).PlayerId },
            { nameof(SpritePath), SpritePath },
            { nameof(PiercingCount), PiercingCount },
            { nameof(BouncingCount), BouncingCount },
            { nameof(Scale), Scale },
            { nameof(Color), Color },
        };
    }

    public static ProjectileStats FromDict(Dictionary<string, Variant> dict, GameManager gameManager)
    {
        return new ProjectileStats()
        {
            Speed = Math.Clamp((float)dict[nameof(Speed)], 0, Projectile.MAX_SPEED),
            Damage = Damage.FromDict((Dictionary<string, Variant>)dict[nameof(Damage)]),
            StartPosition = (Vector2)dict[nameof(StartPosition)],
            Direction = (Vector2?)dict[nameof(Direction)],
            TimeToBeALive = (float)dict[nameof(TimeToBeALive)],
            Caller = gameManager.GetPlayerCharacter((long)dict[nameof(Caller)]),
            SpritePath = (string)dict[nameof(SpritePath)],
            PiercingCount = (int)dict[nameof(PiercingCount)],
            BouncingCount = (int)dict[nameof(BouncingCount)],
            Scale = (Vector2)dict[nameof(Scale)],
            Color = (Vector3)dict[nameof(Color)],
        };
    }
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
    private uint collisionMask = 1 + 4;

    [Signal]
    public delegate void OnDestroyedEventHandler(Vector2 position);

    [Signal]
    public delegate void OnPiercingEventHandler(Vector2 position);
    
    [Signal]
    public delegate void OnCollisionEventHandler(Vector2 position);

    public void SetStats(ProjectileStats pStats)
    {
        stats = pStats;
        Scale = stats.Scale;
        Modulate = new Color(stats.Color.X, stats.Color.Y, stats.Color.Z);
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
                        var ctx = new HitContext();
                        ctx.Source = (PlayerCharacter)stats.Caller;
                        ctx.Target = hitObject as PlayerCharacter;
                        ctx.Damages = new System.Collections.Generic.Dictionary<DamageType, Damage>
                        {
                            { stats.Damage.Type, stats.Damage }
                        };
                        ctx.AbilityGuid = AbilityGuid;
                        hitObject.ApplyDamage(ctx);
                        if (stats.PiercingCount-- <= 0)
                        {
                            DestroyProjectile();
                        }

                        break;
                    }
                }
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