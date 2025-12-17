using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace CardBase.Scripts.Abilities;

public class AoeBaseStats
{
    public PlayerCharacter Owner { get; set; }
    public float Radius { get; set; }
    public float Angle { get; set; } = 360f; // Default to full circle
    public float AngleOffset { get; set; } = 0f; // Rotation offset in degrees
    public float ActivationTime { get; set; }
    public float Duration { get; set; } = -1f; // -1 means until round end
    public bool IsStationary { get; set; }
    public Vector2 StationaryPosition { get; set; }
    public string AbilityGUID { get; set; }
    public float TickInterval { get; set; } = 1f; // How often OnTick is called
    public float ShapeUpdateInterval { get; set; } = 0.15f; // How often to recalculate collision shape
    
    // Callbacks
    public Action<List<PlayerCharacter>> OnActivation { get; set; }
    public Action<List<PlayerCharacter>> OnDeactivation { get; set; }
    public Action<PlayerCharacter, double> OnTick { get; set; }
    public Action<PlayerCharacter> OnPlayerEnter { get; set; }
    public Action<PlayerCharacter> OnPlayerExit { get; set; }
    
    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        var dict = new Godot.Collections.Dictionary<string, Variant>
        {
            ["Radius"] = Radius,
            ["Angle"] = Angle,
            ["AngleOffset"] = AngleOffset,
            ["ActivationTime"] = ActivationTime,
            ["Duration"] = Duration,
            ["IsStationary"] = IsStationary,
            ["StationaryPosition"] = StationaryPosition,
            ["OwnerId"] = Owner?.PlayerId ?? 0,
            ["AbilityGUID"] = AbilityGUID,
            ["TickInterval"] = TickInterval,
        };
        
        return dict;
    }

    public static AoeBaseStats FromDict(Godot.Collections.Dictionary<string, Variant> dict, GameManager gameManager)
    {
        var stats = new AoeBaseStats
        {
            Radius = (float)dict["Radius"],
            Angle = (float)dict["Angle"],
            AngleOffset = (float)dict["AngleOffset"],
            ActivationTime = (float)dict["ActivationTime"],
            Duration = (float)dict["Duration"],
            IsStationary = (bool)dict["IsStationary"],
            StationaryPosition = (Vector2)dict["StationaryPosition"],
            AbilityGUID = (string)dict["AbilityGUID"],
            TickInterval = (float)dict["TickInterval"],
            Owner = gameManager.GetPlayerCharacter((long)dict["OwnerId"])
        };
        
        return stats;
    }
}

[GlobalClass]
public partial class AoeBase : Node2D
{
    private enum InternalState
    {
        ACTIVATION,
        TICK,
        DEACTIVATION
    }

    private AoeBaseStats stats;
    private InternalState internalState;
    
    private List<Vector2> collisionPoints = new();
    private CollisionPolygon2D collisionPolygon = new();
    private Area2D detectArea = new();
    private Polygon2D polygon = new();
    private Shader fillAmountShader = GD.Load<Shader>("res://Shaders/MeleeConeFillShader.gdshader");
    private Texture2D fillAmountTexture = GD.Load<Texture2D>("res://Sprites/whiteBox.png");
    
    private Godot.Color fillColor = Colors.Aqua;
    private Godot.Color emptyColor = Colors.White;
    private PhysicsDirectSpaceState2D spaceState;
    private float activationTimeCount;
    private float durationTimeCount;
    private float tickTimeCount;
    private float shapeUpdateTimeCount;
    private Timer deactivationBlinkTimer;
    private int blinkCount;
    
    private HashSet<PlayerCharacter> playersInArea = new();
    
    public void Initialize(AoeBaseStats aoeStats)
    {
        this.stats = aoeStats;
    }
    
    public override void _Ready()
    {
        if (stats == null)
        {
            GD.PrintErr("AoeBase: stats not initialized!");
            QueueFree();
            return;
        }
        
        spaceState = GetWorld2D().GetDirectSpaceState();
        
        // Setup polygon for display
        AddChild(polygon);
        fillColor.A = 0.5f;
        polygon.Color = fillColor;

        var shaderMaterial = new ShaderMaterial();
        shaderMaterial.Shader = fillAmountShader;
        polygon.Material = shaderMaterial;
        var shaderColorEmpty = new Vector4(emptyColor.R, emptyColor.G, emptyColor.B, emptyColor.A);
        var shaderFillColor = new Vector4(fillColor.R, fillColor.G, fillColor.B, fillColor.A);
        ((ShaderMaterial)polygon.Material).SetShaderParameter("empty_color", shaderColorEmpty);
        ((ShaderMaterial)polygon.Material).SetShaderParameter("fill_color", shaderFillColor);
        polygon.Texture = fillAmountTexture;
        
        // Setup collision detection (server only)
        if (Multiplayer.IsServer())
        {
            AddChild(detectArea);
            detectArea.CollisionMask = 4;
            detectArea.AddChild(collisionPolygon);
            
            // Connect area signals
            detectArea.BodyEntered += OnBodyEntered;
            detectArea.BodyExited += OnBodyExited;
            
            internalState = InternalState.ACTIVATION;
        }
        
        // Position the AOE
        if (stats.IsStationary)
        {
            GlobalPosition = stats.StationaryPosition;
        }
        else if (stats.Owner != null)
        {
            // Will follow owner in _PhysicsProcess
            GlobalPosition = stats.Owner.GlobalPosition;
        }
        
        CalculateCollisionArea();
        UpdateDisplayPolygon();

        QueueRedraw();
    }
    
    private void OnBodyEntered(Node2D body)
    {
        if (body is PlayerCharacter player)
        {
            // Check team (only damage enemies or support allies based on ability type)
            if (ShouldAffectPlayer(player))
            {
                playersInArea.Add(player);
                stats.OnPlayerEnter?.Invoke(player);
            }
        }
    }
    
    private void OnBodyExited(Node2D body)
    {
        if (body is PlayerCharacter player && playersInArea.Contains(player))
        {
            playersInArea.Remove(player);
            stats.OnPlayerExit?.Invoke(player);
        }
    }
    
    private bool ShouldAffectPlayer(PlayerCharacter player)
    {
        return true;
    }
    
    private void OnActivation()
    {
        if (!Multiplayer.IsServer()) return;
        
        var affectedPlayers = GetAffectedPlayers();
        
        stats.OnActivation?.Invoke(affectedPlayers);

        if (stats.Duration > 0)
        {
            internalState = InternalState.TICK;   
        }
        else
        {
            FinalizeDeactivation();
        }
    }
    
    private List<PlayerCharacter> GetAffectedPlayers()
    {
        var bodies = detectArea.GetOverlappingBodies();
        var players = new List<PlayerCharacter>();
        
        foreach (var body in bodies)
        {
            if (body is PlayerCharacter player && ShouldAffectPlayer(player))
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
            var affectedPlayers = playersInArea.ToList();
            stats.OnDeactivation?.Invoke(affectedPlayers);
        }

        Rpc(MethodName.destroyOnClient);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void destroyOnClient()
    {
        QueueFree();
    }
    
    public override void _PhysicsProcess(double delta)
    {
        // Follow owner if not stationary
        if (!stats.IsStationary && stats.Owner != null)
        {
            GlobalPosition = stats.Owner.GetCharacterCenterPosition();
            GlobalRotation = stats.Owner.GetCharacterCenterPoint().GlobalRotation;
        }
        
        if (internalState == InternalState.ACTIVATION)
        {
            activationTimeCount += (float)delta;
            var activationPercentage = Mathf.Clamp(activationTimeCount / stats.ActivationTime, 0f, 1f);
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
            
            // Handle tick effects
            if (tickTimeCount >= stats.TickInterval)
            {
                tickTimeCount = 0f;
                
                // Visual pulse effect every tick
                AnimateTick();
                
                // Apply tick effects to players in area
                if (Multiplayer.IsServer())
                {
                    foreach (var player in playersInArea)
                    {
                        stats.OnTick?.Invoke(player, delta);
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
                    Rpc(MethodName.updateClients, new Array<Vector2>(this.polygon.Polygon), new Array<Vector2>(this.polygon.UV));
                    QueueRedraw();
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void updateClients(Array<Vector2> points, Array<Vector2> uvPoints)
    {
        polygon.Polygon = points.ToArray();
        polygon.UV = uvPoints.ToArray();
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
        
        var isFullCircle = Mathf.Abs(stats.Angle - 360f) < 0.01f;
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
        
        collisionPolygon.Polygon = collisionPoints.ToArray();
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
            Exclude = stats.Owner != null ? new Array<Rid> { stats.Owner.GetRid() } : new Array<Rid>()
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
            DrawLine(from, to, new Godot.Color(1.0f, 1.0f, 1.0f, 0.5f));
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
}