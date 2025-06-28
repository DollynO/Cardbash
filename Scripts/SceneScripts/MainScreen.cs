using Godot;
using System;
using CardBase.Scripts.Cards;

public partial class MainScreen : ColorRect
{
	[Export] private Panel _mpScreen;

	[Export] private LineEdit _username;

	[Export] private LineEdit _ip_address;
	[Export] private LineEdit _port;

	private NetworkManager _network;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_network = GetNode<NetworkManager>(NetworkManager.GetNetworkManagerPath());
		GlobalCardManager.Instance.Load();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void _on_deck_builder_pressed()
	{
		var scene_manager = GetNode("..") as SceneManager;
		scene_manager?.LoadDeckBuilderScene();
	}
	
	private void _on_multiplayer_pressed()
	{
		_port.Text = "8080";
		_username.Text = string.Empty;
		_mpScreen.Visible = true;
	}
	
	private void _on_exit_pressed()
	{
		GetTree().Quit();
	}

	private void _on_cancel_mp_pressed()
	{
		_port.Text = string.Empty;
		_username.Text = string.Empty;
		_mpScreen.Visible = false;
	}

	private void _on_host_pressed()
	{
		_network.LocalUsername = _username.Text;
		_network.StartHost(int.Parse(_port.Text));
		var scene_manager = GetNode("..") as SceneManager;
		scene_manager?.LoadLobbyScene();
	}

	private void _on_join_pressed()
	{
		_network.LocalUsername = _username.Text;
		_network.StartClient( _ip_address.Text, int.Parse(_port.Text));
		var scene_manager = GetNode("..") as SceneManager;
		scene_manager?.LoadLobbyScene();
	}
}
