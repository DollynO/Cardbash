using System;
using System.Collections.Generic;
using CardBase.Scripts;
using CardBase.Scripts.PlayerScripts;
using Godot;

public partial class BattleRoyaleArea : Node2D, IEntityComponent
{
    private const float DamageTickInterval = 0.25f;
    private const int CircleSegments = 128;
    private readonly Dictionary<Type, IComponent> _components = new();
    private readonly RandomNumberGenerator _random = new();

    private GameManager _gameManager;
    private double _roundElapsed;
    private double _damageElapsed;
    private bool _isRunning;
    private bool _isShrinking;
    private Vector2 _mapPosition;
    private Vector2 _mapSize;
    private Vector2 _initialCenter;
    private Vector2 _targetCenter;
    private Vector2 _currentCenter;
    private float _initialRadius;
    private float _currentRadius;
    private float _finalRadius;
    private float _startDelaySeconds = 120f;
    private float _shrinkSpeed = 35f;
    private float _shrinkDurationSeconds = 1f;
    private float _trueDamagePerSecond = 8f;

    public EventBus EventBus => EventBus.Instance;

    [Export]
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            _isRunning = value;
            QueueRedraw();
        }
    }

    [Export]
    public bool IsShrinking
    {
        get => _isShrinking;
        set
        {
            _isShrinking = value;
            QueueRedraw();
        }
    }

    [Export]
    public Vector2 MapPosition
    {
        get => _mapPosition;
        set
        {
            _mapPosition = value;
            QueueRedraw();
        }
    }

    [Export]
    public Vector2 MapSize
    {
        get => _mapSize;
        set
        {
            _mapSize = value;
            QueueRedraw();
        }
    }

    [Export]
    public Vector2 InitialCenter
    {
        get => _initialCenter;
        set
        {
            _initialCenter = value;
            QueueRedraw();
        }
    }

    [Export]
    public Vector2 TargetCenter
    {
        get => _targetCenter;
        set
        {
            _targetCenter = value;
            QueueRedraw();
        }
    }

    [Export]
    public Vector2 CurrentCenter
    {
        get => _currentCenter;
        set
        {
            _currentCenter = value;
            QueueRedraw();
        }
    }

    [Export]
    public float InitialRadius
    {
        get => _initialRadius;
        set
        {
            _initialRadius = value;
            QueueRedraw();
        }
    }

    [Export]
    public float CurrentRadius
    {
        get => _currentRadius;
        set
        {
            _currentRadius = value;
            QueueRedraw();
        }
    }

    [Export]
    public float FinalRadius
    {
        get => _finalRadius;
        set
        {
            _finalRadius = value;
            QueueRedraw();
        }
    }

    [Export]
    public float StartDelaySeconds
    {
        get => _startDelaySeconds;
        set => _startDelaySeconds = value;
    }

    [Export]
    public float ShrinkSpeed
    {
        get => _shrinkSpeed;
        set => _shrinkSpeed = value;
    }

    [Export]
    public float ShrinkDurationSeconds
    {
        get => _shrinkDurationSeconds;
        set => _shrinkDurationSeconds = value;
    }

    [Export]
    public float TrueDamagePerSecond
    {
        get => _trueDamagePerSecond;
        set => _trueDamagePerSecond = value;
    }

    public override void _Ready()
    {
        Name = nameof(BattleRoyaleArea);
        ZIndex = 50;
        _random.Randomize();
        _gameManager = GetParent<GameManager>();
        ReplicationHelper.CreateSynchronizer(this,
            new ReplicationProperties(new NodePath($":{nameof(IsRunning)}"), SceneReplicationConfig.ReplicationMode.OnChange),
            new ReplicationProperties(new NodePath($":{nameof(IsShrinking)}"), SceneReplicationConfig.ReplicationMode.OnChange),
            new ReplicationProperties(new NodePath($":{nameof(MapPosition)}"), SceneReplicationConfig.ReplicationMode.OnChange),
            new ReplicationProperties(new NodePath($":{nameof(MapSize)}"), SceneReplicationConfig.ReplicationMode.OnChange),
            new ReplicationProperties(new NodePath($":{nameof(InitialCenter)}"), SceneReplicationConfig.ReplicationMode.OnChange),
            new ReplicationProperties(new NodePath($":{nameof(TargetCenter)}"), SceneReplicationConfig.ReplicationMode.OnChange),
            new ReplicationProperties(new NodePath($":{nameof(CurrentCenter)}"), SceneReplicationConfig.ReplicationMode.OnChange),
            new ReplicationProperties(new NodePath($":{nameof(InitialRadius)}"), SceneReplicationConfig.ReplicationMode.OnChange),
            new ReplicationProperties(new NodePath($":{nameof(CurrentRadius)}"), SceneReplicationConfig.ReplicationMode.OnChange),
            new ReplicationProperties(new NodePath($":{nameof(FinalRadius)}"), SceneReplicationConfig.ReplicationMode.OnChange),
            new ReplicationProperties(new NodePath($":{nameof(StartDelaySeconds)}"), SceneReplicationConfig.ReplicationMode.OnChange),
            new ReplicationProperties(new NodePath($":{nameof(ShrinkSpeed)}"), SceneReplicationConfig.ReplicationMode.OnChange),
            new ReplicationProperties(new NodePath($":{nameof(ShrinkDurationSeconds)}"), SceneReplicationConfig.ReplicationMode.OnChange),
            new ReplicationProperties(new NodePath($":{nameof(TrueDamagePerSecond)}"), SceneReplicationConfig.ReplicationMode.OnChange));
    }

    public void ServerStart(GameModeSettings settings, Rect2 mapBounds)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        _roundElapsed = 0;
        _damageElapsed = 0;
        MapPosition = mapBounds.Position;
        MapSize = mapBounds.Size;
        InitialCenter = mapBounds.Position + mapBounds.Size / 2f;
        TargetCenter = PickTargetCenter(mapBounds, settings.BattleRoyaleTargetMargin);
        InitialRadius = GetCoveringRadius(InitialCenter, mapBounds);
        FinalRadius = Mathf.Clamp(settings.BattleRoyaleFinalRadius, 64f, InitialRadius);
        CurrentCenter = InitialCenter;
        CurrentRadius = InitialRadius;
        StartDelaySeconds = Mathf.Max(0f, settings.BattleRoyaleStartDelaySeconds);
        ShrinkSpeed = Mathf.Max(1f, settings.BattleRoyaleShrinkSpeed);
        TrueDamagePerSecond = Mathf.Max(0f, settings.BattleRoyaleTrueDamagePerSecond);
        ShrinkDurationSeconds = Mathf.Max(0.1f, (InitialRadius - FinalRadius) / ShrinkSpeed);
        IsShrinking = false;
        IsRunning = true;
    }

    public void ServerStop()
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        IsRunning = false;
        IsShrinking = false;
        _roundElapsed = 0;
        _damageElapsed = 0;
    }

    public override void _Process(double delta)
    {
        if (!Multiplayer.IsServer() || !IsRunning)
        {
            return;
        }

        _roundElapsed += delta;
        if (_roundElapsed < StartDelaySeconds)
        {
            IsShrinking = false;
            CurrentCenter = InitialCenter;
            CurrentRadius = InitialRadius;
            return;
        }

        IsShrinking = true;
        var shrinkElapsed = (float)(_roundElapsed - StartDelaySeconds);
        var progress = Mathf.Clamp(shrinkElapsed / ShrinkDurationSeconds, 0f, 1f);
        CurrentCenter = InitialCenter.Lerp(TargetCenter, progress);
        CurrentRadius = Mathf.Lerp(InitialRadius, FinalRadius, progress);
        ApplyOutsideDamage(delta);
    }

    public override void _Draw()
    {
        if (!IsRunning || CurrentRadius <= 0f)
        {
            return;
        }

        var mapRect = new Rect2(ToLocal(MapPosition), MapSize);
        DrawRect(mapRect, new Color(1f, 0.08f, 0.05f, 0.2f), false, 3f);

        var currentColor = IsShrinking
            ? new Color(1f, 0.08f, 0.05f, 0.92f)
            : new Color(1f, 0.65f, 0.05f, 0.75f);
        DrawCircleOutline(ToLocal(CurrentCenter), CurrentRadius, currentColor, 5f);
        DrawCircleOutline(ToLocal(TargetCenter), FinalRadius, new Color(1f, 0.08f, 0.05f, 0.45f), 2f);
        DrawCircle(ToLocal(TargetCenter), 6f, new Color(1f, 0.08f, 0.05f, 0.85f));
    }

    public bool TryGetComponent<T>(out T component) where T : IComponent
    {
        if (_components.TryGetValue(typeof(T), out var value))
        {
            component = (T)value;
            return true;
        }

        component = default;
        return false;
    }

    public void AddComponent(IComponent component)
    {
        component.SetParent(this);
        _components[component.GetType()] = component;
        if (component is Node node)
        {
            AddChild(node);
        }
    }

    public void RemoveComponent(Type type)
    {
        if (!_components.Remove(type, out var component) || component is not Node node)
        {
            return;
        }

        RemoveChild(node);
    }

    private void ApplyOutsideDamage(double delta)
    {
        _damageElapsed += delta;
        if (_damageElapsed < DamageTickInterval || TrueDamagePerSecond <= 0f)
        {
            return;
        }

        var damageAmount = TrueDamagePerSecond * (float)_damageElapsed;
        _damageElapsed = 0;

        foreach (var player in _gameManager.GetPlayers())
        {
            if (!player.IsTargetable || player.GlobalPosition.DistanceTo(CurrentCenter) <= CurrentRadius)
            {
                continue;
            }

            if (player.TryGetComponent(out HealthComponent health))
            {
                health.ApplyTrueDamage(damageAmount, this);
            }
        }
    }

    private Vector2 PickTargetCenter(Rect2 mapBounds, float margin)
    {
        var usableMargin = Mathf.Max(0f, Mathf.Min(margin, Mathf.Min(mapBounds.Size.X, mapBounds.Size.Y) * 0.45f));
        var minX = mapBounds.Position.X + usableMargin;
        var maxX = mapBounds.Position.X + mapBounds.Size.X - usableMargin;
        var minY = mapBounds.Position.Y + usableMargin;
        var maxY = mapBounds.Position.Y + mapBounds.Size.Y - usableMargin;

        if (maxX < minX || maxY < minY)
        {
            return mapBounds.Position + mapBounds.Size / 2f;
        }

        return new Vector2(
            _random.RandfRange(minX, maxX),
            _random.RandfRange(minY, maxY));
    }

    private static float GetCoveringRadius(Vector2 center, Rect2 bounds)
    {
        var maxX = bounds.Position.X + bounds.Size.X;
        var maxY = bounds.Position.Y + bounds.Size.Y;
        var corners = new[]
        {
            bounds.Position,
            new Vector2(maxX, bounds.Position.Y),
            new Vector2(bounds.Position.X, maxY),
            new Vector2(maxX, maxY),
        };

        var radius = 0f;
        foreach (var corner in corners)
        {
            radius = Mathf.Max(radius, center.DistanceTo(corner));
        }

        return radius;
    }

    private void DrawCircleOutline(Vector2 center, float radius, Color color, float width)
    {
        var points = new Vector2[CircleSegments + 1];
        for (var i = 0; i <= CircleSegments; i++)
        {
            var angle = Mathf.Tau * i / CircleSegments;
            points[i] = center + Vector2.FromAngle(angle) * radius;
        }

        DrawPolyline(points, color, width, true);
    }
}
