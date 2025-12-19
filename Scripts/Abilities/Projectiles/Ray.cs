using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public partial class Ray : Node2D
{
    [Export] private Line2D _innerLine;
    [Export] private Area2D _collisionArea;
    private RayStats _rayStats;
    private PhysicsDirectSpaceState2D _state;

    private SpriteFrames _frames;
    private List<Texture2D> _centerTextureList;
    private AnimatedSprite2D _originSprite;
    private AnimatedSprite2D _endSprite;

    public Ray(RayStats rayStats)
    {
        _rayStats = rayStats;

    }

    public override void _Ready()
    {
        SetMultiplayerAuthority(1);
        
        _innerLine = new Line2D();
        AddChild(_innerLine);
        _innerLine.TextureMode = Line2D.LineTextureMode.Tile;
        
        _originSprite = new AnimatedSprite2D();
        AddChild(_originSprite);
        
        _frames = IconLoader.Instance.LoadAnimation(_rayStats.AnimationResource);
        _centerTextureList = IconLoader.Instance.LoadSingleAnimation(_rayStats.CenterLoopFolder, "frame", _rayStats.CenterLoopCount);
        _originSprite.SpriteFrames = _frames;
        _originSprite.Animation = "OriginLoop";
        _originSprite.Rotate(Mathf.Pi / 2);
        var width = _originSprite.SpriteFrames.GetFrameTexture(_originSprite.Animation, 0).GetWidth();
        _originSprite.Offset = new Vector2(width / 2, 0);
        
        _endSprite = new AnimatedSprite2D();
        AddChild(_endSprite);
        _endSprite.SpriteFrames = _frames;
        _endSprite.Animation = "EndLoop";
        _endSprite.Rotate(Mathf.Pi / 2);
        width = _endSprite.SpriteFrames.GetFrameTexture(_endSprite.Animation, 0).GetWidth();
        _endSprite.Offset = new Vector2(-width / 4, 0);
        
        _endSprite.Play();
        _originSprite.Play();

        this.ZAsRelative = false;
        this.ZIndex = 2;
        
        _innerLine.Texture = _centerTextureList[0];
        _innerLine.Width = _innerLine.Texture.GetHeight();

        if (!Multiplayer.IsServer())
        {
            SetPhysicsProcess(false);
            return;
        }
        
        _state = GetWorld2D().GetDirectSpaceState();

    }

    public override void _Process(double delta)
    {
        var frameSprite = _centerTextureList[_originSprite.Frame];
        if (frameSprite != _innerLine.Texture)
        {
            _innerLine.Texture = frameSprite;
        }

        if (_innerLine.Points.Length > 1)
        {
            _endSprite.GlobalPosition = ToGlobal(_innerLine.Points[1]);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var from = GlobalPosition + this._rayStats.Caster.GetLookAtDirection() * 32;
        var max_to = GlobalPosition + this._rayStats.Caster.GetLookAtDirection() * _rayStats.Range;
        var to = Vector2.Zero;
        var tmp_from = from;
        var hitPlayers = new List<PlayerCharacter>();
        
        while(to.DistanceSquaredTo(from) < max_to.DistanceSquaredTo(from) && hitPlayers.Count < _rayStats.PierceCount + 1)
        {
            var query = new PhysicsRayQueryParameters2D
            {
                From = tmp_from,
                To = max_to,
                CollisionMask = _rayStats.CollisionMask,
                Exclude = new Array<Rid> { (_rayStats.Caster).GetRid() },
            };
            var results = _state.IntersectRay(query);
            if (results.Count > 0)
            {
                var collider = (GodotObject)results["collider"];
                to = results.TryGetValue("position", out var value) ? (Vector2)value : max_to;
                if (collider is PlayerCharacter player)
                {
                    hitPlayers.Add(player);
                }
                tmp_from = to;
            }
            else
            {
                to = max_to;
            }
        }

        foreach (var player in hitPlayers)
        {
            _rayStats.CollisionTick(player, (float)delta);
        }

        var newPoints = new [] { ToLocal(from), ToLocal(to) };
        var dict = new Godot.Collections.Dictionary<string, Variant>
        {
            ["points"] = newPoints
        };
        Rpc(MethodName.syncClient, dict);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void syncClient(Godot.Collections.Dictionary<string, Variant> dict)
    {
        var newPoints = (Vector2[])dict["points"];
        _innerLine.Points = newPoints;
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
    public float Range;
    public PlayerCharacter Caster;
    public uint CollisionMask = 4 + 1;
    public Action<IHitableObject, float> CollisionTick;

    public string AnimationResource;
    public string CenterLoopFolder;
    public int CenterLoopCount;
    
    public int PierceCount;

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        return new Godot.Collections.Dictionary<string, Variant>()
        {
            { nameof(Range), Range },
            { nameof(Caster), Caster.PlayerId },
            { nameof(AnimationResource), AnimationResource},
            { nameof(CenterLoopFolder), CenterLoopFolder},
            { nameof(CenterLoopCount), CenterLoopCount},
            { nameof(PierceCount), PierceCount}
        };
    }

    public static RayStats FromDict(Godot.Collections.Dictionary<string, Variant> dict, GameManager manager)
    {
        return new RayStats()
        {
            Range = (float)dict[nameof(Range)],
            Caster = manager.GetPlayerCharacter((long)dict[nameof(Caster)]),
            CollisionMask = 0,
            CollisionTick = null,
            AnimationResource = (string)dict[nameof(AnimationResource)],
            CenterLoopFolder = (string)dict[nameof(CenterLoopFolder)],
            CenterLoopCount = (int)dict[nameof(CenterLoopCount)],
            PierceCount = (int)dict[nameof(PierceCount)],
        };
    }
}