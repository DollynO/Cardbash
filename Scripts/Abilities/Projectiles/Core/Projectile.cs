using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    public ProjectileSpawnRequest SpawnRequest;
    public ProjectileRuntime Runtime;
    public int TeamId => SpawnRequest?.Caller is ITeamAffiliation team ? team.TeamId : -1;

    [Export] private Timer timer;
    [Export] private CollisionShape2D collisionShape;
    [Export] private Area2D pullArea;
    [Export] private Area2D detectArea;
    [Export] private CollisionShape2D pullAreaShape;
    private Color pullAreaColor = new(0.5f, 0.5f, 0.5f, 0.1f);
    private uint collisionMask = 4;

    private List<IEntityComponent> entityInPullArea = new();

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

    public void Initialize(ProjectileSpawnRequest spawnRequest, ProjectileRuntime runtime = null)
    {
        SpawnRequest = spawnRequest;
        Runtime = runtime ?? ProjectileRuntime.Empty();
        if (spawnRequest.Caller != null && spawnRequest.Caller.TryGetComponent(out statBlock))
        {
            SpawnRequest.Pull.Radius += statBlock.GetStat(StatType.AddPullRadius);
        }

        if (SpawnRequest.Pull.Radius > 0)
        {
            pullArea.Visible = true;
        }

        if (SpawnRequest.Movement.AngleOffset != 0)
        {
            SpawnRequest.Movement.Direction = SpawnRequest.Movement.Direction.Rotated(SpawnRequest.Movement.AngleOffset);
        }

        Scale = SpawnRequest.Visual.Scale;
        foreach (var behavior in Runtime.Behaviors)
        {
            AddBehavior(behavior);
        }
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, SpawnRequest.Pull.Radius * 35, pullAreaColor);
    }

    public override void _Ready()
    {
        SetMultiplayerAuthority(1);
        if (Multiplayer.IsServer())
        {
            if (SpawnRequest.Lifetime.Seconds > 0)
            {
                timer.Start(SpawnRequest.Lifetime.Seconds);
            }

            detectArea.BodyEntered += OnBodyEntered;
            if (statBlock != null)
            {
                SpawnRequest.Pull.Strength += statBlock.GetStat(StatType.AddPullStrength);
            }

            if (SpawnRequest.Collision.CollisionMask > 0)
            {
                detectArea.CollisionMask = SpawnRequest.Collision.CollisionMask;
            }
        }
        pullArea.Visible = SpawnRequest.Pull.Radius > 0;
        if (SpawnRequest.Pull.Radius > 0)
        {
            pullArea.BodyEntered += PullAreaOnBodyEntered;
            pullArea.BodyExited += PullAreaOnBodyExit;
            if (pullAreaShape.Shape is CircleShape2D circleShape)
            {
                circleShape.Radius = SpawnRequest.Pull.Radius * 35;
            }

            QueueRedraw();
        }

        var vs = new VisualComponent();
        if (!string.IsNullOrEmpty(SpawnRequest.Visual.AnimationPath))
        {
            vs.SetAnimation(SpawnRequest.Visual.AnimationPath, SpawnRequest.Visual.AnimationOffset);
        }
        AddComponent(vs);

        GlobalPosition = SpawnRequest.StartPosition;
        Rotation = SpawnRequest.Movement.Direction.Angle();

        if (SpawnRequest.Health.Life > 0)
        {
            var hc = new HealthComponent();
            hc.Reset(SpawnRequest.Health.Life);
            AddComponent(hc);
            EventBus.CombatEventBus.KilledEventHandler += onProjectileDeath;

            var dac = new DamageAbleComponent();
            AddComponent(dac);

            if (SpawnRequest.Health.Life > 1)
            {
                var oui = new OverHeadUiComponent();
                AddComponent(oui);
            }
        }
    }

    private void onProjectileDeath(object sender, KilledEventArgs e)
    {
        if (e.Target != this) return;
        
        EventBus.CombatEventBus.KilledEventHandler -= onProjectileDeath;
        DestroyProjectile();
    }

    public override void _ExitTree()
    {
        EventBus.CombatEventBus.KilledEventHandler -= onProjectileDeath;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body == this)
        {
            return;
        }

        if (!CombatTargeting.IsFriendlyFireEnabled(SpawnRequest.Caller)
            && body is Projectile proj
            && IsSameTeam(proj))
        {
            return;
        }

        if (body is IEntityComponent hitObject
            && CombatTargeting.ShouldAbilityAffect(SpawnRequest.Caller, hitObject))
        {
            HitableObjectCollided(hitObject);
        }
    }

    private void PullAreaOnBodyEntered(Node2D body)
    {
        if (body == this)
        {
            return;
        }

        if (!CombatTargeting.IsFriendlyFireEnabled(SpawnRequest.Caller)
            && body is Projectile proj
            && IsSameTeam(proj))
        {
            return;
        }

        if (body is IEntityComponent ec)
        {
            if (CombatTargeting.ShouldAbilityAffect(SpawnRequest.Caller, ec)
                && !entityInPullArea.Contains(ec))
            {
                entityInPullArea.Add(ec);
            }
        }
    }

    private void PullAreaOnBodyExit(Node2D body)
    {
        if (body == this)
        {
            return;
        }

        if (!CombatTargeting.IsFriendlyFireEnabled(SpawnRequest.Caller)
            && body is Projectile proj
            && IsSameTeam(proj))
        {
            return;
        }

        if (body is IEntityComponent ec && entityInPullArea.Contains(ec))
        {
            entityInPullArea.Remove(ec);
        }
    }

    private bool IsSameTeam(Projectile other)
    {
        return other.TeamId >= 0 && TeamId >= 0 && other.TeamId == TeamId;
    }

    public override void _Process(double delta)
    {
        if (Multiplayer.IsServer())
        {
            foreach (var player in entityInPullArea)
                if (player.TryGetComponent(out MoveComponent moveComponent))
                {
                    moveComponent.Drag(this.GlobalPosition, SpawnRequest.Pull.Strength, 0.1f);
                }
        }
    }

    private Vector2 getNextPosition(ProjectileMovementConfig movement, float delta)
    {
        switch (movement.Mode)
        {
            case MovementMode.STRAIGHT:
                return movement.Direction * movement.Speed * delta;
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
            if (SpawnRequest.Movement.Mode != MovementMode.NONE)
            {
                var to = getNextPosition(SpawnRequest.Movement, (float)delta);
                var collider = MoveAndCollide(to);

                if (collider != null)
                {
                    handleTerrainCollision(collider);
                }
            }

            this.behaviors.ForEach(b => b.OnProcess((float)delta));
        }
    }

    private void handleTerrainCollision(KinematicCollision2D collider)
    {
        if (collider.GetCollider() is TileMapLayer layer)
        {
            if (SpawnRequest.Movement.BounceCount > 0)
            {
                SpawnRequest.Movement.BounceCount--;
                SpawnRequest.Movement.Direction = SpawnRequest.Movement.Direction.Bounce(collider.GetNormal());
                Rotation = SpawnRequest.Movement.Direction.Angle();
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
        Runtime.OnHit?.Invoke(hitObject, this);
        SpawnRequest.Collision.PierceCount--;
        if (SpawnRequest.Collision.PierceCount <= 0)
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
