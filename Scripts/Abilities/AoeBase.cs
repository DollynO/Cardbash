using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;
using Godot.NativeInterop;
using Array = Godot.Collections.Array;

namespace CardBase.Scripts.Abilities;

public class AoeBaseCallbacks
{
    public Action<List<IEntityComponent>, AoeBase> OnActivation { get; set; }
    public Action<List<IEntityComponent>, AoeBase> OnDeactivation { get; set; }
    public Action<IEntityComponent, double, AoeBase> OnTick { get; set; }
    public Action<IEntityComponent, AoeBase> OnEntityEnter { get; set; }
    public Action<IEntityComponent, AoeBase> OnEntityExit { get; set; }
}

public class AoeBaseStats
{
    public IEntityComponent Owner { get; set; }
    public float Radius { get; set; }
    public float Angle { get; set; } = 360f; // Default to full circle
    public float AngleOffset { get; set; } = 0f; // Rotation offset in degrees
    public float ActivationTime { get; set; }
    public float Duration { get; set; } = 0f; // -1 means until round end
    public bool IsStationary { get; set; }
    public Vector2 StationaryPosition { get; set; }
    public string AbilityGUID { get; set; }
    public float TickInterval { get; set; } = 1f; // How often OnTick is called
    public float ShapeUpdateInterval { get; set; } = 0.016f; // How often to recalculate collision shape

    public AoeBaseCallbacks Callbacks { get; set; }
    
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
            ["OwnerId"] = ((Node2D)Owner).GetPath(),
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
            Owner = (IEntityComponent)gameManager.GetNode((string)dict["OwnerId"]),
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
    private AoeBaseCallbacks callbacks;
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
    
    private HashSet<IEntityComponent> playersInArea = new();
    private List<Vector2> oldPolygons = new();
    private int forceUpdateCounter = 5;
    
    public void Initialize(AoeBaseStats aoeStats)
    {
        this.stats = aoeStats;
    }

    public void SetCallbacks(AoeBaseCallbacks pCallbacks)
    {
        this.callbacks = pCallbacks;
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
            GlobalPosition = ((Node2D)stats.Owner).GlobalPosition;
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
                callbacks.OnEntityEnter?.Invoke(player, this);
            }
        }
    }
    
    private void OnBodyExited(Node2D body)
    {
        if (body is PlayerCharacter player && playersInArea.Contains(player))
        {
            playersInArea.Remove(player);
            callbacks.OnEntityExit?.Invoke(player, this);
        }
    }
    
    private bool ShouldAffectPlayer(IEntityComponent entity)
    {
        if (entity is PlayerCharacter player)
        {
            return player != stats.Owner;
        }

        return true;
    }
    
    private void OnActivation()
    {
        if (!Multiplayer.IsServer()) return;
        
        var affectedPlayers = GetAffectedPlayers();
        
        callbacks.OnActivation?.Invoke(affectedPlayers, this);

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
            var affectedPlayers = playersInArea.ToList();
            callbacks.OnDeactivation?.Invoke(affectedPlayers, this);
        }

        QueueFree();
    }

    
    public override void _PhysicsProcess(double delta)
    {
        // Follow owner if not stationary
        if (!stats.IsStationary && stats.Owner != null)
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
                        callbacks.OnTick?.Invoke(player, delta, this);
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
                                pointDict.Add(index,  vector2);
                                index++;
                            }

                            index = 0;
                            foreach (var vector2 in this.polygon.UV)
                            {
                                uvDict.Add(index,  vector2);
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
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void updateClients(Godot.Collections.Dictionary<int,Vector2> points, Godot.Collections.Dictionary<int, Vector2> uvPoints)
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

    public void ChangeFillColor(Color color)
    {
        this.fillColor = color;
        var shaderFillColor = new Vector4(fillColor.R, fillColor.G, fillColor.B, fillColor.A);
        Rpc(MethodName.changeFillColorClient, shaderFillColor);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void changeFillColorClient(Vector4 color)
    {
        ((ShaderMaterial)polygon.Material).SetShaderParameter("fill_color", color);
    }
    
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