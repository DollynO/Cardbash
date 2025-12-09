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
    [Export] private Sprite2D sprite;
    [Export] private CollisionShape2D collisionShape;
    [Export] private Area2D pullArea;
    [Export] private CollisionShape2D pullAreaShape;
    private Godot.Color pullAreaColor = new Godot.Color(0.5f, 0.5f, 0.5f, 0.1f);
    private IHitableObject _lastCollider;
    private PhysicsDirectSpaceState2D _state;
    private uint collisionMask = 1 + 4;
    
    private List<PlayerCharacter> playerInPullArea = new List<PlayerCharacter>();

    [Signal]
    public delegate void OnDestroyedEventHandler(Vector2 position);
    [Signal]
    public delegate void OnPiercingEventHandler(Vector2 position);
    [Signal]
    public delegate void OnCollisionEventHandler(Vector2 position);
    
    private Vector2 syncDirection = Vector2.Zero;
    private float syncSpeed = 0;
    private int syncCounter = 0;
    private float theta = 0;
    public float Theta => theta;

    public void SetStats(ProjectileStats pStats)
    {
        stats = pStats;
        theta = pStats.AngleOffset;
        
        stats.PullRadius += stats.Caller?.StatBlock.GetStat(StatType.AddPullRadius) ?? 0;
        stats.PullStrength += stats.Caller?.StatBlock.GetStat(StatType.AddPullStrength) ?? 0;
        
        Scale = stats.Scale;
        Modulate = new Godot.Color(stats.Color.X, stats.Color.Y, stats.Color.Z);
        
        if (pStats.CustomCollisionMask > 0)
        {
            this.collisionMask = pStats.CustomCollisionMask;
        }
        
        if (!(stats.PullRadius > 0) || !(stats.PullStrength > 0))
        {
            return;
        }

        pullArea.Visible = true;
        if (Multiplayer.IsServer())
        {
            pullArea.BodyEntered += PullAreaOnBodyEntered;
            pullArea.BodyExited += PullAreaOnBodyExit;
            if (pullAreaShape.Shape is CircleShape2D circleShape)
            {
                circleShape.Radius = stats.PullRadius * 35;
            }
        }
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
        }

        pullArea.Visible = false;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is IHitableObject hitObject)
        {
            onHitableObjectCollided(hitObject);
        }
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
            case MovementMode.Straight:
                return GlobalPosition + pStats.Direction * pStats.Speed * delta;
            case MovementMode.Orbit:
                theta += pStats.Speed * delta;
                var offset = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * pStats.Distance;
                return stats.Caller.GetCharacterCenterPosition() + offset;
            case MovementMode.Curve:
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
            
            if (Multiplayer.IsServer())
            {

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
                            onHitableObjectCollided(hitObject);
                            break;
                        }
                    }
                }
            }

            GlobalPosition = to;
            var syncDict = new Godot.Collections.Dictionary<string, Variant>
            {
                ["global_position"] = GlobalPosition,
                ["speed"] = stats.Speed,
                ["direction"] = stats.Direction
            };
            Rpc(MethodName.clientSyncPosition, syncDict);
        }
        else
        {
            if (syncCounter == 0)
            {
                this.GlobalPosition += syncDirection * syncSpeed * (float)delta;
            }

            syncCounter++;
            syncCounter %= 15;
        }
    }

    private void onHitableObjectCollided(IHitableObject hitObject)
    {
        if (stats.CustomProjectileCollided != null)
        {
            stats.CustomProjectileCollided(hitObject);
        }
        else
        {
            HitableObjectCollided(hitObject);
        }
    }

    /**
     * Server only.
     */
    protected virtual void HitableObjectCollided(IHitableObject hitObject)
    {
        var ctx = new HitContext
        {
            Source = stats.Caller,
            Target = hitObject as PlayerCharacter,
            Damages = new System.Collections.Generic.Dictionary<DamageType, Damage>
            {
                { stats.Damage.Type, stats.Damage }
            },
            AbilityGuid = AbilityGuid
        };
        hitObject.ApplyDamage(ctx);
        if (stats.PiercingCount-- <= 0)
        {
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
        EmitSignal(SignalName.OnDestroyed, Position);
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
        var speed = (float)dict["speed"];
        var direction = (Vector2)dict["direction"];
        
        this.GlobalPosition = globalPosition;
        this.syncSpeed = speed;
        this.syncDirection = direction;
    }

    public static double EvenDistributedAngle(List<Projectile> projectiles, int maxProjectiles, int delta)
    {
        if (projectiles.Count <= 0 || maxProjectiles <= 0)
        {
            return 0;
        }
        
        var angles = projectiles.Select(x 
            => Math.Round((Mathf.RadToDeg(x.Theta) % 360 + 360) % 360,0)
        ).ToList();
        
        if (angles.Any())
        {
            var offset = angles.Min();
            for (var i = 0; i < maxProjectiles; i++)
            {
                var a = offset + 360 / maxProjectiles * i;
                if (!angles.Any(x => x - delta < a && x + delta > a))
                {
                    return a;
                }    
            }
        }

        return 0;
    }
}