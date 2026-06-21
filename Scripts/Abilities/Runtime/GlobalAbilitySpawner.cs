using System;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public enum SpawnType
{
    RAY,
    AOE,
    AURA,
    PROJECTILE,
    RING_TEXTURE_NODE,
}

public class SpawnData
{
    public SpawnType SpawnType;
    public string Name;
    public Dictionary<string, Variant> SpawnObjectData;

    public Dictionary<string, Variant> ToDict()
    {
        return new Dictionary<string, Variant>()
        {
            { nameof(SpawnType), (int)SpawnType },
            { nameof(Name), Name },
            { nameof(SpawnObjectData), SpawnObjectData },
        };
    }
}

[GlobalClass]
public partial class GlobalAbilitySpawner : Node2D
{

    [Export] private MultiplayerSpawner abilitySpawner;
    [Export] private GameManager GameManager;

    private System.Collections.Generic.Dictionary<string, PackedScene> LoadedScenes = new();
    public override void _EnterTree()
    {
        abilitySpawner.SetSpawnFunction(new Callable(this, MethodName.customSpawn));
        SetMultiplayerAuthority(1);
    }

    private Node customSpawn(Variant data)
    {
        var dict = data.AsGodotDictionary<string, Variant>();
        if (!dict.TryGetValue(nameof(SpawnData.SpawnType), out var typeVariant)
            || !dict.TryGetValue(nameof(SpawnData.SpawnObjectData), out var spawnDataVariant)
            || !dict.TryGetValue(nameof(SpawnData.Name), out var nameVariant))
        {
            return null;
        }

        var type = (SpawnType)(int)typeVariant;
        var spawnData = spawnDataVariant.AsGodotDictionary<string, Variant>();
        var name = (string)nameVariant;

        switch ((SpawnType)(int)type)
        {
            case SpawnType.RAY:
                var rayStats = RayStats.FromDict(spawnData, GameManager);
                return SpawnRay(rayStats, name);

            case SpawnType.AOE:
                var aoeStats = AoeBaseStats.FromDict(spawnData, GameManager);
                return spawnAoe(aoeStats, name);
            case SpawnType.AURA:
                break;
            case SpawnType.PROJECTILE:
                break;
            case SpawnType.RING_TEXTURE_NODE:
                var spriteStats = SpriteStats.FromDict(spawnData, GameManager);
                return SpawnRingTextureNode(spriteStats, name);
            default:
                throw new ArgumentOutOfRangeException();
        }

        return null;
    }

    public Node Spawn(SpawnData spawnData)
    {
        if (!Multiplayer.IsServer())
        {
            return null;
        }

        spawnData.Name = GenerateSpawnName(spawnData.SpawnType);
        return abilitySpawner.Spawn(spawnData.ToDict());
    }

    public override void _Ready()
    {
        SetMultiplayerAuthority(1);
    }

    public Ray SpawnRay(RayStats props, string name)
    {
        var ray = new Ray(props);
        ray.Name = name;
        return ray;
    }

    public AoeBase SpawnAoe(AoeBaseStats aoeStats)
    {
        if (!Multiplayer.IsServer()) return null;

        var spawnData = new SpawnData()
        {
            Name = GlobalAbilitySpawner.GenerateSpawnName(SpawnType.AOE),
            SpawnType = SpawnType.AOE,
            SpawnObjectData = aoeStats.ToDict()
        };
        var node = Spawn(spawnData);
        if (node is AoeBase aoe)
        {
            aoe.SetCallbacks(aoeStats.Callbacks);
            return aoe;
        }

        return null;
    }

    private AoeBase spawnAoe(AoeBaseStats aoeStats, string name)
    {
        var aoe = new AoeBase();
        aoe.Initialize(aoeStats);
        aoe.Name = name;
        return aoe;
    }

    public Projectile SpawnProjectile(ProjectileSpawnRequest spawnRequest, ProjectileRuntime runtime = null)
    {
        var projectile = instantiateProjectile(spawnRequest.Visual.ScenePath);
        var name = GenerateSpawnName(SpawnType.PROJECTILE);
        projectile.Name = name;

        projectile.Initialize(spawnRequest, runtime);

        var dict = spawnRequest.ToDict();
        AddChild(projectile);

        Rpc(MethodName.spawnProjectileOnClient, dict, name);
        return projectile;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void spawnProjectileOnClient(Variant dict, string name)
    {
        var spawnRequest = ProjectileSpawnRequest.FromDict((Godot.Collections.Dictionary<string, Variant>)dict, GameManager);
        var projectile = instantiateProjectile(spawnRequest.Visual.ScenePath);
        projectile.Name = name;
        projectile.Initialize(spawnRequest);
        AddChild(projectile);
    }

    private Projectile instantiateProjectile(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            path = "res://Scenes/Projectiles/Projectile.tscn";
        }

        if (!LoadedScenes.TryGetValue(path, out var loadedScene))
        {
            loadedScene = GD.Load<PackedScene>(path);
            LoadedScenes.Add(path, loadedScene);
        }

        var projectile = loadedScene.Instantiate();
        return (Projectile)projectile;
    }

    private RingTextureNode SpawnRingTextureNode(SpriteStats stats, string name)
    {
        var textureNode = new RingTextureNode();
        var texture = IconLoader.Instance.LoadImage(stats.TexturePath);
        textureNode.Name = name;
        textureNode.Init(texture, stats.Scale);

        return textureNode;
    }

    public static string GenerateSpawnName(SpawnType type)
    {
        return $"{type}_{Guid.NewGuid()}";
    }



}

public class SpriteStats
{
    public string TexturePath;
    public Vector2 Scale;
    public Node2D Parent;

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        var dict = new Godot.Collections.Dictionary<string, Variant>()
        {
            { nameof(TexturePath), TexturePath },
            { nameof(Scale), Scale },
            { nameof(Parent), Parent?.GetPath() ?? string.Empty },
            { "ParentPlayerId", Parent is PlayerCharacter parentPlayer ? parentPlayer.PlayerId : 0 },
        };
        return dict;
    }

    public static SpriteStats FromDict(Godot.Collections.Dictionary<string, Variant> dict, GameManager manager)
    {
        Node2D parent = null;
        if (dict.TryGetValue("ParentPlayerId", out var parentPlayerIdVariant)
            && (long)parentPlayerIdVariant != 0
            && manager.GetPlayerCharacter((long)parentPlayerIdVariant) is { } parentPlayer)
        {
            parent = parentPlayer;
        }
        else
        {
            var parentPath = (string)dict[nameof(Parent)];
            parent = string.IsNullOrEmpty(parentPath) ? null : manager.GetNodeOrNull<Node2D>(parentPath);
        }

        var stats = new SpriteStats
        {
            TexturePath = (string)dict[nameof(TexturePath)],
            Scale = (Vector2)dict[nameof(Scale)],
            Parent = parent
        };
        return stats;
    }
}
