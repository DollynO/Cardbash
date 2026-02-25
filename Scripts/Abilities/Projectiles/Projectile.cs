using System;
using System.Collections.Generic;
using CardBase.Scripts;
using CardBase.Scripts.Abilities.ProjectileBehavior;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public partial class Projectile : CharacterbodyEntityComponent, ITeamAffiliation
{
    public const float MAX_SPEED = 1000;
    public long CreatorId { get; set; }
    public string AbilityGuid { get; set; }

    public ProjectileStats Stats;
    public int TeamId => Stats?.Caller is ITeamAffiliation team ? team.TeamId : -1;

    [Export] private Timer timer;
    [Export] private CollisionShape2D collisionShape;
    [Export] private Area2D pullArea;
    [Export] private Area2D detectArea;
    [Export] private CollisionShape2D pullAreaShape;
    private Color pullAreaColor = new (0.5f, 0.5f, 0.5f, 0.1f);
    private PhysicsDirectSpaceState2D _state;
    private uint collisionMask = 4;
    
    private List<IEntityComponent> entityInPullArea = new ();

    [Signal]
    public delegate void OnDestroyedEventHandler(Vector2 position, Projectile projectile);
    [Signal]
    public delegate void OnPiercingEventHandler(Vector2 position, Projectile projectile);
    [Signal]
    public delegate void OnCollisionEventHandler(Vector2 position, Projectile projectile);

    private const float UpdateTime = 0.05f;
    private float syncTime = UpdateTime;
    private StatblockComponent statBlock;
    
    
    private List<IProjectileBehavior> behaviors = new();
    public void AddBehavior(IProjectileBehavior behavior)
    {
        behavior.AssignProjectile(this);
        behaviors.Add(behavior);
    }

    public void SetStats(ProjectileStats pStats)
    {
        Stats = pStats;
        if (pStats.Caller != null && pStats.Caller.TryGetComponent(out statBlock))
        {
            Stats.PullRadius += statBlock.GetStat(StatType.AddPullRadius);
        }

        if (Stats.PullRadius > 0)
        {
            pullArea.Visible = true;
        }

        Scale = Stats.Scale;
        foreach (var behavior in Stats.Behaviors)
        {
            AddBehavior(behavior);
        }
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, Stats.PullRadius * 35, pullAreaColor);
    }

    public override void _Ready()
    {
        SetMultiplayerAuthority(1);
        if (Multiplayer.IsServer())
        {
            if (Stats.TimeToBeALive > 0)
            {
                timer.Start(Stats.TimeToBeALive);
            }

            _state = GetWorld2D().GetDirectSpaceState();
            detectArea.BodyEntered += OnBodyEntered;
            if (statBlock != null)
            {
                Stats.PullStrength += statBlock.GetStat(StatType.AddPullStrength);
            }

            if (Stats.CollisionMask > 0)
            {
                detectArea.CollisionMask = Stats.CollisionMask;
            }
        }
        pullArea.Visible = Stats.PullRadius > 0;
        if (Stats.PullRadius > 0)
        {
            pullArea.BodyEntered += PullAreaOnBodyEntered;
            pullArea.BodyExited += PullAreaOnBodyExit;
            if (pullAreaShape.Shape is CircleShape2D circleShape)
            {
                circleShape.Radius = Stats.PullRadius * 35;
            }
        }
        
        var vs = new VisualComponent();
        if (!string.IsNullOrEmpty(Stats.AnimationResourcePath))
        {
            vs.SetAnimation(Stats.AnimationResourcePath, Stats.AnimationOffset);
        }
        AddComponent(vs);

        GlobalPosition = Stats.StartPosition;
        Rotation = Stats.Direction.Angle();

        if (Stats.Life > 0)
        {
            var hc = new HealthComponent();
            hc.Reset(Stats.Life);
            AddComponent(hc);
            hc.Death += onProjectileDeath;
            var oui = new OverHeadUiComponent();
            AddComponent(oui);
            var dac = new DamageAbleComponent();
            AddComponent(dac);
        }
    }

    private void onProjectileDeath(object sender, EventArgs e)
    {
        DestroyProjectile();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body == this || body is Projectile proj && ((PlayerCharacter)proj.Stats.Caller).TeamId == ((PlayerCharacter)this.Stats.Caller).TeamId)
        {
            return;
        }
        
        if (body is IEntityComponent hitObject && hitObject != Stats.Caller)
        {
            HitableObjectCollided(hitObject);
        }
    }

    private void PullAreaOnBodyEntered(Node2D body)
    {
        if (body == this || body is Projectile proj && ((PlayerCharacter)proj.Stats.Caller).TeamId == ((PlayerCharacter)this.Stats.Caller).TeamId)
        {
            return;
        }
        
        if (body is IEntityComponent ec)
        {
            if (Stats.Caller is not ITeamAffiliation callerTeam || ec is not ITeamAffiliation targetTeam)
            {
                return;
            }

            if (targetTeam.TeamId == callerTeam.TeamId && !entityInPullArea.Contains(ec))
            {
                entityInPullArea.Add(ec);
            }
        }
    }

    private void PullAreaOnBodyExit(Node2D body)
    {
        if (body == this || body is Projectile proj && ((PlayerCharacter)proj.Stats.Caller).TeamId == ((PlayerCharacter)this.Stats.Caller).TeamId)
        {
            return;
        }
        
        if (body is IEntityComponent ec && entityInPullArea.Contains(ec))
        {
            entityInPullArea.Remove(ec);
        }
    }

    public override void _Process(double delta)
    {
        if (Multiplayer.IsServer())
        {
            var ecs = new IEntityComponent[entityInPullArea.Count];
            entityInPullArea.CopyTo(ecs);
            foreach (var player in entityInPullArea)
                if (player.TryGetComponent(out MoveComponent moveComponent))
                {
                    moveComponent.Drag(this.GlobalPosition, Stats.PullStrength, 0.1f);
                }
        }

        QueueRedraw();
    }

    private Vector2 getNextPosition(ProjectileStats pStats, float delta)
    {
        switch (Stats.MovementMode)
        {
            case MovementMode.STRAIGHT:
                return pStats.Direction * pStats.Speed * delta;
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
            if (Stats.MovementMode != MovementMode.NONE)
            {
                var to = getNextPosition(Stats, (float)delta);
                var collider = MoveAndCollide(to);

                if (collider != null)
                {
                    handleTerrainCollision(collider);
                }
            }

            if (syncTime >= UpdateTime)
            {
                syncTime = 0;
                var syncDict = new Godot.Collections.Dictionary<string, Variant>
                {
                    ["global_position"] = GlobalPosition,
                    ["global_rotation"] = this.GlobalRotation,
                };
                //Rpc(MethodName.clientSyncPosition, syncDict);
            }

            syncTime += (float)delta;
            
            this.behaviors.ForEach(b => b.OnProcess((float)delta));
        }
    }

    private void handleTerrainCollision(KinematicCollision2D collider)
    {
        if (collider.GetCollider() is TileMapLayer layer)
        {
            if (Stats.BouncingCount > 0)
            {
                Stats.BouncingCount--;
                Stats.Direction = Stats.Direction.Bounce(collider.GetNormal());
                Rotation = Stats.Direction.Angle();
            }
            else
            {
                DestroyProjectile();
            }
            
            
            EmitSignal(SignalName.OnCollision, Position, this);
        }
    }
    
    /**
     * Server only.
     */
    protected virtual void HitableObjectCollided(IEntityComponent hitObject)
    {
        Stats.OnHit?.Invoke(hitObject, this);
        Stats.PiercingCount--;
        if (Stats.PiercingCount <= 0)
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
    public void DestroyProjectile()
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
    private void clientSyncStats(Variant data)
    {
        var dict = data.AsGodotDictionary<string, Variant>();
        
        this.GlobalPosition = (Vector2)dict["global_position"];
        this.GlobalRotation = (float)dict["global_rotation"];
    }
}

public class ProjectileStats
{
    public string CastGuid = Guid.NewGuid().ToString();
    public Node2D? Parent;
    public string AnimationResourcePath;
    public Vector2 AnimationOffset = Vector2.Zero;
    public float Speed = 0;
    public Action<IEntityComponent, Projectile> OnHit;
    public Vector2 StartPosition = new (-10000, -10000);
    public Vector2 Direction;
    public float TimeToBeALive = 0;
    public IEntityComponent Caller;
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
    public uint DetectCollisionMask;
    public float AngleOffset = 0;
    public float Life = -1;
    public List<IProjectileBehavior> Behaviors = new();

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        var dict = new Godot.Collections.Dictionary<string, Variant>()
        {
            { nameof(CastGuid),  this.CastGuid},
            { nameof(Parent), Parent?.GetPath() ?? string.Empty },
            { nameof(AnimationResourcePath), AnimationResourcePath},
            {nameof(AnimationOffset), AnimationOffset},
            { nameof(Speed), Speed },
            { nameof(StartPosition), StartPosition },
            { nameof(Direction), Direction },
            { nameof(TimeToBeALive), TimeToBeALive },
            { nameof(Caller), ((Node2D)Caller).GetPath() },
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
            { nameof(Life), Life},
        };
        return dict;
    }

    public static ProjectileStats FromDict(Godot.Collections.Dictionary<string, Variant> dict, GameManager manager)
    {
        var parentString = (string)dict[nameof(Parent)];
        var stats = new ProjectileStats()
        {
            CastGuid = (string)dict[nameof(CastGuid)],
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
            Caller = (IEntityComponent)manager.GetNode((string)dict[nameof(Caller)]),
            TimeToBeALive = (float)dict[nameof(TimeToBeALive)],
            Direction = (Vector2)dict[nameof(Direction)],
            StartPosition = (Vector2)dict[nameof(StartPosition)],
            Speed = (float)dict[nameof(Speed)],
            AnimationResourcePath = (string)dict[nameof(AnimationResourcePath)],
            AnimationOffset =  (Vector2)dict[nameof(AnimationOffset)],
            Parent = !string.IsNullOrEmpty(parentString) ? (Node2D)manager.GetNode((string)dict[nameof(Parent)]) : null,
            Life = (float)dict[nameof(Life)],
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
