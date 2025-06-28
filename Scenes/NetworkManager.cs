using Godot;
using System;
using System.Linq;
using CardBase.Scripts;
using Godot.Collections;
using Array = Godot.Collections.Array;

public partial class NetworkManager : Node
{
	public Array<Player> CurrentPlayers = new ();
	public string LocalUsername { get; set; }

	[Export] private PackedScene player_scene;
	[Export] private MultiplayerSpawner multiplayer_spawner;

	[Signal]
	public delegate void OnConnectedToServerEventHandler();
	
	[Signal]
	public delegate void OnServerDisconnectedEventHandler();
	
	[Signal]
	public delegate void OnPlayerJoinedEventHandler(long id);

	public static string GetNetworkManagerPath()
	{
		return "/root/Main/NetworkManager";
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void StartHost(int port)
	{
		CurrentPlayers.Clear();
		clearMultiplayerSpawner();
		var peer = new ENetMultiplayerPeer();
		peer.CreateServer(port);
		Multiplayer.MultiplayerPeer = peer;
		
		Multiplayer.PeerConnected += _on_player_connected;
		Multiplayer.PeerDisconnected += _on_player_disconnected;
		
		_on_player_connected(Multiplayer.GetUniqueId());
		_connected_to_server();
	}

	public void StartClient(string ip, int port)
	{
		//clearMultiplayerSpawner();
		CurrentPlayers.Clear();
		var peer = new ENetMultiplayerPeer();
		peer.CreateClient(ip, port);
		Multiplayer.MultiplayerPeer = peer;

		Multiplayer.ConnectedToServer += _connected_to_server;
		Multiplayer.ConnectionFailed += _connection_failed;
		Multiplayer.ServerDisconnected += _server_disconnected;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void DisconnectPlayer(int playerId)
	{
		Multiplayer.MultiplayerPeer.DisconnectPeer(playerId);
	}

	private void _on_player_connected(long id)
	{
		var player = player_scene.Instantiate();
		player.Name = id.ToString();
		multiplayer_spawner.AddChild(player, true);
		EmitSignal(SignalName.OnPlayerJoined, id);
	}

	private void _on_player_disconnected(long id)
	{
		var node = multiplayer_spawner.GetNode(id.ToString()) as Player;
		if (CurrentPlayers.Any(x => x == node))
		{
			RemovePlayerFromList(node);
		}

		node?.QueueFree();
	}

	private void _connected_to_server()
	{
		EmitSignal(SignalName.OnConnectedToServer);
	}

	private void _connection_failed()
	{
		
	}

	private void _server_disconnected()
	{
		EmitSignal(SignalName.OnServerDisconnected);
	}

	public void AddPlayerToList(Player player)
	{
		player.TeamNumber = CurrentPlayers.Count;
		CurrentPlayers.Add(player);
	}

	public void RemovePlayerFromList(Player player)
	{
		CurrentPlayers.Remove(player);
	}

	private void clearMultiplayerSpawner()
	{
		foreach (var child in multiplayer_spawner.GetChildren())
		{
			multiplayer_spawner.RemoveChild(child);
			child.QueueFree();
		}
	}
}
