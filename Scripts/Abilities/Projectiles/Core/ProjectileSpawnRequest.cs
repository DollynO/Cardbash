using System;
using System.Collections.Generic;
using CardBase.Scripts.Abilities.ProjectileBehavior;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class ProjectileSpawnRequest
{
    public string CastGuid = Guid.NewGuid().ToString();
    public IEntityComponent Caller;
    public Vector2 StartPosition = new(-10000, -10000);
    public ProjectileVisualConfig Visual = new();
    public ProjectileMovementConfig Movement = new();
    public ProjectileCollisionConfig Collision = new();
    public ProjectileLifetimeConfig Lifetime = new();
    public ProjectilePullConfig Pull = new();
    public ProjectileHealthConfig Health = new();

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            { nameof(CastGuid), CastGuid },
            { nameof(Caller), Caller is Node2D callerNode ? callerNode.GetPath() : string.Empty },
            { "CallerPlayerId", Caller is PlayerCharacter callerPlayer ? callerPlayer.PlayerId : 0 },
            { nameof(StartPosition), StartPosition },
            { nameof(Visual), Visual.ToDict() },
            { nameof(Movement), Movement.ToDict() },
            { nameof(Collision), Collision.ToDict() },
            { nameof(Lifetime), Lifetime.ToDict() },
            { nameof(Pull), Pull.ToDict() },
            { nameof(Health), Health.ToDict() },
        };
    }

    public static ProjectileSpawnRequest FromDict(Godot.Collections.Dictionary<string, Variant> dict, GameManager manager)
    {
        return new ProjectileSpawnRequest
        {
            CastGuid = (string)dict[nameof(CastGuid)],
            Caller = ResolveCaller(dict, manager),
            StartPosition = (Vector2)dict[nameof(StartPosition)],
            Visual = ProjectileVisualConfig.FromDict(dict[nameof(Visual)].AsGodotDictionary<string, Variant>()),
            Movement = ProjectileMovementConfig.FromDict(dict[nameof(Movement)].AsGodotDictionary<string, Variant>()),
            Collision = ProjectileCollisionConfig.FromDict(dict[nameof(Collision)].AsGodotDictionary<string, Variant>()),
            Lifetime = ProjectileLifetimeConfig.FromDict(dict[nameof(Lifetime)].AsGodotDictionary<string, Variant>()),
            Pull = ProjectilePullConfig.FromDict(dict[nameof(Pull)].AsGodotDictionary<string, Variant>()),
            Health = ProjectileHealthConfig.FromDict(dict[nameof(Health)].AsGodotDictionary<string, Variant>()),
        };
    }

    private static IEntityComponent ResolveCaller(Godot.Collections.Dictionary<string, Variant> dict, GameManager manager)
    {
        if (dict.TryGetValue("CallerPlayerId", out var callerPlayerIdVariant)
            && (long)callerPlayerIdVariant != 0
            && manager.GetPlayerCharacter((long)callerPlayerIdVariant) is { } callerPlayer)
        {
            return callerPlayer;
        }

        var callerPath = dict.TryGetValue(nameof(Caller), out var callerPathVariant)
            ? (string)callerPathVariant
            : string.Empty;
        return string.IsNullOrEmpty(callerPath)
            ? null
            : manager.GetNodeOrNull<Node>(callerPath) as IEntityComponent;
    }
}

public class ProjectileRuntime
{
    public Action<IEntityComponent, Projectile> OnHit;
    public List<IProjectileBehavior> Behaviors = new();

    public static ProjectileRuntime Empty()
    {
        return new ProjectileRuntime();
    }
}

public class ProjectileVisualConfig
{
    public string ScenePath = string.Empty;
    public string AnimationPath = string.Empty;
    public Vector2 AnimationOffset = Vector2.Zero;
    public Vector2 Scale = Vector2.One;

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            { nameof(ScenePath), ScenePath },
            { nameof(AnimationPath), AnimationPath },
            { nameof(AnimationOffset), AnimationOffset },
            { nameof(Scale), Scale },
        };
    }

    public static ProjectileVisualConfig FromDict(Godot.Collections.Dictionary<string, Variant> dict)
    {
        return new ProjectileVisualConfig
        {
            ScenePath = (string)dict[nameof(ScenePath)],
            AnimationPath = (string)dict[nameof(AnimationPath)],
            AnimationOffset = (Vector2)dict[nameof(AnimationOffset)],
            Scale = (Vector2)dict[nameof(Scale)],
        };
    }
}

public class ProjectileMovementConfig
{
    public float Speed;
    public Vector2 Direction;
    public MovementMode Mode = MovementMode.STRAIGHT;
    public int BounceCount;
    public float AngleOffset;

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            { nameof(Speed), Speed },
            { nameof(Direction), Direction },
            { nameof(Mode), (int)Mode },
            { nameof(BounceCount), BounceCount },
            { nameof(AngleOffset), AngleOffset },
        };
    }

    public static ProjectileMovementConfig FromDict(Godot.Collections.Dictionary<string, Variant> dict)
    {
        return new ProjectileMovementConfig
        {
            Speed = (float)dict[nameof(Speed)],
            Direction = (Vector2)dict[nameof(Direction)],
            Mode = (MovementMode)(int)dict[nameof(Mode)],
            BounceCount = (int)dict[nameof(BounceCount)],
            AngleOffset = (float)dict[nameof(AngleOffset)],
        };
    }
}

public class ProjectileCollisionConfig
{
    public int PierceCount;
    public uint CollisionMask;

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            { nameof(PierceCount), PierceCount },
            { nameof(CollisionMask), CollisionMask },
        };
    }

    public static ProjectileCollisionConfig FromDict(Godot.Collections.Dictionary<string, Variant> dict)
    {
        return new ProjectileCollisionConfig
        {
            PierceCount = (int)dict[nameof(PierceCount)],
            CollisionMask = (uint)dict[nameof(CollisionMask)],
        };
    }
}

public class ProjectileLifetimeConfig
{
    public float Seconds;

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            { nameof(Seconds), Seconds },
        };
    }

    public static ProjectileLifetimeConfig FromDict(Godot.Collections.Dictionary<string, Variant> dict)
    {
        return new ProjectileLifetimeConfig
        {
            Seconds = (float)dict[nameof(Seconds)],
        };
    }
}

public class ProjectilePullConfig
{
    public float Radius;
    public float Strength;

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            { nameof(Radius), Radius },
            { nameof(Strength), Strength },
        };
    }

    public static ProjectilePullConfig FromDict(Godot.Collections.Dictionary<string, Variant> dict)
    {
        return new ProjectilePullConfig
        {
            Radius = (float)dict[nameof(Radius)],
            Strength = (float)dict[nameof(Strength)],
        };
    }
}

public class ProjectileHealthConfig
{
    public float Life = 1;

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            { nameof(Life), Life },
        };
    }

    public static ProjectileHealthConfig FromDict(Godot.Collections.Dictionary<string, Variant> dict)
    {
        return new ProjectileHealthConfig
        {
            Life = (float)dict[nameof(Life)],
        };
    }
}

public enum MovementMode
{
    NONE,
    STRAIGHT,
    CURVE
}
