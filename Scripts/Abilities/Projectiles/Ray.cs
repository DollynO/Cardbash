using System;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public partial class Ray : Node2D
{
    [Export] private Line2D _innerLine;
    [Export] private Line2D _outerLine;
    [Export] private Area2D _collisionArea;
    private RayStats _rayStats;
    private PhysicsDirectSpaceState2D _state;

    public Ray(RayStats rayStats)
    {
        _rayStats = rayStats;

    }

    public override void _Ready()
    {
        SetMultiplayerAuthority(1);
        
        _innerLine = new Line2D();
        this.AddChild(_innerLine);
        _innerLine.DefaultColor = _rayStats.InnerColor;
        _outerLine = new Line2D();
        this.AddChild(_outerLine);
        _outerLine.DefaultColor = _rayStats.OuterColor;
        
        _innerLine.Width = 10;
        _outerLine.Width = 20;

        if (!Multiplayer.IsServer())
        {
            SetPhysicsProcess(false);
            return;
        }
        
        _state = GetWorld2D().GetDirectSpaceState();

    }

    public override void _Process(double delta)
    {
        
    }

    public override void _PhysicsProcess(double delta)
    {
        var from = GlobalPosition;
        var to = GlobalPosition + (this._rayStats.Caster.GetLookAtDirection()) * _rayStats.Range;
        var query = new PhysicsRayQueryParameters2D
        {
            From = from,
            To = to,
            CollisionMask = _rayStats.CollisionMask,
            Exclude = new Array<Rid> { (_rayStats.Caster).GetRid() },
        };

        var results = _state.IntersectRay(query);

        if (results.Count > 0)
        {
            var collider = (GodotObject)results["collider"];
            if (collider is PlayerCharacter player)
            {
                _rayStats.CollisionTick(player, (float)delta);
            }
            to = results.TryGetValue("position", out var value) ? (Vector2)value : to;
        }

        var newPoints = new [] { ToLocal(from), ToLocal(to) };
        var dict = new Dictionary<string, Variant>
        {
            ["points"] = newPoints
        };
        Rpc(MethodName.syncClient, dict);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void syncClient(Dictionary<string, Variant> dict)
    {
        var newPoints = (Vector2[])dict["points"];
        _innerLine.Points = newPoints;
        _outerLine.Points = newPoints;
    }

    public void Destroy()
    {
        Rpc(MethodName.destroyOnClient);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,  CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void destroyOnClient()
    {
        this.QueueFree();
    }
}

public class RayStats
{
    public Color InnerColor;
    public Color OuterColor;
    public float Range;
    public PlayerCharacter Caster;
    public uint CollisionMask = 4 + 1;
    public Action<IHitableObject, float> CollisionTick;

    public Dictionary<string, Variant> ToDict()
    {
        return new Dictionary<string, Variant>()
        {
            { nameof(InnerColor), InnerColor.ToHtml() },
            { nameof(OuterColor), OuterColor.ToHtml() },
            { nameof(Range), InnerColor.ToHtml() },
            { nameof(Caster), Caster.PlayerId },
        };
    }

    public static RayStats FromDict(Dictionary<string, Variant> dict, GameManager manager)
    {
        return new RayStats()
        {
            InnerColor = Color.FromHtml((string)dict[nameof(InnerColor)]),
            OuterColor = Color.FromHtml((string)dict[nameof(OuterColor)]),
            Range = (float)dict[nameof(Range)],
            Caster = manager.GetPlayerCharacter((long)dict[nameof(Caster)]),
            CollisionMask = 0,
            CollisionTick = null,
        };
    }
}