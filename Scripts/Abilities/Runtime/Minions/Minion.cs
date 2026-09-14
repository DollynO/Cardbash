using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public class MinionSpawnStats
{
    public IEntityComponent Owner { get; set; }
    public MinionKind Kind { get; set; } = MinionKind.Afterimage;
    public string AbilityGUID { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Scale { get; set; } = Vector2.One;
    public Vector2 AnimationOffset { get; set; } = Vector2.Zero;
    public string AnimationPath { get; set; } = "res://AnimationRes/PlayerAnimation/PlayerCharacterAnimation.tres";
    public float Duration { get; set; } = 0.6f;
    public float Life { get; set; } = 20f;
    public float Alpha { get; set; } = 0.45f;
    public float CollisionRadius { get; set; } = 12f;
    public int TeamId { get; set; } = -1;
    public Dictionary<string, Variant> BehaviorData { get; set; } = new();

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            { nameof(Kind), (int)Kind },
            { nameof(AbilityGUID), AbilityGUID ?? string.Empty },
            { nameof(Position), Position },
            { nameof(Scale), Scale },
            { nameof(AnimationOffset), AnimationOffset },
            { nameof(AnimationPath), AnimationPath ?? string.Empty },
            { nameof(Duration), Duration },
            { nameof(Life), Life },
            { nameof(Alpha), Alpha },
            { nameof(CollisionRadius), CollisionRadius },
            { nameof(TeamId), TeamId },
            { nameof(BehaviorData), BehaviorData },
            { "OwnerPath", Owner is Node2D ownerNode ? ownerNode.GetPath() : string.Empty },
            { "OwnerPlayerId", Owner is PlayerCharacter ownerPlayer ? ownerPlayer.PlayerId : 0 },
        };
    }

    public static MinionSpawnStats FromDict(Godot.Collections.Dictionary<string, Variant> dict, GameManager gameManager)
    {
        return new MinionSpawnStats
        {
            Owner = ResolveOwner(dict, gameManager),
            Kind = (MinionKind)(int)dict[nameof(Kind)],
            AbilityGUID = (string)dict[nameof(AbilityGUID)],
            Position = (Vector2)dict[nameof(Position)],
            Scale = (Vector2)dict[nameof(Scale)],
            AnimationOffset = (Vector2)dict[nameof(AnimationOffset)],
            AnimationPath = (string)dict[nameof(AnimationPath)],
            Duration = (float)dict[nameof(Duration)],
            Life = (float)dict[nameof(Life)],
            Alpha = (float)dict[nameof(Alpha)],
            CollisionRadius = (float)dict[nameof(CollisionRadius)],
            TeamId = (int)dict[nameof(TeamId)],
            BehaviorData = dict.TryGetValue(nameof(BehaviorData), out var behaviorDataVariant)
                ? behaviorDataVariant.AsGodotDictionary<string, Variant>()
                : new Dictionary<string, Variant>(),
        };
    }

    private static IEntityComponent ResolveOwner(Godot.Collections.Dictionary<string, Variant> dict, GameManager gameManager)
    {
        if (gameManager == null)
        {
            return null;
        }

        if (dict.TryGetValue("OwnerPlayerId", out var ownerPlayerIdVariant)
            && (long)ownerPlayerIdVariant != 0
            && gameManager.GetPlayerCharacter((long)ownerPlayerIdVariant) is { } ownerPlayer)
        {
            return ownerPlayer;
        }

        var ownerPath = dict.TryGetValue("OwnerPath", out var ownerPathVariant)
            ? (string)ownerPathVariant
            : string.Empty;

        return string.IsNullOrEmpty(ownerPath)
            ? null
            : gameManager.GetNodeOrNull<Node>(ownerPath) as IEntityComponent;
    }
}

public partial class Minion : CharacterbodyEntityComponent, IMinionEntity
{
    protected MinionSpawnStats Stats { get; private set; }
    protected HealthComponent HealthComponent { get; private set; }
    protected GlobalAbilitySpawner Spawner { get; private set; }
    protected GameManager GameManager { get; private set; }

    protected virtual MinionKind DefaultKind => MinionKind.Afterimage;
    protected virtual bool FadeOutOverLifetime => false;

    public IEntityComponent MinionOwner => Stats?.Owner;
    public MinionKind MinionKind => Stats?.Kind ?? DefaultKind;
    public int TeamId => MinionOwner is ITeamAffiliation ownerTeam ? ownerTeam.TeamId : Stats?.TeamId ?? -1;
    public bool IsTargetable => HealthComponent is { IsDead: false } && !IsQueuedForDeletion();

    public void Initialize(MinionSpawnStats minionStats, GlobalAbilitySpawner spawner = null, GameManager gameManager = null)
    {
        Stats = minionStats;
        Spawner = spawner;
        GameManager = gameManager;
    }

    public void Destroy()
    {
        if (!IsQueuedForDeletion())
        {
            QueueFree();
        }
    }

    public override void _Ready()
    {
        if (Stats == null)
        {
            QueueFree();
            return;
        }

        SetMultiplayerAuthority(1);
        ZAsRelative = false;
        ZIndex = 4;
        GlobalPosition = Stats.Position;
        Scale = Stats.Scale;
        CollisionLayer = CombatCollisionLayers.Minion;
        CollisionMask = 0;
        AddToGroup("Minion");

        AddVisual();
        AddCollision();
        AddCombatComponents();
        AddBehaviorComponents();
        StartLifetime();
        StartFade();
    }

    public override void _PhysicsProcess(double delta)
    {
        ProcessMinion(delta);
    }

    public override void _ExitTree()
    {
        EventBus.CombatEventBus.KilledEventHandler -= OnEntityKilled;
    }

    protected virtual void AddBehaviorComponents()
    {
    }

    protected virtual void ProcessMinion(double delta)
    {
    }

    protected virtual void ConfigureVisual(VisualComponent visual)
    {
        visual.SetAnimation(Stats.AnimationPath, Stats.AnimationOffset);
        visual.SetShader(CreateTeamShader());
    }

    private void AddVisual()
    {
        var visual = new VisualComponent
        {
            Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(Stats.Alpha, 0f, 1f)),
        };
        ConfigureVisual(visual);
        AddComponent(visual);
    }

    private ShaderMaterial CreateTeamShader()
    {
        var shader = GD.Load<Shader>("res://Shaders/PlayerCharacter_TeamColor.gdshader");
        var shaderMaterial = new ShaderMaterial
        {
            Shader = shader,
        };
        shaderMaterial.SetShaderParameter("mask_color", new Vector4(0.341f, 0.227f, 0.196f, 1));
        shaderMaterial.SetShaderParameter("mask_color_2", new Vector4(0.251f, 0.153f, 0.09f, 1));
        shaderMaterial.SetShaderParameter("tolerance", 0.1f);
        shaderMaterial.SetShaderParameter("team_color", ColorPlate.GetColor(TeamId));
        return shaderMaterial;
    }

    private void AddCollision()
    {
        AddChild(new CollisionShape2D
        {
            Shape = new CircleShape2D
            {
                Radius = Mathf.Max(Stats.CollisionRadius, 1f),
            },
        });
    }

    private void AddCombatComponents()
    {
        var life = Mathf.Max(Stats.Life, 1f);
        var statBlock = new StatblockComponent
        {
            ReplicateStats = false,
        };
        AddComponent(statBlock);
        statBlock.Define(StatType.Life, life, 1f, float.PositiveInfinity);
        statBlock.Define(StatType.Armor, 0, 0, float.PositiveInfinity);
        statBlock.Define(StatType.EnergyShield, 0, float.NegativeInfinity, float.PositiveInfinity);
        statBlock.Define(StatType.MovementSpeed, 0, 0, float.PositiveInfinity);

        HealthComponent = new HealthComponent();
        AddComponent(HealthComponent);
        HealthComponent.Reset(life);

        AddComponent(new DamageAbleComponent());
        AddComponent(new BuffManagerComponent());

        if (Multiplayer.IsServer())
        {
            EventBus.CombatEventBus.KilledEventHandler += OnEntityKilled;
        }
    }

    private void StartLifetime()
    {
        if (!Multiplayer.IsServer() || Stats.Duration <= 0)
        {
            return;
        }

        var lifetimeTimer = new Timer
        {
            OneShot = true,
        };
        AddChild(lifetimeTimer);
        lifetimeTimer.Timeout += Destroy;
        lifetimeTimer.Start(Stats.Duration);
    }

    private void StartFade()
    {
        if (!FadeOutOverLifetime || Stats.Duration <= 0)
        {
            return;
        }

        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0f, Stats.Duration);
    }

    private void OnEntityKilled(object sender, KilledEventArgs args)
    {
        if (args.Target == this)
        {
            Destroy();
        }
    }
}
