using Godot;
using System;
using System.Runtime.CompilerServices;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;
using Godot.Collections;

public enum SpawnType{
	SpawnTypeProjectile,
}

public partial class AbilitySpawner : MultiplayerSpawner
{
	[Export]
	PackedScene projectileScene;
	
	[Export]
	GameManager gameManager;
	public override void _EnterTree()
	{ 
		SpawnFunction = new Callable(this, MethodName.customSpawner);
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SpawnObject(Dictionary<string, Variant> dict)
	{
		Rpc(MethodName.spawnOnHost, dict);
	}
	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void spawnOnHost(Dictionary<string, Variant> dict)
	{
		if (Multiplayer.IsServer())
		{
			Spawn(dict);
		}
	}
	
	private Node customSpawner(Variant data)
	{
		var dic = data.AsGodotDictionary<string, Variant>();
		switch ((SpawnType)(int)dic["spawn_type"])
		{
			case SpawnType.SpawnTypeProjectile:
				return spawnProjectile(dic);
		}

		return null;
	}

	private Node spawnProjectile(Dictionary<string, Variant> data)
	{
		if (projectileScene.Instantiate() is Projectile projectile)
		{
			var speed = (float)data["speed"];
			speed = speed > Projectile.MAX_SPEED ? Projectile.MAX_SPEED : speed;
			var projectileStats = new ProjectileStats()
			{
				Damage = (Dictionary<DamageType, float>)data["damage"],
				Direction = (Vector2)data["direction"],
				Speed = speed,
				SpritePath = (string)data["sprite_path"],
				TimeToBeALive = (float)data["time_to_be_a_live"],
				Caller = gameManager.GetPlayerCharacter((long)data["caller_id"]),
				PiercingCount = (int)data["piercing_count"],
				BouncingCount = (int)data["bouncing_count"],
				StartPosition = (Vector2)data["start_position"],
			};
			projectile.SetStats(projectileStats);
			projectile.GlobalPosition = projectileStats.StartPosition;
			return projectile;
		}
		
		return null;
	}
}
