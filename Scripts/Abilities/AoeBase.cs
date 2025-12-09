using System;
using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public partial class AoeBase : Node2D
{
    private enum InternalState{
     ACTIVATION,
     TICK,
     DEACTIVATION,
    };

    private PlayerCharacter Caller;
    private List<Vector2> points = new();
    private List<Vector2> displayPoints = new();
    private uint collisionLayerMask = 2;
    private CollisionPolygon2D collisionPolygon = new();
    private Area2D detectArea = new();
    private Polygon2D polygon = new();
    private Shader fillAmountShader = GD.Load<Shader>("res://Shaders/MeleeConeFillShader.gdshader");
    private Texture2D fillAmountTexture = GD.Load<Texture2D>("res://Sprites/whiteBox.png");
    
    private Godot.Color outline = Colors.Aqua;
    private Godot.Color fillColor = Colors.Aqua;
    private PhysicsDirectSpaceState2D _space;
    private Node2D Parent;
    private float attackTimeCount;
    
    AoeBaseStats aoeStats;
    private InternalState internalState;
    
    public AoeBase(AoeBaseStats stats)
    {
        
    }
    
    public override void _Ready()
    {
        _space = GetWorld2D().GetDirectSpaceState();
        Parent = GetParent() as Node2D;
        
        AddChild(polygon);
        fillColor.A = 0.5f;
        polygon.Color = fillColor;

        var shaderMaterial = new ShaderMaterial();
        shaderMaterial.Shader = fillAmountShader;
        polygon.Material = shaderMaterial;
        polygon.Texture = fillAmountTexture;
        if (Multiplayer.IsServer())
        {
            var attackTimer = new Timer();
            AddChild(attackTimer);
            attackTimer.Timeout += onActivation;
            attackTimer.Start(Mathf.Max(aoeStats.ActivationTime, 0.1f));
            internalState = InternalState.ACTIVATION;
        
            AddChild(detectArea);
            detectArea.CollisionMask = 4;
            detectArea.AddChild(collisionPolygon);
        }

        displayPoints.Clear();
        var Angle = 360;
        var piAngle = Mathf.DegToRad(Angle);
        var uvPoints = new List<Vector2>();
        uvPoints.Add(Vector2.Zero);
        displayPoints.Add(Vector2.Zero);
        if ( aoeStats.Radius > 0)
        {
            for (var i = 0; i < Angle * 2; i++)
            {
                var point = displayConePoint(aoeStats.Radius, Mathf.DegToRad(i));
                displayPoints.Add(point);
                uvPoints.Add(point.Normalized() / 2);
                
            }
        }

        polygon.Polygon = displayPoints.ToArray();
        polygon.UV = uvPoints.ToArray();
    }
    
    /**
     * @brief Triggered after the attack time is over.
     */
    private void onActivation()
    {
        if (Multiplayer.IsServer())
        {
            var bodies = detectArea.GetOverlappingBodies();
            foreach (var body in bodies)
            {
                if (body is IHitableObject hitableObject)
                {
                    /*if ((hitableObject is PlayerCharacter player && player.TeamId != Owner.TeamId) 
                        || hitableObject is not PlayerCharacter)
                    {
                        var ctx = new HitContext();
                        ctx.Target = hitableObject as PlayerCharacter;
                        ctx.Source = Owner;
                        ctx.AbilityGuid = AbilityGuid;
                        ctx.Damages = new System.Collections.Generic.Dictionary<DamageType, Damage>
                        {
                            { Damage.Type, Damage }
                        };
                        hitableObject.ApplyDamage(ctx);
                    }*/
                }
            }
        }

        internalState = InternalState.TICK;
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (internalState == InternalState.ACTIVATION)
        {
            attackTimeCount += (float)delta;
            var attackTimePercentage = attackTimeCount / aoeStats.ActivationTime;
            ((ShaderMaterial)polygon.Material).SetShaderParameter("fill_amount", attackTimePercentage * 100);
        } 
        else if (internalState == InternalState.TICK)
        {
            aoeStats.OnTick(delta);
        }

        if (Multiplayer.IsServer())
        {
            calculateArea();
        }
    }

    private void calculateArea(float angle = 360)
    {
        var piAngle = Mathf.DegToRad(angle);
        if (aoeStats.Radius > 0)
        {
            points.Clear();
            points.Add(Vector2.Zero);
            const int pointCount = 180;

            for (var i = 0; i < pointCount; i++)
            {
                var newPoint = rayTo(
                    new Vector2(0, aoeStats.Radius).Rotated((piAngle / pointCount) * i));
                points.Add(newPoint);
            }

            collisionPolygon.Polygon = points.ToArray();
        }
        QueueRedraw();
    }

    private Vector2 rayTo(Vector2 direction)
    {
        var destination = ToGlobal(direction);
        var query = new PhysicsRayQueryParameters2D()
        {
            From = GlobalPosition,
            To = destination,
            CollisionMask = 1+2,
            Exclude = new Array<Rid> { Caller.GetRid() },
        };
        
        var collision = _space.IntersectRay(query);
        var rayPosition = collision.TryGetValue("position", out var value) ? (Vector2)value : destination;
        return ToLocal(rayPosition);
    }

    public override void _Draw()
    {
        if (points.Count == 0)
            return;

        var from = points[0];
        var to  = Vector2.Zero;

        for (var i = 1; i < points.Count; i++)
        {
            to = points[i];
            DrawLine(from, to, new Godot.Color(1.0f, 1.0f, 1.0f, 0.5f));
            from = to;
        }
    }

    private Vector2 displayConePoint(float length, float angle)
    {
        var x = Mathf.Cos(angle) * length;
        var y = Mathf.Sin(angle) * length;
        var arcPosition = new Vector2(x, y);
        return arcPosition;
    }
}

public class AoeBaseStats
{
    public Node Parent { get; set; }
    public float Radius { get; set; }
    public float ActivationTime { get; set; }
    public float Duration { get; set; }
    public bool IsStationary { get; set; }
    public Action<bool> OnActivation { get; set; }
    public Action<bool> OnDeactivation { get; set; }
    public Action<double> OnTick { get; set; }
    
    
    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        var dict = new Godot.Collections.Dictionary<string, Variant>();
        return dict;
    }

    public static AoeBaseStats FromDict(Godot.Collections.Dictionary<string, Variant> dict, GameManager gameManager)
    {
        return new AoeBaseStats();
    }
}