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
    private RayStats _rayStats;

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

        ReplicationHelper.CreateSynchronizer(this,
            new ReplicationProperties(":position", SceneReplicationConfig.ReplicationMode.Always),
            new ReplicationProperties(":rotation", SceneReplicationConfig.ReplicationMode.Always));

        if (!Multiplayer.IsServer())
        {
            SetPhysicsProcess(false);
            return;
        }

        _shapeCast2D = new ShapeCast2D();
        AddChild(_shapeCast2D);
        var rectShape = new RectangleShape2D();
        rectShape.Size = new Vector2(5, _innerLine.Width / 2f);
        _shapeCast2D.Shape = rectShape;

        _shapeCast2D.CollisionMask = _rayStats.CollisionMask;
        if (_rayStats.Caster is CharacterbodyEntityComponent cec)
        {
            _shapeCast2D.AddExceptionRid(cec.GetRid());
        }

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
            var start = _innerLine.Points[0];
            var end = _innerLine.Points[1];
            var direction = end - start;
            if (direction != Vector2.Zero)
            {
                var spriteRotation = direction.Angle();
                _originSprite.Rotation = spriteRotation;
                _endSprite.Rotation = spriteRotation;
            }

            _endSprite.GlobalPosition = ToGlobal(end);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_rayStats.Caster == null)
        {
            return;
        }

        this.GlobalPosition = ((Node2D)_rayStats.Caster).GlobalPosition;

        if (!_rayStats.Caster.TryGetComponent(out AimComponent aimComponent))
        {
            return;
        }

        var pierceCount = _rayStats.PierceCount + 1;
        var direction = aimComponent.GetLookAtDirection();
        if (direction == Vector2.Zero)
        {
            return;
        }

        _shapeCast2D.Position = direction * 32;
        _shapeCast2D.Rotation = direction.Angle();
        _shapeCast2D.TargetPosition = Vector2.Right * Mathf.Max(_rayStats.Range - 32, 0);
        _shapeCast2D.ForceShapecastUpdate();

        var from = ToGlobal(_shapeCast2D.Position);
        var to = GlobalPosition + direction * _rayStats.Range;

        var hittedObjects = new List<IEntityComponent>();
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
                        lastHitPosition = hitPosition;

                        if (hitObject is IEntityComponent hitableObject)
                        {
                            hittedObjects.Add(hitableObject);
                            _shapeCast2D.AddExceptionRid(_shapeCast2D.GetColliderRid(i));
                            pierceCount--;
                        }
                        else
                        {
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

            foreach (var hitableObject in hittedObjects.Distinct())
            {
                _rayStats.CollisionTick?.Invoke(hitableObject, (float)delta);
            }

            var toHit = lastHitPosition - from;
            var length = toHit.Dot(direction);
            to = GlobalPosition + direction * length;
        }

        var newPoints = new[] { ToLocal(GlobalPosition + aimComponent.GetLookAtDirection() * 32), ToLocal(to) };
        var dict = new Godot.Collections.Dictionary<string, Variant>
        {
            ["global_position"] = GlobalPosition,
            ["points"] = newPoints
        };
        Rpc(MethodName.syncClient, dict);

        _shapeCast2D.ClearExceptions();
        if (_rayStats.Caster is CharacterbodyEntityComponent cec)
        {
            _shapeCast2D.AddExceptionRid(cec.GetRid());
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void syncClient(Godot.Collections.Dictionary<string, Variant> dict)
    {
        GlobalPosition = (Vector2)dict["global_position"];
        var newPoints = (Vector2[])dict["points"];
        _innerLine.Points = newPoints;
    }

    public void SetCollisionTick(Action<IEntityComponent, float> collisionTick)
    {
        _rayStats.CollisionTick = collisionTick;
    }

    public void Destroy()
    {
        Rpc(MethodName.destroyOnClient);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void destroyOnClient()
    {
        this.QueueFree();
    }
}

public class RayStats
{
    public float Range;
    public IEntityComponent Caster;
    public uint CollisionMask = 4 + 1;
    public Action<IEntityComponent, float> CollisionTick;

    public string AnimationResource;
    public string CenterLoopFolder;
    public int CenterLoopCount;

    public int PierceCount;

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        return new Godot.Collections.Dictionary<string, Variant>()
        {
            { nameof(Range), Range },
            { nameof(Caster), Caster is Node2D casterNode ? casterNode.GetPath() : string.Empty },
            { "CasterPlayerId", Caster is PlayerCharacter casterPlayer ? casterPlayer.PlayerId : 0 },
            { nameof(CollisionMask), CollisionMask },
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
            Caster = ResolveCaster(dict, manager),
            CollisionMask = (uint)(int)dict[nameof(CollisionMask)],
            CollisionTick = null,
            AnimationResource = (string)dict[nameof(AnimationResource)],
            CenterLoopFolder = (string)dict[nameof(CenterLoopFolder)],
            CenterLoopCount = (int)dict[nameof(CenterLoopCount)],
            PierceCount = (int)dict[nameof(PierceCount)],
        };
    }

    private static IEntityComponent ResolveCaster(Godot.Collections.Dictionary<string, Variant> dict, GameManager manager)
    {
        if (dict.TryGetValue("CasterPlayerId", out var casterPlayerIdVariant)
            && (long)casterPlayerIdVariant != 0
            && manager.GetPlayerCharacter((long)casterPlayerIdVariant) is { } casterPlayer)
        {
            return casterPlayer;
        }

        var casterPath = dict.TryGetValue(nameof(Caster), out var casterPathVariant)
            ? (string)casterPathVariant
            : string.Empty;
        return string.IsNullOrEmpty(casterPath)
            ? null
            : manager.GetNodeOrNull<Node>(casterPath) as IEntityComponent;
    }
}
