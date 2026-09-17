using System;
using CardBase.Scripts;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public enum SpawnType
{
    ABILITY_VOLUME,
    AURA,
    RING_TEXTURE_NODE,
    VISUAL_CONNECTION,
    MINION,
}

public interface IAbilityVolumeModifier
{
    void Modify(AbilityVolumeStats stats);
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
    private readonly System.Collections.Generic.List<IAbilityVolumeModifier> volumeModifiers = new();
    private readonly System.Collections.Generic.HashSet<string> destroyedSpawnNames = new();
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
        if (destroyedSpawnNames.Remove(name))
        {
            return null;
        }

        switch ((SpawnType)(int)type)
        {
            case SpawnType.ABILITY_VOLUME:
                var volumeStats = AbilityVolumeStats.FromDict(spawnData, GameManager);
                return SpawnAbilityVolume(volumeStats, name);
            case SpawnType.AURA:
                break;
            case SpawnType.RING_TEXTURE_NODE:
                var spriteStats = SpriteStats.FromDict(spawnData, GameManager);
                return SpawnRingTextureNode(spriteStats, name);
            case SpawnType.VISUAL_CONNECTION:
                var connectionStats = VisualConnectionStats.FromDict(spawnData, GameManager);
                return SpawnVisualConnection(connectionStats, name);
            case SpawnType.MINION:
                var minionStats = MinionSpawnStats.FromDict(spawnData, GameManager);
                return SpawnMinion(minionStats, name);
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

    public void AddVolumeModifier(IAbilityVolumeModifier modifier)
    {
        if (modifier != null && !volumeModifiers.Contains(modifier))
        {
            volumeModifiers.Add(modifier);
        }
    }

    public void RemoveVolumeModifier(IAbilityVolumeModifier modifier)
    {
        volumeModifiers.Remove(modifier);
    }

    private void ApplyVolumeModifiers(AbilityVolumeStats stats)
    {
        foreach (var modifier in volumeModifiers)
        {
            modifier.Modify(stats);
        }
    }

    public AbilityVolume SpawnAbilityVolume(AbilityVolumeStats stats)
    {
        if (!Multiplayer.IsServer()) return null;

        stats.NormalizeForSpawn();
        ApplyVolumeModifiers(stats);
        stats.NormalizeForSpawn();
        var node = Spawn(new SpawnData
        {
            SpawnType = SpawnType.ABILITY_VOLUME,
            SpawnObjectData = stats.ToDict(),
        });
        if (node is AbilityVolume volume)
        {
            volume.SetCallbacks(stats.Callbacks);
            return volume;
        }

        return null;
    }

    private AbilityVolume SpawnAbilityVolume(AbilityVolumeStats stats, string name)
    {
        var volume = InstantiateAbilityVolume(stats.Visual?.ScenePath);
        volume.Initialize(stats);
        volume.Name = name;
        return volume;
    }

    public Projectile SpawnProjectile(ProjectileSpawnRequest spawnRequest, ProjectileRuntime runtime = null)
    {
        if (!HasClearProjectileSpawnPath(spawnRequest))
        {
            return null;
        }

        var stats = AbilityVolumeStats.FromProjectile(spawnRequest);
        if (spawnRequest.Caller != null && spawnRequest.Caller.TryGetComponent(out StatblockComponent statBlock))
        {
            stats.Pull.Radius += statBlock.GetStat(StatType.AddPullRadius);
            stats.Pull.Strength += statBlock.GetStat(StatType.AddPullStrength);
        }

        var volume = SpawnAbilityVolume(stats);
        if (volume is not Projectile projectile)
        {
            return null;
        }

        projectile.ConfigureProjectile(spawnRequest, runtime);
        return projectile;
    }

    private bool HasClearProjectileSpawnPath(ProjectileSpawnRequest spawnRequest)
    {
        if (spawnRequest?.Caller == null)
        {
            return true;
        }

        var from = GetProjectileCasterPosition(spawnRequest);
        var to = spawnRequest.StartPosition;
        if (from.IsEqualApprox(to))
        {
            return true;
        }

        var query = new PhysicsRayQueryParameters2D
        {
            From = from,
            To = to,
            CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.Wall,
            CollideWithAreas = false,
            CollideWithBodies = true,
        };

        return GetWorld2D().DirectSpaceState.IntersectRay(query).Count == 0;
    }

    private static Vector2 GetProjectileCasterPosition(ProjectileSpawnRequest spawnRequest)
    {
        if (spawnRequest.Caller.TryGetComponent(out AimComponent aimComponent))
        {
            return aimComponent.GetCharacterCenterPosition();
        }

        return spawnRequest.Caller is Node2D callerNode
            ? callerNode.GlobalPosition
            : spawnRequest.StartPosition;
    }

    public void DestroySpawnedNode(string nodeName)
    {
        if (string.IsNullOrEmpty(nodeName))
        {
            return;
        }

        DestroySpawnedNodeLocally(nodeName);
    }

    private void DestroySpawnedNodeLocally(string nodeName)
    {
        if (TryGetSpawnedNode(nodeName, out var node))
        {
            if (!node.IsQueuedForDeletion())
            {
                node.QueueFree();
            }

            return;
        }

        destroyedSpawnNames.Add(nodeName);
    }

    private bool TryGetSpawnedNode(string nodeName, out Node node)
    {
        node = GetNodeOrNull<Node>(nodeName)
               ?? GetNodeOrNull<Node>($"AbilitySpawns/{nodeName}");
        return node != null;
    }

    private AbilityVolume InstantiateAbilityVolume(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return new AbilityVolume();
        }

        if (!LoadedScenes.TryGetValue(path, out var loadedScene))
        {
            loadedScene = GD.Load<PackedScene>(path);
            LoadedScenes.Add(path, loadedScene);
        }

        return (AbilityVolume)loadedScene.Instantiate();
    }

    private RingTextureNode SpawnRingTextureNode(SpriteStats stats, string name)
    {
        var textureNode = new RingTextureNode();
        var texture = IconLoader.Instance.LoadImage(stats.TexturePath);
        textureNode.Name = name;
        textureNode.Init(texture, stats.Scale);

        return textureNode;
    }

    public VisualConnection SpawnVisualConnection(VisualConnectionStats stats)
    {
        if (!Multiplayer.IsServer()) return null;

        var node = Spawn(new SpawnData
        {
            SpawnType = SpawnType.VISUAL_CONNECTION,
            SpawnObjectData = stats.ToDict(),
        });
        return node as VisualConnection;
    }

    private VisualConnection SpawnVisualConnection(VisualConnectionStats stats, string name)
    {
        var connection = new VisualConnection();
        connection.Initialize(stats);
        connection.Name = name;
        return connection;
    }

    public Minion SpawnMinion(MinionSpawnStats stats)
    {
        if (!Multiplayer.IsServer()) return null;

        var node = Spawn(new SpawnData
        {
            SpawnType = SpawnType.MINION,
            SpawnObjectData = stats.ToDict(),
        });
        return node as Minion;
    }

    private Minion SpawnMinion(MinionSpawnStats stats, string name)
    {
        var minion = CreateMinion(stats.Kind);
        minion.Initialize(stats, this, GameManager);
        minion.Name = name;
        return minion;
    }

    private static Minion CreateMinion(MinionKind kind)
    {
        return kind switch
        {
            MinionKind.Afterimage => new AfterimageMinion(),
            MinionKind.StationaryTower => new StationaryTowerMinion(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown minion kind"),
        };
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
