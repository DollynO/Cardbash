using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public partial class Projectile : Area2D, ICustomSpawnObject
{
    public const float MAX_SPEED = 1000;
    public long CreatorId { get; set; }
    public string AbilityGuid { get; set; }

    protected ProjectileStats stats;

    [Export] private Timer timer;
    [Export] private AnimatedSprite2D sprite;
    [Export] private CollisionShape2D collisionShape;
    [Export] private Area2D pullArea;
    [Export] private CollisionShape2D pullAreaShape;
    private Godot.Color pullAreaColor = new Godot.Color(0.5f, 0.5f, 0.5f, 0.1f);
    private IHitableObject _lastCollider;
    private PhysicsDirectSpaceState2D _state;
    private uint collisionMask = 1 + 4;
    
    private List<PlayerCharacter> playerInPullArea = new List<PlayerCharacter>();

    [Signal]
    public delegate void OnDestroyedEventHandler(Vector2 position, Projectile projectile);
    [Signal]
    public delegate void OnPiercingEventHandler(Vector2 position, Projectile projectile);
    [Signal]
    public delegate void OnCollisionEventHandler(Vector2 position, Projectile projectile);

    private const float UpdateTime = 0.05f;
    private float syncTime = UpdateTime;

    public void SetStats(ProjectileStats pStats)
    {
        stats = pStats;
        stats.PullRadius += stats.Caller?.StatBlock.GetStat(StatType.AddPullRadius) ?? 0;
        if (stats.PullRadius > 0)
        {
            pullArea.Visible = true;
        }

        Scale = stats.Scale;
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, stats.PullRadius * 35, pullAreaColor);
    }

    public override void _Ready()
    {
        SetMultiplayerAuthority(1);
        if (Multiplayer.IsServer())
        {
            if (stats.TimeToBeALive > 0)
            {
                timer.Start(stats.TimeToBeALive);
            }

            _state = GetWorld2D().GetDirectSpaceState();
            this.BodyEntered += OnBodyEntered;

            stats.PullStrength += stats.Caller?.StatBlock.GetStat(StatType.AddPullStrength) ?? 0;
            if (stats.CollisionMask > 0)
            {
                this.collisionMask = stats.CollisionMask;
            }
        }
        pullArea.Visible = stats.PullRadius > 0;
        if (stats.PullRadius > 0)
        {
            pullArea.BodyEntered += PullAreaOnBodyEntered;
            pullArea.BodyExited += PullAreaOnBodyExit;
            if (pullAreaShape.Shape is CircleShape2D circleShape)
            {
                circleShape.Radius = stats.PullRadius * 35;
            }
        }
        
        if (!string.IsNullOrEmpty(stats.AnimationResourcePath))
        {
            sprite.SpriteFrames = IconLoader.Instance.LoadAnimation(stats.AnimationResourcePath);
        }

        sprite.Play();
        GlobalPosition = stats.StartPosition;
        Rotation = stats.Direction.Angle();
    }

    private void OnBodyEntered(Node2D body)
    {
        switch (body)
        {
            case IHitableObject hitObject:
                HitableObjectCollided(hitObject);
                break;
            case TileMapLayer tile:
                DestroyProjectile();
                break;
        }
        
        EmitSignal(SignalName.OnCollision, Position, this);
    }

    private void PullAreaOnBodyEntered(Node2D body)
    {
        if (body is PlayerCharacter player && player.TeamId != ((PlayerCharacter)stats.Caller).TeamId)
        {
            if (!playerInPullArea.Contains(player))
            {
                playerInPullArea.Add(player);
            }
        }
    }

    private void PullAreaOnBodyExit(Node2D body)
    {
        if (body is PlayerCharacter player && playerInPullArea.Contains(player))
        {
            playerInPullArea.Remove(player);
        }
    }

    public override void _Process(double delta)
    {
        if (Multiplayer.IsServer())
        {
            var players = new  PlayerCharacter[playerInPullArea.Count];
            playerInPullArea.CopyTo(players);
            foreach (var player in playerInPullArea)
                player.MoveController.RequestDrag(this.GlobalPosition, stats.PullStrength, 0.1f);
        }

        QueueRedraw();
    }

    private Vector2 getNextPosition(ProjectileStats pStats, float delta)
    {
        switch (stats.MovementMode)
        {
            case MovementMode.STRAIGHT:
                return GlobalPosition + pStats.Direction * pStats.Speed * delta;
            case MovementMode.CURVE:
            case MovementMode.NONE:
            default:
                return Vector2.Zero;
        }
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (Multiplayer.IsServer())
        {
            var from = GlobalPosition;
            var to = getNextPosition(stats, (float)delta);
        
            var query = new PhysicsRayQueryParameters2D
            {
                From = from,
                To = to,
                CollisionMask = collisionMask,
                Exclude = new Array<Rid> { (stats.Caller).GetRid() },
            };

            var results = _state.IntersectRay(query);
            if (results.Count > 0)
            {
                var collider = (GodotObject)results["collider"];
                switch (collider)
                {
                    case TileMapLayer layer when stats.BouncingCount-- > 0:
                        stats.Direction = stats.Direction.Bounce((Vector2)results["normal"]);
                        Rotation = stats.Direction.Angle();
                        var dcDict = new Godot.Collections.Dictionary<string, Variant>
                        {
                            ["global_position"] = to,
                            ["direction"] = stats.Direction,
                            ["speed"] = stats.Speed
                        };
                        Rpc(MethodName.clientSyncPosition, dcDict);
                        break;
                    case TileMapLayer layer:
                        DestroyProjectile();
                        break;
                    case IHitableObject hitObject:
                    {
                        HitableObjectCollided(hitObject);

                        break;
                    }
                }
                
                EmitSignal(SignalName.OnCollision, Position, this);
            }
        

            if (stats.Speed != 0)
            {
                GlobalPosition = to;
            }

            if (syncTime >= UpdateTime)
            {
                syncTime = 0;
                var syncDict = new Godot.Collections.Dictionary<string, Variant>
                {
                    ["global_position"] = GlobalPosition,
                };
                Rpc(MethodName.clientSyncPosition, syncDict);
            }

            syncTime += (float)delta;
        }
    }

    /**
     * Server only.
     */
    protected virtual void HitableObjectCollided(IHitableObject hitObject)
    {
        stats.OnHit?.Invoke(hitObject, this);
        stats.PiercingCount--;
        if (stats.PiercingCount <= 0)
        {
            EmitSignal(SignalName.OnPiercing, Position, this);
            DestroyProjectile();
        }
    }

    
    /**
     * Server only.
     */
    private void _on_timer_timeout()
    {
        if (Multiplayer.IsServer())
        {
            DestroyProjectile();
        }
    }

    
    /**
     * Server only.
     */
    private void DestroyProjectile()
    {
        EmitSignal(SignalName.OnDestroyed, Position, this);
        Rpc(MethodName.destroyClientProjectile);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void destroyClientProjectile()
    {
        QueueFree();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void clientSyncPosition(Variant data)
    {
        var dict = data.AsGodotDictionary<string, Variant>();
        var globalPosition = (Vector2)dict["global_position"];
        
        this.GlobalPosition = globalPosition;
    }
}

public class ProjectileStats
{
    public Node2D? Parent;
    public string AnimationResourcePath;
    public float Speed = 0;
    public Action<IHitableObject, Projectile> OnHit;
    public Vector2 StartPosition = new (-10000, -10000);
    public Vector2 Direction;
    public float TimeToBeALive = 0;
    public PlayerCharacter Caller;
    public int PiercingCount = 0;
    public int BouncingCount = 0;
    public Vector2 Scale = Vector2.One;
    public float KnockbackForce = 0;
    public float PullStrength = 0;
    public float PullRadius = 0;
    public string CustomProjectilePath = string.Empty;
    public MovementMode MovementMode = MovementMode.STRAIGHT;
    public float Distance = -1;
    public uint CollisionMask;
    public float AngleOffset = 0;

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        var dict = new Godot.Collections.Dictionary<string, Variant>()
        {
            { nameof(Parent), Parent?.GetPath() ?? string.Empty },
            {nameof(AnimationResourcePath), AnimationResourcePath},
            { nameof(Speed), Speed },
            { nameof(StartPosition), StartPosition },
            { nameof(Direction), Direction },
            { nameof(TimeToBeALive), TimeToBeALive },
            { nameof(Caller), Caller.PlayerId },
            { nameof(PiercingCount), PiercingCount },
            { nameof(BouncingCount), BouncingCount },
            { nameof(Scale), Scale },
            { nameof(KnockbackForce), KnockbackForce },
            { nameof(PullRadius), PullRadius },
            { nameof(PullStrength), PullStrength },
            { nameof(CustomProjectilePath), CustomProjectilePath },
            { nameof(MovementMode), (int)MovementMode },
            { nameof(Distance), Distance },
            { nameof(CollisionMask), CollisionMask },
            { nameof(AngleOffset), AngleOffset },
        };
        return dict;
    }

    public static ProjectileStats FromDict(Godot.Collections.Dictionary<string, Variant> dict, GameManager manager)
    {
        var parentString = (string)dict[nameof(Parent)];
        var stats = new ProjectileStats()
        {
            AngleOffset = (float)dict[nameof(AngleOffset)],
            CollisionMask = (uint)dict[nameof(CollisionMask)],
            Distance = (float)dict[nameof(Distance)],
            MovementMode = (MovementMode)(int)dict[nameof(MovementMode)],
            CustomProjectilePath = (string)dict[nameof(CustomProjectilePath)],
            PullStrength = (float)dict[nameof(PullStrength)],
            PullRadius = (float)dict[nameof(PullRadius)],
            KnockbackForce = (float)dict[nameof(KnockbackForce)],
            Scale = (Vector2)dict[nameof(Scale)],
            BouncingCount = (int)dict[nameof(BouncingCount)],
            PiercingCount = (int)dict[nameof(PiercingCount)],
            Caller = manager.GetPlayerCharacter((long)dict[nameof(Caller)]),
            TimeToBeALive = (float)dict[nameof(TimeToBeALive)],
            Direction = (Vector2)dict[nameof(Direction)],
            StartPosition = (Vector2)dict[nameof(StartPosition)],
            Speed = (float)dict[nameof(Speed)],
            AnimationResourcePath = (string)dict[nameof(AnimationResourcePath)],
            Parent = !string.IsNullOrEmpty(parentString) ? (Node2D)manager.GetNode((string)dict[nameof(Parent)]) : null
        };
        return stats;
    }
}

public enum MovementMode
{
    NONE,
    STRAIGHT,
    CURVE
}