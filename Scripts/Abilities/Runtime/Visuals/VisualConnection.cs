using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class VisualConnectionStats
{
    public Node2D FromNode { get; set; }
    public Node2D ToNode { get; set; }
    public Vector2 FromPosition { get; set; }
    public Vector2 ToPosition { get; set; }
    public bool FollowEndpoints { get; set; }
    public float Duration { get; set; } = 0.18f;
    public float Width { get; set; } = 5f;
    public Color Color { get; set; } = new(0.45f, 0.85f, 1f, 0.95f);
    public int ZIndex { get; set; } = 3;

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            { nameof(FromPosition), FromPosition },
            { nameof(ToPosition), ToPosition },
            { nameof(FollowEndpoints), FollowEndpoints },
            { nameof(Duration), Duration },
            { nameof(Width), Width },
            { nameof(Color), Color },
            { nameof(ZIndex), ZIndex },
            { "FromPath", FromNode?.GetPath() ?? string.Empty },
            { "ToPath", ToNode?.GetPath() ?? string.Empty },
            { "FromPlayerId", FromNode is PlayerCharacter fromPlayer ? fromPlayer.PlayerId : 0 },
            { "ToPlayerId", ToNode is PlayerCharacter toPlayer ? toPlayer.PlayerId : 0 },
        };
    }

    public static VisualConnectionStats FromDict(Godot.Collections.Dictionary<string, Variant> dict, GameManager gameManager)
    {
        return new VisualConnectionStats
        {
            FromNode = ResolveNode(dict, gameManager, "FromPlayerId", "FromPath"),
            ToNode = ResolveNode(dict, gameManager, "ToPlayerId", "ToPath"),
            FromPosition = (Vector2)dict[nameof(FromPosition)],
            ToPosition = (Vector2)dict[nameof(ToPosition)],
            FollowEndpoints = (bool)dict[nameof(FollowEndpoints)],
            Duration = (float)dict[nameof(Duration)],
            Width = (float)dict[nameof(Width)],
            Color = (Color)dict[nameof(Color)],
            ZIndex = (int)dict[nameof(ZIndex)],
        };
    }

    private static Node2D ResolveNode(
        Godot.Collections.Dictionary<string, Variant> dict,
        GameManager gameManager,
        string playerIdKey,
        string pathKey)
    {
        if (gameManager == null)
        {
            return null;
        }

        if (dict.TryGetValue(playerIdKey, out var playerIdVariant)
            && (long)playerIdVariant != 0
            && gameManager.GetPlayerCharacter((long)playerIdVariant) is { } player)
        {
            return player;
        }

        var path = dict.TryGetValue(pathKey, out var pathVariant)
            ? (string)pathVariant
            : string.Empty;

        return string.IsNullOrEmpty(path) ? null : gameManager.GetNodeOrNull<Node2D>(path);
    }
}

public partial class VisualConnection : Node2D
{
    private VisualConnectionStats stats;
    private Line2D line;

    public void Initialize(VisualConnectionStats connectionStats)
    {
        stats = connectionStats;
    }

    public override void _Ready()
    {
        if (stats == null)
        {
            QueueFree();
            return;
        }

        SetMultiplayerAuthority(1);
        ZAsRelative = false;
        ZIndex = stats.ZIndex;

        line = new Line2D
        {
            Width = stats.Width,
            DefaultColor = stats.Color,
        };
        AddChild(line);
        UpdateLine();
        StartLifetime();
    }

    public override void _Process(double delta)
    {
        if (stats?.FollowEndpoints == true)
        {
            UpdateLine();
        }
    }

    private void UpdateLine()
    {
        var from = ResolvePosition(stats.FromNode, stats.FromPosition);
        var to = ResolvePosition(stats.ToNode, stats.ToPosition);

        GlobalPosition = from;
        line.Points = new[] { Vector2.Zero, to - from };
    }

    private static Vector2 ResolvePosition(Node2D node, Vector2 fallback)
    {
        if (node is IEntityComponent entity && entity.TryGetComponent(out AimComponent aimComponent))
        {
            return aimComponent.GetCharacterCenterPosition();
        }

        return node?.GlobalPosition ?? fallback;
    }

    private void StartLifetime()
    {
        if (stats.Duration <= 0)
        {
            return;
        }

        var tween = CreateTween();
        tween.TweenProperty(line, "modulate:a", 0f, stats.Duration);

        if (Multiplayer.IsServer())
        {
            tween.TweenCallback(Callable.From(QueueFree));
        }
    }
}
