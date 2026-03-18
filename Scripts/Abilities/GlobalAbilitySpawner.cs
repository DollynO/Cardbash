using System;
using CardBase.Scripts.GameSettings;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public enum SpawnType
{
    RAY,
    AOE,
    MELEE,
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
    
    private System.Collections.Generic.Dictionary<string, PackedScene> LoadedScenes = new ();
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
                break;

            case SpawnType.AOE:
                var aoeStats = AoeBaseStats.FromDict(spawnData, GameManager);
                return spawnAoe(aoeStats, name);
                break;
            case SpawnType.MELEE:
                break;
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
        if (Multiplayer.IsServer())
        {
            return abilitySpawner.Spawn(spawnData.ToDict());
        }

        return null;
    }

    public override void _Ready()
    {
        SetMultiplayerAuthority(1);
    }

    public Ray SpawnRay(RayStats props)
    {
        var ray = new Ray(props);
        var name = GenerateSpawnName(SpawnType.RAY);
        ray.Name = name;
        if (props.Caster.TryGetComponent(out AimComponent aimComponent))
        {
            aimComponent.GetCharacterCenterPoint().AddChild(ray);
        }
        else
        {
            ((Node2D)props.Caster).AddChild(ray);
        }
        Rpc(MethodName.spawnRayOnClient, props.ToDict(), name);
        return ray;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void spawnRayOnClient(Variant dict, string name)
    {
        var props = RayStats.FromDict((Godot.Collections.Dictionary<string, Variant>)dict, GameManager);
        var ray = new Ray(props);
        ray.Name = name;
        if (props.Caster.TryGetComponent(out AimComponent aimComponent))
        {
            aimComponent.GetCharacterCenterPoint().AddChild(ray);
        }
        else
        {
            ((Node2D)props.Caster).AddChild(ray);
        }
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

    public Projectile SpawnProjectile(ProjectileStats projectileStats)
    {
        var projectile = instantiateProjectile(projectileStats.CustomProjectilePath);
        var name = GenerateSpawnName(SpawnType.PROJECTILE);
        projectile.Name = name;

        projectile.SetStats(projectileStats);
        
        var dict = projectileStats.ToDict();
        if (projectileStats.Parent == null)
        {
            this.AddChild(projectile);
        }
        
        Rpc(MethodName.spawnProjectileOnClient, dict, name);
        return projectile;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false,  TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void spawnProjectileOnClient(Variant dict, string name)
    {
        var projectileStats = ProjectileStats.FromDict((Godot.Collections.Dictionary<string, Variant>)dict, GameManager);
        var projectile = instantiateProjectile(projectileStats.CustomProjectilePath);
        projectile.Name = name;
        projectile.SetStats(projectileStats);
        if (projectileStats.Parent == null)
        {
            this.AddChild(projectile);
        }
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
            { nameof(Parent), Parent.GetPath() },
        };
        return dict;
    }

    public static SpriteStats FromDict(Godot.Collections.Dictionary<string, Variant> dict, GameManager manager)
    {
        var parentPath = (string)dict[nameof(Parent)];
        var stats = new SpriteStats
        {
            TexturePath = (string)dict[nameof(TexturePath)],
            Scale = (Vector2)dict[nameof(Scale)],
            Parent = string.IsNullOrEmpty(parentPath) ? null : manager.GetNode(parentPath) as Node2D
        };
        return stats;
    }
}