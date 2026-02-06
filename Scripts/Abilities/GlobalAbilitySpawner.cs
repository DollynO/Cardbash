using System;
using System.Collections.Generic;
using CardBase.Scripts.GameSettings;
using Godot;

namespace CardBase.Scripts.Abilities;

[GlobalClass]
public partial class GlobalAbilitySpawner : Node2D
{
    private Dictionary<string, PackedScene> LoadedScenes = new Dictionary<string, PackedScene>();
    public override void _Ready()
    {
        SetMultiplayerAuthority(1);
    }

    [Export] private GameManager GameManager;
    public Ray SpawnRay(RayStats props)
    {
        var ray = new Ray(props);
        var name = generateName(SpawnType.RAY);
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
        var aoe = new AoeBase();
        aoe.Initialize(aoeStats);
        var name = generateName(SpawnType.AOE);
        aoe.Name = name;
        var dict = aoeStats.ToDict();
        this.AddChild(aoe);
        Rpc(MethodName.spawnAoeOnClient, dict, name);
        return aoe;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false,  TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void spawnAoeOnClient(Variant dict, string name)
    {
        var stats = AoeBaseStats.FromDict((Godot.Collections.Dictionary<string, Variant>)dict, GameManager);
        var aoe = new AoeBase();
        aoe.Initialize(stats);
        aoe.Name = name;
        this.AddChild(aoe);
    }

    public Projectile SpawnProjectile(ProjectileStats projectileStats)
    {
        var projectile = instantiateProjectile(projectileStats.CustomProjectilePath);
        var name = generateName(SpawnType.PROJECTILE);
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

    public RingTextureNode SpawnSprite(SpriteStats stats)
    {
        var textureNode = new RingTextureNode();
        var texture = IconLoader.Instance.LoadImage(stats.TexturePath);
        var name =  generateName(SpawnType.RING_TEXTURE_NODE);
        textureNode.Init(texture, stats.Scale, name);

        Rpc(MethodName.spawnRingTextureClient, stats.ToDict(), name);
        
        return textureNode;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,  CallLocal = false,  TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void spawnRingTextureClient(Variant dict, string name)
    {
        var textureNode = new RingTextureNode();
        var stats = SpriteStats.FromDict((Godot.Collections.Dictionary<string, Variant>)dict, GameManager);
        var texture = IconLoader.Instance.LoadImage(stats.TexturePath);
        textureNode.Init(texture, stats.Scale, name);
        if (stats.Parent != null)
        {
            stats.Parent.AddChild(textureNode);
        }
        else
        {
            this.AddChild(textureNode);
        }
        
    }
    
    private string generateName(SpawnType type)
    {
        return $"{type}_{Guid.NewGuid()}";
    }
    
    
    private enum SpawnType
    {
        RAY,
        AOE,
        MELEE,
        AURA,
        PROJECTILE,
        RING_TEXTURE_NODE,
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