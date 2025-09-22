using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public class MeleeConeProperties : IDictAble<MeleeConeProperties>
{
    public float Angle { get; init; }
    public float Offset { get; init; }
    public float Length { get; init; }
    public float AttackTime { get; init; }
    public PlayerCharacter Owner { get; set; }
    public Damage Damage { get; init; }
    public string AbilityGUID { get; init; }

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        var dict = new Godot.Collections.Dictionary<string, Variant>()
        {
            ["Angle"] = Angle,
            ["Offset"] = Offset,
            ["Length"] = Length,
            ["AttackTime"] = AttackTime,
            ["OwnerId"] = Owner.PlayerId,
            ["Damage"] = Damage.ToDict(),
            ["AbilityGUID"] = AbilityGUID,
        };
        
        return dict;
    }
    
    public static MeleeConeProperties FromDict(Godot.Collections.Dictionary<string, Variant> dict)
    {
        return new MeleeConeProperties
        {
            Angle = (float)dict["Angle"],
            Offset = (float)dict["Offset"],
            Length = (float)dict["Length"],
            AttackTime = (float)dict["AttackTime"],
            Damage = Damage.FromDict((Godot.Collections.Dictionary<string, Variant>)dict["Damage"]),
            AbilityGUID = (string)dict["AbilityGUID"],
        };
    } 
}

[GlobalClass]
public partial class MeleeCone : Node2D
{
    [Export] public float Angle;
    [Export] public float Offset;
    [Export] public float Length;
    [Export] public PlayerCharacter Owner;
    [Export] public float AttackTime;
    public string AbilityGuid;
    private Damage Damage = new();
    
    private float piOffset;
    private List<Vector2> points = new();
    private List<Vector2> displayPoints = new();
    private uint collisionLayerMask = 2;
    private CollisionPolygon2D collisionPolygon = new();
    private Area2D detectArea = new();
    private Polygon2D polygon = new();
    private Shader fillAmountShader = GD.Load<Shader>("res://Shaders/MeleeConeFillShader.gdshader");
    private Texture2D fillAmountTexture = GD.Load<Texture2D>("res://Sprites/whiteBox.png");

    private Color outline = Colors.Aqua;
    private Color fillColor = Colors.Aqua;
    private PhysicsDirectSpaceState2D _space;
    private Node2D Parent;
    private float attackTimeCount;

    public override void _Ready()
    {
        _space = GetWorld2D().GetDirectSpaceState();
        Parent = GetParent() as Node2D;
        piOffset = Mathf.DegToRad(Offset);
        
        AddChild(polygon);
        fillColor.A = 0.5f;
        polygon.Color = fillColor;

        var shaderMaterial = new ShaderMaterial();
        shaderMaterial.Shader = fillAmountShader;
        polygon.Material = shaderMaterial;
        polygon.Texture = fillAmountTexture;
        
        var attackTimer = new Timer();
        AddChild(attackTimer);
        attackTimer.Timeout += onAttack;
        attackTimer.Start(AttackTime);
        
        if (Multiplayer.IsServer())
        {
            AddChild(detectArea);
            detectArea.CollisionMask = 4;
            detectArea.AddChild(collisionPolygon);
        }

        displayPoints.Clear();
        var piAngle = Mathf.DegToRad(Angle);
        var uvPoints = new List<Vector2>();
        uvPoints.Add(Vector2.Zero);
        displayPoints.Add(Vector2.Zero);
        if (Length > 0)
        {
            for (var i = 0; i < Angle * 2; i++)
            {
                var point = displayConePoint(Length, piAngle / 2, (float)i / ((float)Angle*2), piOffset + Mathf.Pi / 2);
                displayPoints.Add(point);
                uvPoints.Add(point.Normalized() / 2);
                
            }
        }

        polygon.Polygon = displayPoints.ToArray();
        polygon.UV = uvPoints.ToArray();
    }

    public void SetStats(MeleeConeProperties stats)
    {
        this.Angle = stats.Angle;
        this.Offset = stats.Offset;
        this.Length = stats.Length;
        this.AttackTime = stats.AttackTime;
        this.Owner = stats.Owner;
        this.Damage = stats.Damage;
        this.AbilityGuid = stats.AbilityGUID;

    }
    
    /**
     * @brief Triggered after the attack time is over.
     */
    private void onAttack()
    {
        if (Multiplayer.IsServer())
        {
            var bodies = detectArea.GetOverlappingBodies();
            foreach (var body in bodies)
            {
                if (body is IHitableObject hitableObject)
                {
                    if ((hitableObject is PlayerCharacter player && player.TeamId != Owner.TeamId) 
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
                    }
                }
            }
        }
        
        QueueFree();
    }
    
    public override void _PhysicsProcess(double delta)
    {
        attackTimeCount += (float)delta;
        var attackTimePercentage = attackTimeCount / AttackTime;
        ((ShaderMaterial)polygon.Material).SetShaderParameter("fill_amount", attackTimePercentage * 100);
        if (Multiplayer.IsServer())
        {

            var piAngle = Mathf.DegToRad(Angle);
            if (Length > 0)
            {
                points.Clear();
                points.Add(Vector2.Zero);
                var pointCount = Angle < 50 ? Angle * 2 : 100;
                var angleOffset = -piAngle / 2 + piOffset;

                for (var i = 0; i < pointCount; i++)
                {
                    var newPoint = rayTo(
                        new Vector2(0, Length).Rotated((piAngle / pointCount) * i + angleOffset));
                    points.Add(newPoint);
                }

                collisionPolygon.Polygon = points.ToArray();
            }
            QueueRedraw();
        }
    }

    private Vector2 rayTo(Vector2 direction)
    {
        var destination = ToGlobal(direction);
        var query = new PhysicsRayQueryParameters2D()
        {
            From = GlobalPosition,
            To = destination,
            CollisionMask = 1+2,
            Exclude = new Array<Rid> { Owner.GetRid() },
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
            DrawLine(from, to, new Color(1.0f, 1.0f, 1.0f, 0.5f));
            from = to;
        }
    }

    private Vector2 displayConePoint(float length, float halfAngle, float segmantFraction, float offset)
    {
        var angle = offset -halfAngle + segmantFraction * (2f * halfAngle);
        var x = Mathf.Cos(angle) * length;
        var y = Mathf.Sin(angle) * length;
        var arcPosition = new Vector2(x, y);
        return arcPosition;
    }
}