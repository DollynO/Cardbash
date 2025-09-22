using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CardBase.Scripts;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;

public enum SpawnType{
	Undefined,
	SpawnTypeProjectile,
}

public sealed class QueueObjectProperties
{
	public float Delay;
	public Godot.Collections.Dictionary<string, Variant> Dict;
}

public partial class AbilitySpawner : MultiplayerSpawner
{
	[Export]
	PackedScene projectileScene;
	
	[Export]
	GameManager gameManager;

	private List<QueueObjectProperties> queuedObjects = new();
	public override void _EnterTree()
	{ 
		SpawnFunction = new Callable(this, MethodName.customSpawner);
		Spawned += OnSpawned;
	}

	private void OnSpawned(Node node)
	{
		if (node is ICustomSpawnObject customSpawnObject)
		{
			gameManager.GetPlayerCharacter(customSpawnObject.CreatorId).RegisterAbilityObject(node);
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetProcess(Multiplayer.IsServer());
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var executeQueueObjects = new List<QueueObjectProperties>();
		foreach (var t in queuedObjects)
		{
			t.Delay -= (float)delta;
			if (t.Delay <= 0f)
			{
				executeQueueObjects.Add(t);
			}
		}

		foreach (var t in executeQueueObjects)
		{
			SpawnObject(t.Dict);
		}
		
		queuedObjects.RemoveAll(o => o.Delay <= 0f);
	}

	public void SpawnObject(Godot.Collections.Dictionary<string, Variant> dict)
	{
		Rpc(MethodName.spawnOnHost, dict);
	}
	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void spawnOnHost(Godot.Collections.Dictionary<string, Variant> dict)
	{
		if (Multiplayer.IsServer())
		{
			Spawn(dict);
		}
	}
	
	private Node customSpawner(Variant data)
	{
		var dic = data.AsGodotDictionary<string, Variant>();
		if (!dic.TryGetValue("spawn_properties", out var spawnPropertiesDict))
		{
			throw new ArgumentNullException("no spawn properties");
		}
		
		var spawnProperties = SpawnerBaseProperties.FromDict(spawnPropertiesDict.AsGodotDictionary<string, Variant>());
		var objectStat = dic["object_stats"].AsGodotDictionary<string, Variant>();
		var node = spawnProperties.SpawnType switch
		{
			SpawnType.SpawnTypeProjectile => spawnProjectile(objectStat),
			_ => null
		};

		if (node is ICustomSpawnObject customSpawnObject)
		{
			customSpawnObject.CreatorId = spawnProperties.CreatorId;
			customSpawnObject.AbilityGuid = spawnProperties.AbilityGuid;
		}

		if (Multiplayer.IsServer())
		{
			if (spawnProperties.SpawnCount > 0)
			{
				(dic["spawn_properties"].AsGodotDictionary<string, Variant>())[nameof(spawnProperties.SpawnCount)] = --spawnProperties.SpawnCount;
				queuedObjects.Add(new QueueObjectProperties(){Delay = spawnProperties.SpawnDelay, Dict = dic});
			}
		}

		return node;
	}

	private Node spawnProjectile(Godot.Collections.Dictionary<string, Variant> data)
	{
		Projectile projectile;
		if (data.TryGetValue("custom_projectile", out var customProjectile))
		{
			projectile = GD.Load<PackedScene>((string)customProjectile).Instantiate() as Projectile;
		}
		else
		{
			projectile = (Projectile)projectileScene.Instantiate();
		}
		
		
		if (projectile != null)
		{
			var projectileStats = ProjectileStats.FromDict(data, gameManager);
			projectileStats.Direction = ((PlayerCharacter)projectileStats.Caller).GetLookAtDirection();
			projectileStats.StartPosition = ((PlayerCharacter)projectileStats.Caller).GetProjectileStartPosition();
			projectile.SetStats(projectileStats);
			projectile.GlobalPosition = projectileStats.StartPosition;
			return projectile;
		}
		
		return null;
	}
}
