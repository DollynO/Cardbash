using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts;
using CardBase.Scripts.Abilities.ProjectileBehavior;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public class AbilityVolumeCallbacks
{
    public Action<List<IEntityComponent>, AbilityVolume> OnActivation { get; set; }
    public Action<List<IEntityComponent>, AbilityVolume> OnDeactivation { get; set; }
    public Action<IEntityComponent, double, AbilityVolume> OnTick { get; set; }
    public Action<IEntityComponent, AbilityVolume> OnEntityEnter { get; set; }
    public Action<IEntityComponent, AbilityVolume> OnEntityExit { get; set; }
    public Action<IEntityComponent, AbilityVolume> OnHit { get; set; }
    public Action<Vector2, AbilityVolume> OnDestroyed { get; set; }
    public Action<Vector2, AbilityVolume> OnExpired { get; set; }
    public Action<Vector2, AbilityVolume> OnCollision { get; set; }
    public Action<Vector2, AbilityVolume> OnPierced { get; set; }
}

public enum AbilityVolumeShape
{
    Circle,
    Cone,
    Rectangle,
}

public enum AbilityVolumeAnchor
{
    World,
    Owner,
}

public class AbilityVolumeStats
{
    public const float DefaultTickInterval = 0.5f;

    public IEntityComponent Owner { get; set; }
    public DamageType BaseDamageType { get; set; }
    public System.Collections.Generic.Dictionary<DamageType, float> DamageTypePercentages { get; set; } = new();
    public float Radius { get; set; }
    public float Width { get; set; }
    public AbilityVolumeShape Shape { get; set; } = AbilityVolumeShape.Circle;
    public float Angle { get; set; } = 360f; // Default to full circle
    public float AngleOffset { get; set; } = 0f; // Rotation offset in degrees
    public float ActivationTime { get; set; }
    public float Duration { get; set; } = 0f; // -1 means until round end
    public bool IsStationary { get; set; }
    public Vector2 StationaryPosition { get; set; }
    public AbilityVolumeAnchor Anchor { get; set; } = AbilityVolumeAnchor.World;
    public Vector2 StartPosition { get; set; } = new(-10000, -10000);
    public Vector2 Direction { get; set; } = Vector2.Zero;
    public float Speed { get; set; }
    public MovementMode MovementMode { get; set; } = MovementMode.NONE;
    public int BounceCount { get; set; }
    public bool DestroyOnTerrainCollision { get; set; } = true;
    public bool ActivateOnEntityEnter { get; set; }
    public int PierceCount { get; set; }
    public uint CollisionMask { get; set; } = CombatCollisionLayers.TargetableEntities;
    public ProjectileVisualConfig Visual { get; set; } = new();
    public ProjectilePullConfig Pull { get; set; } = new();
    public ProjectileHealthConfig Health { get; set; } = new();
    public bool HasHealth { get; set; }
    public string AbilityGUID { get; set; }
    public float TickInterval { get; set; } = DefaultTickInterval; // How often OnTick is called
    public float ShapeUpdateInterval { get; set; } = 0.016f; // How often to recalculate collision shape
    public bool CanAffectOwner { get; set; } = true;

    public AbilityVolumeCallbacks Callbacks { get; set; }

    public void NormalizeForSpawn()
    {
        if (IsStationary)
        {
            Anchor = AbilityVolumeAnchor.World;
            StartPosition = StationaryPosition;
        }
        else if (Owner != null && MovementMode == MovementMode.NONE)
        {
            Anchor = AbilityVolumeAnchor.Owner;
        }

        if (Shape != AbilityVolumeShape.Rectangle)
        {
            Shape = Mathf.Abs(Angle - 360f) < 0.01f
                ? AbilityVolumeShape.Circle
                : AbilityVolumeShape.Cone;
        }

        CollisionMask = CollisionMask > 0
            ? CollisionMask
            : CombatCollisionLayers.TargetableEntities;
    }

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        var damageTypePercentages = new Godot.Collections.Dictionary<int, float>();
        foreach (var (damageType, percentage) in DamageTypePercentages ?? new System.Collections.Generic.Dictionary<DamageType, float>())
        {
            damageTypePercentages[(int)damageType] = percentage;
        }

        var dict = new Godot.Collections.Dictionary<string, Variant>
        {
            ["BaseDamageType"] = (int)BaseDamageType,
            ["DamageTypePercentages"] = damageTypePercentages,
            ["Radius"] = Radius,
            ["Width"] = Width,
            ["Shape"] = (int)Shape,
            ["Angle"] = Angle,
            ["AngleOffset"] = AngleOffset,
            ["ActivationTime"] = ActivationTime,
            ["Duration"] = Duration,
            ["IsStationary"] = IsStationary,
            ["StationaryPosition"] = StationaryPosition,
            ["Anchor"] = (int)Anchor,
            ["StartPosition"] = StartPosition,
            ["Direction"] = Direction,
            ["Speed"] = Speed,
            ["MovementMode"] = (int)MovementMode,
            ["BounceCount"] = BounceCount,
            ["DestroyOnTerrainCollision"] = DestroyOnTerrainCollision,
            ["ActivateOnEntityEnter"] = ActivateOnEntityEnter,
            ["PierceCount"] = PierceCount,
            ["CollisionMask"] = CollisionMask,
            ["Visual"] = Visual.ToDict(),
            ["Pull"] = Pull.ToDict(),
            ["Health"] = Health.ToDict(),
            ["HasHealth"] = HasHealth,
            ["OwnerPath"] = Owner is Node2D ownerNode ? ownerNode.GetPath() : string.Empty,
            ["OwnerPlayerId"] = Owner is PlayerCharacter ownerPlayer ? ownerPlayer.PlayerId : 0,
            ["AbilityGUID"] = AbilityGUID,
            ["TickInterval"] = TickInterval,
            ["ShapeUpdateInterval"] = ShapeUpdateInterval,
            ["CanAffectOwner"] = CanAffectOwner,
        };

        return dict;
    }

    public static AbilityVolumeStats FromDict(Godot.Collections.Dictionary<string, Variant> dict, GameManager gameManager)
    {
        var stats = new AbilityVolumeStats
        {
            Radius = (float)dict["Radius"],
            Width = dict.TryGetValue("Width", out var widthVariant) ? (float)widthVariant : 0f,
            Shape = dict.TryGetValue("Shape", out var shapeVariant)
                    && System.Enum.IsDefined(typeof(AbilityVolumeShape), (int)shapeVariant)
                ? (AbilityVolumeShape)(int)shapeVariant
                : AbilityVolumeShape.Circle,
            Angle = (float)dict["Angle"],
            AngleOffset = (float)dict["AngleOffset"],
            ActivationTime = (float)dict["ActivationTime"],
            Duration = (float)dict["Duration"],
            IsStationary = (bool)dict["IsStationary"],
            StationaryPosition = (Vector2)dict["StationaryPosition"],
            Anchor = dict.TryGetValue("Anchor", out var anchorVariant)
                     && System.Enum.IsDefined(typeof(AbilityVolumeAnchor), (int)anchorVariant)
                ? (AbilityVolumeAnchor)(int)anchorVariant
                : ((bool)dict["IsStationary"] ? AbilityVolumeAnchor.World : AbilityVolumeAnchor.Owner),
            StartPosition = dict.TryGetValue("StartPosition", out var startPositionVariant)
                ? (Vector2)startPositionVariant
                : (Vector2)dict["StationaryPosition"],
            Direction = dict.TryGetValue("Direction", out var directionVariant)
                ? (Vector2)directionVariant
                : Vector2.Zero,
            Speed = dict.TryGetValue("Speed", out var speedVariant) ? (float)speedVariant : 0f,
            MovementMode = dict.TryGetValue("MovementMode", out var movementModeVariant)
                           && System.Enum.IsDefined(typeof(MovementMode), (int)movementModeVariant)
                ? (MovementMode)(int)movementModeVariant
                : MovementMode.NONE,
            BounceCount = dict.TryGetValue("BounceCount", out var bounceCountVariant) ? (int)bounceCountVariant : 0,
            DestroyOnTerrainCollision = !dict.TryGetValue("DestroyOnTerrainCollision", out var destroyOnTerrainCollisionVariant)
                                        || (bool)destroyOnTerrainCollisionVariant,
            ActivateOnEntityEnter = dict.TryGetValue("ActivateOnEntityEnter", out var activateOnEntityEnterVariant)
                                    && (bool)activateOnEntityEnterVariant,
            PierceCount = dict.TryGetValue("PierceCount", out var pierceCountVariant) ? (int)pierceCountVariant : 0,
            CollisionMask = dict.TryGetValue("CollisionMask", out var collisionMaskVariant)
                ? (uint)collisionMaskVariant
                : CombatCollisionLayers.TargetableEntities,
            Visual = dict.TryGetValue("Visual", out var visualVariant)
                ? ProjectileVisualConfig.FromDict(visualVariant.AsGodotDictionary<string, Variant>())
                : new ProjectileVisualConfig(),
            Pull = dict.TryGetValue("Pull", out var pullVariant)
                ? ProjectilePullConfig.FromDict(pullVariant.AsGodotDictionary<string, Variant>())
                : new ProjectilePullConfig(),
            Health = dict.TryGetValue("Health", out var healthVariant)
                ? ProjectileHealthConfig.FromDict(healthVariant.AsGodotDictionary<string, Variant>())
                : new ProjectileHealthConfig(),
            HasHealth = dict.TryGetValue("HasHealth", out var hasHealthVariant) && (bool)hasHealthVariant,
            AbilityGUID = (string)dict["AbilityGUID"],
            TickInterval = (float)dict["TickInterval"],
            ShapeUpdateInterval = (float)dict["ShapeUpdateInterval"],
            CanAffectOwner = !dict.TryGetValue("CanAffectOwner", out var canAffectOwnerVariant)
                             || (bool)canAffectOwnerVariant,
        };

        if (dict.TryGetValue("BaseDamageType", out var baseDamageTypeVariant)
            && System.Enum.IsDefined(typeof(DamageType), (int)baseDamageTypeVariant))
        {
            stats.BaseDamageType = (DamageType)(int)baseDamageTypeVariant;
        }

        if (dict.TryGetValue("DamageTypePercentages", out var damageTypePercentagesVariant))
        {
            var damageTypePercentages = damageTypePercentagesVariant.AsGodotDictionary<int, float>();
            foreach (var (damageType, percentage) in damageTypePercentages)
            {
                if (System.Enum.IsDefined(typeof(DamageType), damageType))
                {
                    stats.DamageTypePercentages[(DamageType)damageType] = percentage;
                }
            }
        }

        if (dict.TryGetValue("OwnerPlayerId", out var ownerPlayerIdVariant)
            && (long)ownerPlayerIdVariant != 0
            && gameManager.GetPlayerCharacter((long)ownerPlayerIdVariant) is { } ownerPlayer)
        {
            stats.Owner = ownerPlayer;
        }
        else
        {
            var ownerPath = dict.TryGetValue("OwnerPath", out var ownerPathVariant)
                ? (string)ownerPathVariant
                : dict.TryGetValue("OwnerId", out var legacyOwnerPathVariant)
                    ? (string)legacyOwnerPathVariant
                    : string.Empty;
            stats.Owner = string.IsNullOrEmpty(ownerPath)
                ? null
                : gameManager.GetNodeOrNull<Node>(ownerPath) as IEntityComponent;
        }

        return stats;
    }

    public static AbilityVolumeStats FromProjectile(ProjectileSpawnRequest request)
    {
        var radius = 16f * Mathf.Max(request.Visual.Scale.X, request.Visual.Scale.Y);
        request.Visual.ScenePath = string.IsNullOrEmpty(request.Visual.ScenePath)
            ? "res://Scenes/Projectiles/Projectile.tscn"
            : request.Visual.ScenePath;

        return new AbilityVolumeStats
        {
            Owner = request.Caller,
            Radius = radius,
            Shape = AbilityVolumeShape.Circle,
            Angle = 360f,
            ActivationTime = 0f,
            Duration = request.Lifetime.Seconds,
            IsStationary = false,
            Anchor = AbilityVolumeAnchor.World,
            StartPosition = request.StartPosition,
            StationaryPosition = request.StartPosition,
            Direction = request.Movement.Direction,
            Speed = request.Movement.Speed,
            MovementMode = request.Movement.Mode,
            BounceCount = request.Movement.BounceCount,
            DestroyOnTerrainCollision = true,
            ActivateOnEntityEnter = true,
            PierceCount = request.Collision.PierceCount,
            CollisionMask = request.Collision.CollisionMask > 0
                ? request.Collision.CollisionMask
                : CombatCollisionLayers.ProjectileTargets,
            Visual = request.Visual,
            Pull = request.Pull,
            Health = request.Health,
            HasHealth = request.Health.Life > 0,
            CanAffectOwner = request.Collision.AllowCallerCollision,
        };
    }
}

[GlobalClass]
public partial class AbilityVolume : CharacterbodyEntityComponent, ITeamAffiliation
{
    private enum InternalState
    {
        ACTIVATION,
        TICK,
        DEACTIVATION
    }

    protected AbilityVolumeStats stats;
    protected AbilityVolumeCallbacks callbacks;
    private InternalState internalState;

    private List<Vector2> collisionPoints = new();
    private CollisionPolygon2D collisionPolygon = new();
    private Area2D detectArea = new();
    private Polygon2D polygon = new();
    private Shader fillAmountShader = GD.Load<Shader>("res://Shaders/MeleeConeFillShader.gdshader");
    private Texture2D fillAmountTexture = GD.Load<Texture2D>("res://Sprites/whiteBox.png");

    private Godot.Color fillColor = Colors.Aqua;
    private Godot.Color emptyColor = Colors.White;
    private Godot.Color borderColor = new(1f, 1f, 1f, 0.5f);
    private PhysicsDirectSpaceState2D spaceState;
    private float activationTimeCount;
    private float durationTimeCount;
    private float tickTimeCount;
    private float shapeUpdateTimeCount;
    private Timer deactivationBlinkTimer;
    private int blinkCount;

    private HashSet<IEntityComponent> playersInArea = new();
    private List<IEntityComponent> entityInPullArea = new();
    private HashSet<IEntityComponent> hitEntities = new();
    private List<IProjectileBehavior> projectileBehaviors = new();
    private List<Vector2> oldPolygons = new();
    private int forceUpdateCounter = 5;
    private bool callerCollisionReady;
    private Timer lifetimeTimer;
    private Area2D pullArea;
    private CollisionShape2D pullAreaShape;
    private Color pullAreaColor = new(0.5f, 0.5f, 0.5f, 0.1f);
    private StatblockComponent statBlock;
    private bool detectAreaSignalsConnected;
    private bool pullAreaSignalsConnected;
    private bool isDestroying;

    public ProjectileSpawnRequest SpawnRequest { get; private set; }
    public ProjectileRuntime Runtime { get; private set; } = ProjectileRuntime.Empty();
    public int TeamId => stats?.Owner is ITeamAffiliation team ? team.TeamId : -1;

    [Signal]
    public delegate void OnDestroyedEventHandler(Vector2 position, Projectile projectile);
    [Signal]
    public delegate void OnPiercingEventHandler(Vector2 position, Projectile projectile);
    [Signal]
    public delegate void OnCollisionEventHandler(Vector2 position, Projectile projectile);

    public void Initialize(AbilityVolumeStats aoeStats)
    {
        this.stats = aoeStats;
        this.callbacks = aoeStats.Callbacks ?? new AbilityVolumeCallbacks();
        if (this is Projectile)
        {
            SpawnRequest = ProjectileSpawnRequest.FromVolumeStats(aoeStats);
        }
    }

    public void Initialize(ProjectileSpawnRequest spawnRequest, ProjectileRuntime runtime = null)
    {
        if (spawnRequest.Movement.AngleOffset != 0)
        {
            spawnRequest.Movement.Direction = spawnRequest.Movement.Direction.Rotated(spawnRequest.Movement.AngleOffset);
        }

        var volumeStats = AbilityVolumeStats.FromProjectile(spawnRequest);
        volumeStats.Owner ??= spawnRequest.Caller;
        Initialize(volumeStats);
        ConfigureProjectile(spawnRequest, runtime);
    }

    public void ConfigureProjectile(ProjectileSpawnRequest spawnRequest, ProjectileRuntime runtime = null)
    {
        SpawnRequest = spawnRequest;
        Runtime = runtime ?? ProjectileRuntime.Empty();
        statBlock = null;

        if (spawnRequest.Caller != null && spawnRequest.Caller.TryGetComponent(out statBlock))
        {
            stats.Owner ??= spawnRequest.Caller;
        }

        callbacks.OnHit = (target, _) => Runtime.OnHit?.Invoke(target, this as Projectile);
        foreach (var behavior in Runtime.Behaviors)
        {
            AddBehavior(behavior);
        }
    }

    public void SetCallbacks(AbilityVolumeCallbacks pCallbacks)
    {
        this.callbacks = pCallbacks ?? new AbilityVolumeCallbacks();
    }

    public void AddBehavior(IProjectileBehavior behavior)
    {
        if (behavior == null || this is not Projectile projectile)
        {
            return;
        }

        behavior.AssignProjectile(projectile);
        projectileBehaviors.Add(behavior);
    }

    public void RestartLifetime(float seconds)
    {
        stats.Duration = seconds;
        if (!Multiplayer.IsServer() || lifetimeTimer == null)
        {
            return;
        }

        if (seconds > 0)
        {
            lifetimeTimer.Start(seconds);
        }
        else
        {
            lifetimeTimer.Stop();
        }
    }

    public float GetRemainingLifetime()
    {
        if (stats?.Duration <= 0f)
        {
            return stats?.Duration ?? 0f;
        }

        if (lifetimeTimer != null && !lifetimeTimer.IsStopped())
        {
            return (float)lifetimeTimer.TimeLeft;
        }

        return stats.Duration;
    }

    public void Cancel()
    {
        if (IsQueuedForDeletion())
        {
            return;
        }

        QueueFree();
    }

    public override void _Ready()
    {
        if (stats == null)
        {
            QueueFree();
            return;
        }

        spaceState = GetWorld2D().GetDirectSpaceState();
        SetMultiplayerAuthority(1);
        CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.Wall;
        if (stats.ActivateOnEntityEnter)
        {
            CollisionMask |= stats.CollisionMask & ~(CombatCollisionLayers.World | CombatCollisionLayers.Wall);
        }
        Scale = stats.Visual?.Scale ?? Vector2.One;

        // Setup polygon for display
        AddChild(polygon);
        fillColor.A = 0.5f;
        polygon.Color = fillColor;

        var shaderMaterial = new ShaderMaterial();
        shaderMaterial.Shader = fillAmountShader;
        polygon.Material = shaderMaterial;
        var shaderColorEmpty = new Vector4(emptyColor.R, emptyColor.G, emptyColor.B, emptyColor.A);
        ((ShaderMaterial)polygon.Material).SetShaderParameter("empty_color", shaderColorEmpty);
        ApplyDamageVisuals((ShaderMaterial)polygon.Material);
        polygon.Texture = fillAmountTexture;

        // Setup collision detection (server only)
        if (Multiplayer.IsServer())
        {
            SetupMovementCollision();
            SetupDetectArea();
            
            internalState = InternalState.ACTIVATION;

            if (stats.Duration > 0f && stats.MovementMode != MovementMode.NONE)
            {
                lifetimeTimer = new Timer();
                AddChild(lifetimeTimer);
                lifetimeTimer.Timeout += OnLifetimeExpired;
                lifetimeTimer.Start(stats.Duration);
            }
        }

        SetupPullArea();
        SetupVisual();
        SetupHealth();

        if (stats.Anchor == AbilityVolumeAnchor.World || stats.IsStationary)
        {
            GlobalPosition = stats.StartPosition != new Vector2(-10000, -10000)
                ? stats.StartPosition
                : stats.StationaryPosition;
        }
        else if (stats.Anchor == AbilityVolumeAnchor.Owner && stats.Owner != null)
        {
            // Will follow owner in _PhysicsProcess
            GlobalPosition = ((Node2D)stats.Owner).GlobalPosition;
        }

        if (stats.Direction != Vector2.Zero)
        {
            Rotation = stats.Direction.Angle();
        }

        CalculateCollisionArea();
        UpdateDisplayPolygon();

        QueueRedraw();
    }

    private void SetupDetectArea()
    {
        detectArea = GetNodeOrNull<Area2D>("DetectArea") ?? detectArea;
        if (detectArea.GetParent() == null)
        {
            detectArea.Name = "DetectArea";
            AddChild(detectArea);
        }

        collisionPolygon = detectArea.GetNodeOrNull<CollisionPolygon2D>("EffectPolygon") ?? collisionPolygon;
        if (collisionPolygon.GetParent() == null)
        {
            collisionPolygon.Name = "EffectPolygon";
            detectArea.AddChild(collisionPolygon);
        }

        if (Multiplayer.IsServer())
        {
            detectArea.CollisionMask = stats.CollisionMask > 0
                ? stats.CollisionMask
                : CombatCollisionLayers.TargetableEntities;

            if (!detectAreaSignalsConnected)
            {
                detectArea.BodyEntered += OnBodyEntered;
                detectArea.BodyExited += OnBodyExited;
                detectAreaSignalsConnected = true;
            }
        }
    }

    private void SetupMovementCollision()
    {
        if (stats.MovementMode == MovementMode.NONE)
        {
            return;
        }

        var shape = GetNodeOrNull<CollisionShape2D>("MovementShape")
                    ?? GetNodeOrNull<CollisionShape2D>("CollisionShape")
                    ?? new CollisionShape2D { Name = "MovementShape" };
        shape.Shape = new CircleShape2D { Radius = Mathf.Max(4f, stats.Width > 0f ? stats.Width * 0.5f : stats.Radius) };
        if (shape.GetParent() == null)
        {
            AddChild(shape);
        }

        if (!stats.CanAffectOwner && stats.Owner is Node ownerNode)
        {
            AddCollisionExceptionWith(ownerNode);
        }
    }

    private void SetupPullArea()
    {
        if (stats.Pull == null || stats.Pull.Radius <= 0f)
        {
            return;
        }

        pullArea = GetNodeOrNull<Area2D>("PullArea") ?? pullArea ?? new Area2D();
        pullArea.Name = "PullArea";
        pullArea.CollisionLayer = 0;
        pullArea.CollisionMask = CombatCollisionLayers.TargetableEntities;
        if (pullArea.GetParent() == null)
        {
            AddChild(pullArea);
        }

        pullAreaShape = pullArea.GetNodeOrNull<CollisionShape2D>("PullAreaShape")
                        ?? pullAreaShape
                        ?? new CollisionShape2D { Name = "PullAreaShape" };
        pullAreaShape.Shape = new CircleShape2D { Radius = stats.Pull.Radius * 35 };
        if (pullAreaShape.GetParent() == null)
        {
            pullArea.AddChild(pullAreaShape);
        }

        if (Multiplayer.IsServer() && !pullAreaSignalsConnected)
        {
            pullArea.BodyEntered += PullAreaOnBodyEntered;
            pullArea.BodyExited += PullAreaOnBodyExit;
            pullAreaSignalsConnected = true;
        }

        if (statBlock != null)
        {
            stats.Pull.Strength += statBlock.GetStat(StatType.AddPullStrength);
        }
    }

    private void SetupVisual()
    {
        if (stats.Visual == null || string.IsNullOrEmpty(stats.Visual.AnimationPath))
        {
            return;
        }

        var visual = new VisualComponent();
        visual.SetAnimation(stats.Visual.AnimationPath, stats.Visual.AnimationOffset);
        AddComponent(visual);
    }

    private void SetupHealth()
    {
        if (!stats.HasHealth || stats.Health.Life <= 0f)
        {
            return;
        }

        var health = new HealthComponent();
        health.Reset(stats.Health.Life);
        AddComponent(health);
        EventBus.CombatEventBus.KilledEventHandler += OnVolumeDeath;

        var damageable = new DamageAbleComponent();
        AddComponent(damageable);

        if (stats.Health.Life > 1f)
        {
            AddComponent(new OverHeadUiComponent());
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body == this)
        {
            return;
        }

        if (!CombatTargeting.IsFriendlyFireEnabled(stats.Owner)
            && body is Projectile projectile
            && IsSameTeam(projectile))
        {
            return;
        }

        if (body is IEntityComponent entity)
        {
            if (stats.ActivateOnEntityEnter && hitEntities.Contains(entity))
            {
                return;
            }

            // Check team (only damage enemies or support allies based on ability type)
            if (ShouldAffectPlayer(entity))
            {
                playersInArea.Add(entity);
                callbacks?.OnEntityEnter?.Invoke(entity, this);
                if (stats.ActivateOnEntityEnter)
                {
                    HitableObjectCollided(entity);
                }
            }
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is IEntityComponent entity && playersInArea.Contains(entity))
        {
            playersInArea.Remove(entity);
            callbacks?.OnEntityExit?.Invoke(entity, this);
        }
    }

    private bool ShouldAffectPlayer(IEntityComponent entity)
    {
        if (entity == this)
        {
            return false;
        }

        if (!stats.CanAffectOwner && ReferenceEquals(entity, stats.Owner))
        {
            return false;
        }

        if (ReferenceEquals(entity, stats.Owner) && SpawnRequest?.Collision.AllowCallerCollision == true)
        {
            return callerCollisionReady;
        }

        return CombatTargeting.ShouldAbilityAffect(stats.Owner, entity);
    }

    private void RemoveUnaffectablePlayers()
    {
        foreach (var player in playersInArea.ToList())
        {
            if (ShouldAffectPlayer(player))
            {
                continue;
            }

            playersInArea.Remove(player);
            callbacks?.OnEntityExit?.Invoke(player, this);
        }
    }

    private void OnActivation()
    {
        if (!Multiplayer.IsServer()) return;

        var affectedPlayers = GetAffectedPlayers();
        foreach (var player in affectedPlayers)
        {
            playersInArea.Add(player);
        }

        callbacks?.OnActivation?.Invoke(affectedPlayers, this);

        if (stats.Duration != 0)
        {
            internalState = InternalState.TICK;
        }
        else
        {
            FinalizeDeactivation();
        }
    }

    private List<IEntityComponent> GetAffectedPlayers()
    {
        var bodies = detectArea.GetOverlappingBodies();
        var players = new List<IEntityComponent>();

        foreach (var body in bodies)
        {
            if (body is IEntityComponent player && ShouldAffectPlayer(player))
            {
                players.Add(player);
            }
        }

        return players;
    }

    private void StartDeactivation()
    {
        internalState = InternalState.DEACTIVATION;
        blinkCount = 0;

        deactivationBlinkTimer = new Timer();
        AddChild(deactivationBlinkTimer);
        deactivationBlinkTimer.Timeout += OnDeactivationBlink;
        deactivationBlinkTimer.Start(0.15f); // Blink every 0.15 seconds
    }

    private void OnDeactivationBlink()
    {
        polygon.Visible = !polygon.Visible;
        blinkCount++;

        if (blinkCount >= 4) // 2 complete blinks (on-off-on-off)
        {
            deactivationBlinkTimer.Stop();
            FinalizeDeactivation();
        }
    }

    private void FinalizeDeactivation()
    {
        if (Multiplayer.IsServer())
        {
            RemoveUnaffectablePlayers();
            var affectedPlayers = playersInArea.ToList();
            callbacks?.OnDeactivation?.Invoke(affectedPlayers, this);
        }

        QueueFree();
    }

    public override void _ExitTree()
    {
        EventBus.CombatEventBus.KilledEventHandler -= OnVolumeDeath;
        if (detectAreaSignalsConnected && detectArea != null)
        {
            detectArea.BodyEntered -= OnBodyEntered;
            detectArea.BodyExited -= OnBodyExited;
            detectAreaSignalsConnected = false;
        }

        if (pullAreaSignalsConnected && pullArea != null)
        {
            pullArea.BodyEntered -= PullAreaOnBodyEntered;
            pullArea.BodyExited -= PullAreaOnBodyExit;
            pullAreaSignalsConnected = false;
        }
    }

    public override void _Process(double delta)
    {
        if (isDestroying || !Multiplayer.IsServer())
        {
            return;
        }

        foreach (var entity in entityInPullArea.ToList())
        {
            if (entity.TryGetComponent(out MoveComponent moveComponent))
            {
                moveComponent.Drag(GlobalPosition, stats.Pull.Strength, 0.1f);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (isDestroying)
        {
            return;
        }

        if (Multiplayer.IsServer())
        {
            RemoveUnaffectablePlayers();
            UpdateCallerCollisionReadiness();
            MoveVolume((float)delta);
        }

        // Follow owner if not stationary
        if (stats.Anchor == AbilityVolumeAnchor.Owner && !stats.IsStationary && stats.Owner != null)
        {
            if (stats.Owner.TryGetComponent(out AimComponent aimComponent))
            {
                GlobalPosition = aimComponent.GetCharacterCenterPosition();
                GlobalRotation = aimComponent.GetCharacterCenterPoint().GlobalRotation;
            }
            else
            {
                GlobalPosition = ((Node2D)stats.Owner).GlobalPosition;
                GlobalRotation = ((Node2D)stats.Owner).GlobalRotation;
            }
        }

        if (internalState == InternalState.ACTIVATION)
        {
            activationTimeCount += (float)delta;
            var activationPercentage = stats.ActivationTime <= 0f
                ? 1f
                : Mathf.Clamp(activationTimeCount / stats.ActivationTime, 0f, 1f);
            ((ShaderMaterial)polygon.Material).SetShaderParameter("fill_amount", activationPercentage);

            if (activationTimeCount >= stats.ActivationTime)
            {
                OnActivation();
            }
        }
        else if (internalState == InternalState.TICK)
        {
            durationTimeCount += (float)delta;
            tickTimeCount += (float)delta;

            var tickInterval = stats.TickInterval > 0f
                ? stats.TickInterval
                : AbilityVolumeStats.DefaultTickInterval;

            // Handle tick effects
            while (tickTimeCount >= tickInterval)
            {
                tickTimeCount -= tickInterval;

                // Visual pulse effect every tick
                AnimateTick();

                // Apply tick effects to players in area
                if (Multiplayer.IsServer())
                {
                    foreach (var player in playersInArea)
                    {
                        callbacks?.OnTick?.Invoke(player, tickInterval, this);
                    }
                }
            }

            // Check duration
            if (stats.Duration > 0 && durationTimeCount >= stats.Duration)
            {
                StartDeactivation();
            }
        }
        else if (internalState == InternalState.DEACTIVATION)
        {

        }

        // Update collision area on server at specified interval
        if (Multiplayer.IsServer() && internalState != InternalState.DEACTIVATION)
        {
            // For stationary AOEs, only calculate once (already done in _Ready)
            if (!stats.IsStationary)
            {
                shapeUpdateTimeCount += (float)delta;
                if (shapeUpdateTimeCount >= stats.ShapeUpdateInterval)
                {
                    shapeUpdateTimeCount = 0f;
                    CalculateCollisionArea();
                    UpdateDisplayPolygon();

                    if (this.forceUpdateCounter == 5)
                    {
                        this.forceUpdateCounter = 0;
                        this.oldPolygons.Clear();
                    }
                    this.forceUpdateCounter++;

                    if (!areEqualApprox(this.polygon.Polygon.ToList(), this.oldPolygons))
                    {
                        var pointDict = new Godot.Collections.Dictionary<int, Vector2>();
                        var uvDict = new Godot.Collections.Dictionary<int, Vector2>();

                        if (this.oldPolygons.Count != this.polygon.Polygon.Length)
                        {
                            var index = 0;
                            foreach (var vector2 in this.polygon.Polygon)
                            {
                                pointDict.Add(index, vector2);
                                index++;
                            }

                            index = 0;
                            foreach (var vector2 in this.polygon.UV)
                            {
                                uvDict.Add(index, vector2);
                                index++;
                            }
                        }
                        else
                        {
                            for (var i = 0; i < this.polygon.Polygon.Length; i++)
                            {
                                if (this.polygon.Polygon[i] != this.oldPolygons[i])
                                {
                                    pointDict.Add(i, this.polygon.Polygon[i]);
                                    uvDict.Add(i, this.polygon.UV[i]);
                                }
                            }
                        }

                        this.oldPolygons.Clear();
                        this.oldPolygons = this.polygon.Polygon.ToList();
                        Rpc(MethodName.updateClients, pointDict, uvDict);
                    }

                    QueueRedraw();
                }
            }
        }

        foreach (var behavior in projectileBehaviors)
        {
            behavior.OnProcess((float)delta);
        }
    }

    private void MoveVolume(float delta)
    {
        if (stats.MovementMode == MovementMode.NONE || stats.Speed <= 0f || stats.Direction == Vector2.Zero)
        {
            return;
        }

        var movement = stats.MovementMode switch
        {
            MovementMode.STRAIGHT => stats.Direction * stats.Speed * delta,
            MovementMode.CURVE => Vector2.Zero,
            _ => Vector2.Zero,
        };

        if (movement == Vector2.Zero)
        {
            return;
        }

        if (TryHitEntityAlongMovement(ref movement) && isDestroying)
        {
            return;
        }

        if (movement == Vector2.Zero)
        {
            return;
        }

        var collision = MoveAndCollide(movement);
        if (collision != null)
        {
            HandleMovementCollision(collision);
        }
    }

    private bool TryHitEntityAlongMovement(ref Vector2 movement)
    {
        if (!stats.ActivateOnEntityEnter)
        {
            return false;
        }

        var targetMask = stats.CollisionMask & ~(CombatCollisionLayers.World | CombatCollisionLayers.Wall);
        if (targetMask == 0)
        {
            targetMask = CombatCollisionLayers.TargetableEntities;
        }

        if (TryHitEntityShapeSweep(targetMask, ref movement))
        {
            return true;
        }

        var from = GlobalPosition;
        var to = GlobalPosition + movement;
        var didHit = false;
        var excluded = new Godot.Collections.Array<Rid> { GetRid() };
        if (!stats.CanAffectOwner && stats.Owner is CollisionObject2D ownerCollider)
        {
            excluded.Add(ownerCollider.GetRid());
        }

        // A small loop lets piercing projectiles consume several targets in one physics frame.
        for (var i = 0; i < 8 && !from.IsEqualApprox(to); i++)
        {
            var query = new PhysicsRayQueryParameters2D
            {
                From = from,
                To = to,
                CollisionMask = targetMask,
                CollideWithAreas = false,
                CollideWithBodies = true,
                Exclude = excluded,
            };

            var hit = spaceState.IntersectRay(query);
            if (!hit.TryGetValue("collider", out var colliderVariant)
                || colliderVariant.AsGodotObject() is not Node2D collider)
            {
                return didHit;
            }

            if (collider is not IEntityComponent entity || !ShouldAffectPlayer(entity))
            {
                if (collider is CollisionObject2D ignoredCollider)
                {
                    excluded.Add(ignoredCollider.GetRid());
                    continue;
                }

                return didHit;
            }

            if (hitEntities.Contains(entity))
            {
                if (collider is CollisionObject2D repeatedCollider)
                {
                    excluded.Add(repeatedCollider.GetRid());
                    continue;
                }

                return didHit;
            }

            didHit = true;
            var hitPosition = hit.TryGetValue("position", out var positionVariant)
                ? (Vector2)positionVariant
                : from;
            GlobalPosition = hitPosition;
            HitableObjectCollided(entity);
            if (isDestroying)
            {
                movement = Vector2.Zero;
                return true;
            }

            if (collider is CollisionObject2D hitCollider)
            {
                excluded.Add(hitCollider.GetRid());
            }

            var direction = (to - from).Normalized();
            from = hitPosition + direction;
        }

        if (didHit)
        {
            movement = to - GlobalPosition;
        }

        return didHit;
    }

    private bool TryHitEntityShapeSweep(uint targetMask, ref Vector2 movement)
    {
        var shape = new CircleShape2D { Radius = Mathf.Max(4f, stats.Radius * Mathf.Max(Scale.X, Scale.Y)) };
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape,
            Transform = new Transform2D(GlobalRotation, GlobalPosition),
            Motion = movement,
            CollisionMask = targetMask,
            CollideWithAreas = false,
            CollideWithBodies = true,
        };

        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        if (!stats.CanAffectOwner && stats.Owner is CollisionObject2D ownerCollider)
        {
            query.Exclude.Add(ownerCollider.GetRid());
        }

        var hits = spaceState.IntersectShape(query, Mathf.Max(1, stats.PierceCount + 1));
        var didHit = false;
        foreach (var hit in hits)
        {
            if (!hit.TryGetValue("collider", out var colliderVariant)
                || colliderVariant.AsGodotObject() is not IEntityComponent entity
                || hitEntities.Contains(entity)
                || !ShouldAffectPlayer(entity))
            {
                continue;
            }

            didHit = true;
            HitableObjectCollided(entity);
            if (isDestroying)
            {
                movement = Vector2.Zero;
                return true;
            }
        }

        return didHit;
    }

    private void HandleMovementCollision(KinematicCollision2D collision)
    {
        if (collision.GetCollider() is IEntityComponent entity
            && ShouldAffectPlayer(entity))
        {
            GlobalPosition += collision.GetTravel();
            if (!hitEntities.Contains(entity))
            {
                callbacks?.OnHit?.Invoke(entity, this);
                hitEntities.Add(entity);
            }

            DestroyVolume();
            return;
        }

        if (stats.BounceCount > 0)
        {
            stats.BounceCount--;
            stats.Direction = stats.Direction.Bounce(collision.GetNormal());
            Rotation = stats.Direction.Angle();
        }
        else
        {
            DestroyVolume();
        }

        callbacks?.OnCollision?.Invoke(GlobalPosition, this);
        if (this is Projectile projectile)
        {
            EmitSignal(SignalName.OnCollision, GlobalPosition, projectile);
        }
    }

    private void UpdateCallerCollisionReadiness()
    {
        if (callerCollisionReady
            || SpawnRequest?.Collision.AllowCallerCollision != true
            || SpawnRequest.Caller is not Node2D callerNode)
        {
            return;
        }

        foreach (var body in detectArea.GetOverlappingBodies())
        {
            if (body == callerNode)
            {
                return;
            }
        }

        callerCollisionReady = true;
    }

    protected virtual void HitableObjectCollided(IEntityComponent hitObject)
    {
        hitEntities.Add(hitObject);
        callbacks?.OnHit?.Invoke(hitObject, this);
        DestroyVolume();
    }

    private void PullAreaOnBodyEntered(Node2D body)
    {
        if (body == this)
        {
            return;
        }

        if (!CombatTargeting.IsFriendlyFireEnabled(stats.Owner)
            && body is Projectile projectile
            && IsSameTeam(projectile))
        {
            return;
        }

        if (body is IEntityComponent entity
            && CombatTargeting.ShouldAbilityAffect(stats.Owner, entity)
            && !entityInPullArea.Contains(entity))
        {
            entityInPullArea.Add(entity);
        }
    }

    private void PullAreaOnBodyExit(Node2D body)
    {
        if (body is IEntityComponent entity)
        {
            entityInPullArea.Remove(entity);
        }
    }

    private bool IsSameTeam(Projectile projectile)
    {
        return projectile.TeamId >= 0 && TeamId >= 0 && projectile.TeamId == TeamId;
    }

    private void OnLifetimeExpired()
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        callbacks?.OnExpired?.Invoke(GlobalPosition, this);
        DestroyVolume();
    }

    private void _on_timer_timeout()
    {
        OnLifetimeExpired();
    }

    private void OnVolumeDeath(object sender, KilledEventArgs e)
    {
        if (e.Target != this)
        {
            return;
        }

        EventBus.CombatEventBus.KilledEventHandler -= OnVolumeDeath;
        DestroyVolume();
    }

    public void DestroyProjectile()
    {
        DestroyVolume();
    }

    public void DestroyVolume()
    {
        if (isDestroying)
        {
            return;
        }

        isDestroying = true;
        callbacks?.OnDestroyed?.Invoke(GlobalPosition, this);
        if (this is Projectile projectile)
        {
            EmitSignal(SignalName.OnDestroyed, GlobalPosition, projectile);
        }

        if (Multiplayer.IsServer())
        {
            var spawner = GetParent() as GlobalAbilitySpawner
                          ?? GetNodeOrNull<GlobalAbilitySpawner>("/root/Main/Game/GlobalAbilitySpawner");
            if (spawner != null)
            {
                spawner.DestroySpawnedNode(Name);
                return;
            }
        }

        DestroyClientVolume();
    }

    private void DestroyClientVolume()
    {
        QueueFree();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void updateClients(Godot.Collections.Dictionary<int, Vector2> points, Godot.Collections.Dictionary<int, Vector2> uvPoints)
    {
        if (points.Count <= 0)
        {
            return;
        }

        var tmpPoints = polygon.Polygon.ToList();
        var tmpUVs = polygon.UV.ToList();
        if (tmpPoints.Count == 0)
        {
            tmpPoints.AddRange(points.Values);
            tmpUVs.AddRange(uvPoints.Values);
        }
        else
        {
            foreach (var kvp in points)
            {
                var index = kvp.Key;
                tmpPoints[index] = kvp.Value;
                tmpUVs[index] = uvPoints[index];
            }
        }

        polygon.Polygon = tmpPoints.ToArray();
        polygon.UV = tmpUVs.ToArray();
        QueueRedraw();
    }

    private void AnimateTick()
    {
        // Create a quick pulse effect
        var tween = CreateTween();
        tween.TweenProperty(polygon, "modulate:a", 0.8f, 0.1f);
        tween.TweenProperty(polygon, "modulate:a", 0.5f, 0.1f);
    }

    private void CalculateCollisionArea()
    {
        collisionPoints.Clear();

        if (stats.Radius <= 0) return;

        if (stats.Shape == AbilityVolumeShape.Rectangle)
        {
            CalculateRectangleCollisionArea();
            return;
        }

        var isFullCircle = stats.Shape == AbilityVolumeShape.Circle || Mathf.Abs(stats.Angle - 360f) < 0.01f;
        var pointCount = isFullCircle ? 180 : (int)Mathf.Max(stats.Angle * 2, 100);

        collisionPoints.Add(Vector2.Zero);
        if (isFullCircle)
        {
            for (var i = 0; i < pointCount; i++)
            {
                var angle = (Mathf.Tau / pointCount) * i;
                var newPoint = RayTo(new Vector2(0, stats.Radius).Rotated(angle));
                collisionPoints.Add(newPoint);
            }
            collisionPoints.Add(collisionPoints[1]);
        }
        else
        {
            var piAngle = Mathf.DegToRad(stats.Angle);
            var angleOffset = -piAngle / 2 + Mathf.DegToRad(stats.AngleOffset);

            for (var i = 0; i < pointCount; i++)
            {
                var angle = (piAngle / pointCount) * i + angleOffset;
                var newPoint = RayTo(new Vector2(0, stats.Radius).Rotated(angle));
                collisionPoints.Add(newPoint);
            }

            collisionPoints.Add(Vector2.Zero);
        }

        UpdateCollisionPolygon();
    }

    private void CalculateRectangleCollisionArea()
    {
        var width = stats.Width > 0f ? stats.Width : stats.Radius * 0.2f;
        var halfWidth = width * 0.5f;
        var localPoints = new[]
        {
            new Vector2(0f, -halfWidth),
            new Vector2(stats.Radius, -halfWidth),
            new Vector2(stats.Radius, halfWidth),
            new Vector2(0f, halfWidth),
        };

        foreach (var point in localPoints)
        {
            collisionPoints.Add(RayTo(point));
        }

        collisionPoints.Add(collisionPoints[0]);
        UpdateCollisionPolygon();
    }

    private void UpdateCollisionPolygon()
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        collisionPolygon.SetDeferred(CollisionPolygon2D.PropertyName.Polygon, collisionPoints.ToArray());
    }

    private void UpdateDisplayPolygon()
    {
        if (collisionPoints.Count == 0) return;

        // Calculate UV coordinates based on actual collision points
        var uvPoints = new List<Vector2>();

        // Find max distance for UV normalization
        float maxDist = 0f;
        foreach (var point in collisionPoints)
        {
            maxDist = Mathf.Max(maxDist, point.Length());
        }

        if (maxDist > 0)
        {
            for (var i = 0; i < collisionPoints.Count; i++)
            {
                var normalized = collisionPoints[i] / maxDist;
                uvPoints.Add(normalized * 0.5f + Vector2.One * 0.5f);
            }
        }

        polygon.Polygon = collisionPoints.ToArray();
        polygon.UV = uvPoints.ToArray();
    }

    private Vector2 RayTo(Vector2 direction)
    {
        var destination = ToGlobal(direction);
        var query = new PhysicsRayQueryParameters2D
        {
            From = GlobalPosition,
            To = destination,
            CollisionMask = 1 + 2,
        };

        var collision = spaceState.IntersectRay(query);
        var rayPosition = collision.TryGetValue("position", out var value) ? (Vector2)value : destination;
        return ToLocal(rayPosition);
    }

    public override void _Draw()
    {
        if (collisionPoints.Count == 0 || internalState == InternalState.DEACTIVATION)
            return;

        var from = collisionPoints[0];

        for (var i = 1; i < collisionPoints.Count; i++)
        {
            var to = collisionPoints[i];
            DrawLine(from, to, borderColor, 2f);
            from = to;
        }
    }

    private Vector2 DisplayConePoint(float length, float halfAngle, float segmentFraction, float offset)
    {
        var angle = offset - halfAngle + segmentFraction * (2f * halfAngle);
        var x = Mathf.Cos(angle) * length;
        var y = Mathf.Sin(angle) * length;
        return new Vector2(x, y);
    }

    private Vector2 DisplayCirclePoint(float length, float angle)
    {
        var x = Mathf.Cos(angle) * length;
        var y = Mathf.Sin(angle) * length;
        return new Vector2(x, y);
    }

    public void ChangeFillColor(Color color)
    {
        this.fillColor = color;
        var shaderFillColor = new Vector4(fillColor.R, fillColor.G, fillColor.B, fillColor.A);
        Rpc(MethodName.changeFillColorClient, shaderFillColor);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void changeFillColorClient(Vector4 color)
    {
        if (polygon.Material is not ShaderMaterial material)
        {
            return;
        }

        material.SetShaderParameter("damage_mix_count", 1);
        material.SetShaderParameter("fill_color", color);
        material.SetShaderParameter("fill_color_1", color);
        material.SetShaderParameter("fill_split_1", 1f);
        material.SetShaderParameter("fill_split_2", 1f);
        material.SetShaderParameter("fill_split_3", 1f);
    }

    private void ApplyDamageVisuals(ShaderMaterial material)
    {
        var parts = GetDamageVisualParts();
        if (parts.Count == 0)
        {
            var fallbackColor = ToShaderColor(fillColor);
            material.SetShaderParameter("fill_color", fallbackColor);
            material.SetShaderParameter("fill_color_1", fallbackColor);
            material.SetShaderParameter("damage_mix_count", 1);
            material.SetShaderParameter("fill_split_1", 1f);
            material.SetShaderParameter("fill_split_2", 1f);
            material.SetShaderParameter("fill_split_3", 1f);
            return;
        }

        borderColor = DamageTypeBorderColor();
        var color1 = ToShaderColor(parts[0].Color);
        var color2 = ToShaderColor(parts[Math.Min(1, parts.Count - 1)].Color);
        var color3 = ToShaderColor(parts[Math.Min(2, parts.Count - 1)].Color);
        var color4 = ToShaderColor(parts[Math.Min(3, parts.Count - 1)].Color);

        material.SetShaderParameter("fill_color", color1);
        material.SetShaderParameter("fill_color_1", color1);
        material.SetShaderParameter("fill_color_2", color2);
        material.SetShaderParameter("fill_color_3", color3);
        material.SetShaderParameter("fill_color_4", color4);
        material.SetShaderParameter("damage_mix_count", parts.Count);
        material.SetShaderParameter("fill_split_1", parts[0].Stop);
        material.SetShaderParameter("fill_split_2", parts[Math.Min(1, parts.Count - 1)].Stop);
        material.SetShaderParameter("fill_split_3", parts[Math.Min(2, parts.Count - 1)].Stop);
    }

    private List<DamageVisualPart> GetDamageVisualParts()
    {
        var total = stats.DamageTypePercentages.Values.Sum(percentage => Mathf.Max(0f, percentage));
        var parts = new List<DamageVisualPart>();
        if (total <= 0f)
        {
            return parts;
        }

        var displayedPercentages = stats.DamageTypePercentages
            .Where(kvp => kvp.Value > 0f)
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key)
            .Take(4)
            .ToList();

        if (displayedPercentages.Count == 0)
        {
            return parts;
        }

        if (stats.DamageTypePercentages.Count(kvp => kvp.Value > 0f) > displayedPercentages.Count)
        {
            var displayedTotal = displayedPercentages.Take(displayedPercentages.Count - 1)
                .Sum(kvp => Mathf.Max(0f, kvp.Value));
            var last = displayedPercentages[^1];
            displayedPercentages[^1] = new KeyValuePair<DamageType, float>(last.Key, total - displayedTotal);
        }

        var cumulative = 0f;
        foreach (var (damageType, percentage) in displayedPercentages)
        {
            cumulative += Mathf.Max(0f, percentage) / total;
            parts.Add(new DamageVisualPart(DamageTypeColor(damageType, 0.5f), Mathf.Clamp(cumulative, 0f, 1f)));
        }

        parts[^1] = parts[^1] with { Stop = 1f };
        return parts;
    }

    private Color DamageTypeBorderColor()
    {
        var weightedColor = new Color(0f, 0f, 0f, 0f);
        var total = 0f;

        foreach (var (damageType, percentage) in stats.DamageTypePercentages)
        {
            var weight = Mathf.Max(0f, percentage);
            if (weight <= 0f)
            {
                continue;
            }

            var color = DamageTypeColor(damageType, 1f);
            weightedColor.R += color.R * weight;
            weightedColor.G += color.G * weight;
            weightedColor.B += color.B * weight;
            total += weight;
        }

        if (total <= 0f)
        {
            var fallback = fillColor;
            fallback.A = 0.5f;
            return fallback;
        }

        weightedColor.R /= total;
        weightedColor.G /= total;
        weightedColor.B /= total;
        weightedColor.A = 0.85f;
        return weightedColor;
    }

    private static Vector4 ToShaderColor(Color color)
    {
        return new Vector4(color.R, color.G, color.B, color.A);
    }

    private static Color DamageTypeColor(DamageType type, float alpha)
    {
        var color = type switch
        {
            DamageType.Physical => new Color(0.72f, 0.72f, 0.68f),
            DamageType.Poison => new Color(0.22f, 0.86f, 0.24f),
            DamageType.Fire => new Color(1f, 0.36f, 0.05f),
            DamageType.Ice => new Color(0.25f, 0.72f, 1f),
            DamageType.Lightning => new Color(1f, 0.9f, 0.18f),
            DamageType.Darkness => new Color(0.55f, 0.16f, 0.82f),
            DamageType.Holy => new Color(1f, 0.88f, 0.44f),
            DamageType.True => new Color(1f, 0.05f, 0.05f),
            _ => Colors.Aqua,
        };
        color.A = alpha;
        return color;
    }

    private readonly record struct DamageVisualPart(Color Color, float Stop);

    private bool areEqualApprox(List<Vector2> a, List<Vector2> b)
    {
        if (a.Count != b.Count)
            return false;

        for (var i = 0; i < a.Count; i++)
        {
            if (!a[i].IsEqualApprox(b[i]))
                return false;
        }

        return true;
    }
}
