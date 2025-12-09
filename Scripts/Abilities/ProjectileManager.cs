using System;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public class NetProjectileStats : IDictAble<NetProjectileStats>
{
    public float Speed = 0;
    public Vector2 StartPosition = new (-10000, -10000);
    public Vector2 Direction;
    public string SpritePath = string.Empty;
    public Vector2 Scale = Vector2.Zero;
    public Vector3 Color = Vector3.Zero;
    public float PullRadius = 0;
    public string CustomProjectilePath = string.Empty;
    public string Name;
    public MovementMode MovementMode = MovementMode.Straight;
    

    public Dictionary<string, Variant> ToDict()
    {
        return new Dictionary<string, Variant>()
        {
            { nameof(Speed), Speed },
            { nameof(StartPosition), StartPosition },
            { nameof(Direction), Direction },
            { nameof(SpritePath), SpritePath },
            { nameof(Scale), Scale },
            { nameof(Color), Color },
            { nameof(PullRadius), PullRadius },
            { nameof(CustomProjectilePath), CustomProjectilePath },
            { nameof(Name), Name},
            { nameof(MovementMode), (int)MovementMode },
        };
    }

    public static NetProjectileStats FromDict(Dictionary<string, Variant> dict)
    {
        return new NetProjectileStats()
        {
            Speed = (float)dict[nameof(Speed)],
            StartPosition = (Vector2)dict[nameof(StartPosition)],
            Direction = (Vector2)dict[nameof(Direction)],
            SpritePath = (string)dict[nameof(SpritePath)],
            Scale = (Vector2)dict[nameof(Scale)],
            Color = (Vector3)dict[nameof(Color)],
            PullRadius = (float)dict[nameof(PullRadius)],
            CustomProjectilePath = (string)dict[nameof(CustomProjectilePath)],
            Name = (string)dict[nameof(Name)],
            MovementMode = (MovementMode)(int)dict[nameof(MovementMode)],
        };
    }
}

public enum MovementMode
{
    Straight,
    Orbit,
    Curve,
}

public class ProjectileStats
{
    public float Speed = 0;
    public Damage Damage;
    public Vector2 StartPosition = new (-10000, -10000);
    public Vector2 Direction;
    public float TimeToBeALive = 0;
    public PlayerCharacter Caller;
    public string SpritePath = string.Empty;
    public int PiercingCount = 0;
    public int BouncingCount = 0;
    public Vector2 Scale = Vector2.Zero;
    public Vector3 Color = Vector3.Zero;
    public float KnockbackForce = 0;
    public float PullStrength = 0;
    public float PullRadius = 0;
    public string CustomProjectilePath = string.Empty;
    public Action<IHitableObject> CustomProjectileCollided;
    public string Name;
    public MovementMode MovementMode = MovementMode.Straight;
    public float Distance = -1;
    public uint CustomCollisionMask;
    public float AngleOffset = 0;

    public NetProjectileStats CreateNetProjectileStats()
    {
        return new NetProjectileStats()
        {
            Speed = Speed,
            Color = Color,
            CustomProjectilePath = CustomProjectilePath,
            Direction = Direction,
            PullRadius = PullRadius,
            Scale = Scale,
            SpritePath = SpritePath,
            StartPosition = StartPosition,
            Name = Name,
            MovementMode = MovementMode,
        };
    }
    
    public void FromNetProjectileStats(NetProjectileStats stats)
    {
        Speed = stats.Speed;
        Color = stats.Color;
        CustomProjectilePath = stats.CustomProjectilePath;
        Direction = stats.Direction;
        PullRadius = stats.PullRadius;
        Scale = stats.Scale;
        SpritePath = stats.SpritePath;
        StartPosition = stats.StartPosition;
        Name = stats.Name;
        MovementMode = stats.MovementMode;
    }
}

[GlobalClass]
public partial class ProjectileManager : Node
{
    private readonly PackedScene _projectileScene = GD.Load<PackedScene>("res://Scenes/Projectiles/Projectile.tscn");
    private readonly System.Collections.Generic.Dictionary<Node, Projectile> ProjectileOrbiter = new();

    public override void _EnterTree()
    {
        base._EnterTree();
        SetProcess(false);
        SetPhysicsProcess(false);
        SetMultiplayerAuthority(1);
    }

    public void CreateProjectile(ProjectileStats stats, Ability? ability = null)
    {
        Projectile projectile;
        if (string.IsNullOrEmpty(stats.Name))
        {
            stats.Name = $"p{Guid.NewGuid():N}";
        }
		
        if (!string.IsNullOrEmpty(stats.CustomProjectilePath))
        {
            projectile = GD.Load<PackedScene>(stats.CustomProjectilePath).Instantiate() as Projectile;
        }
        else
        {
            projectile = (Projectile)_projectileScene.Instantiate();
        }

        if (projectile == null)
        {
            return;
        }
        
        projectile.Name = stats.Name;
        projectile.AbilityGuid = ability?.GUID ?? string.Empty;

        if (stats.Caller != null)
        {
            stats.Direction = stats.Caller.GetLookAtDirection();
            stats.StartPosition = stats.Caller.GetProjectileStartPosition();
        }

        projectile.SetStats(stats);
        projectile.GlobalPosition = stats.StartPosition;
        
        if (Multiplayer.IsServer())
        {
            if (ability != null)
            {
                ability.RegisterSpawnedNode(projectile);
            }
            Rpc(MethodName.spawnOnClient, stats.CreateNetProjectileStats().ToDict());
        }
        AddChild(projectile);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void spawnOnClient(Variant data)
    {
        if (Multiplayer.IsServer())
        {
            return;
        }
        
        var netStats = NetProjectileStats.FromDict(data.AsGodotDictionary<string, Variant>());
        var stats = new ProjectileStats();
        stats.FromNetProjectileStats(netStats);
        CreateProjectile(stats);
    }
}