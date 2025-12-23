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

    private ShapeCast2D _shapeCast2D;

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

        _shapeCast2D = new ShapeCast2D();
        AddChild(_shapeCast2D);
        _state = GetWorld2D().GetDirectSpaceState();
        var rectShape = new RectangleShape2D();
        rectShape.Size = new Vector2(5, width); // width x length
        _shapeCast2D.Shape = rectShape;
        
        _shapeCast2D.Position = Vector2.Zero;
        _shapeCast2D.TargetPosition = Vector2.Right * _rayStats.Range;
        _shapeCast2D.Rotation = Mathf.Pi / 2;
        _shapeCast2D.CollisionMask = _rayStats.CollisionMask;
        _shapeCast2D.AddExceptionRid((_rayStats.Caster).GetRid());
        _shapeCast2D.MaxResults = 10;
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
        var pierceCount = _rayStats.PierceCount + 1;
        var from = ToGlobal(_shapeCast2D.Position);
        var direction = _rayStats.Caster.GetLookAtDirection();
        var to = GlobalPosition + direction * _rayStats.Range;

        var hittedObjects = new List<IHitableObject>();
        var lastHitPosition = Vector2.Zero;
        
        if (_shapeCast2D.IsColliding())
        {
            while (pierceCount > 0)
            {
                _shapeCast2D.ForceShapecastUpdate();
                if (_shapeCast2D.IsColliding())
                {
                    for (var i = 0; i < _shapeCast2D.GetCollisionCount(); i++)
                    {
                        var hitPosition = _shapeCast2D.GetCollisionPoint(i);
                        var hitObject = _shapeCast2D.GetCollider(i);
                        
                        if (hitObject is IHitableObject hitableObject)
                        {
                            hittedObjects.Add(hitableObject);
                            _shapeCast2D.AddExceptionRid(_shapeCast2D.GetColliderRid(i));
                            pierceCount--;
                        }
                        else
                        {
                            lastHitPosition = hitPosition;
                            pierceCount = 0;
                        }

                        if (pierceCount == 0)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    lastHitPosition = to;
                    pierceCount = 0;
                }
            }

            foreach (var hitableObject in hittedObjects)
            {
                _rayStats.CollisionTick(hitableObject, (float)delta);
            }
            
            var toHit = lastHitPosition - from;
            var length = toHit.Dot(direction);
            to = GlobalPosition + direction * length;
        }
        
        var newPoints = new [] { ToLocal(GlobalPosition + _rayStats.Caster.GetLookAtDirection() * 32), ToLocal(to)};
        var dict = new Godot.Collections.Dictionary<string, Variant>
        {
            ["points"] = newPoints
        };
        Rpc(MethodName.syncClient, dict);
        
        _shapeCast2D.ClearExceptions();
        _shapeCast2D.AddExceptionRid((_rayStats.Caster).GetRid());
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