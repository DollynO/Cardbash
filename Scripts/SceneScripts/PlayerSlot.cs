using Godot;
using System;
using CardBase.Scripts;
using Array = Godot.Collections.Array;

public partial class PlayerSlot : Panel
{
	[Export] private Label _nameText;

	[Export] private Button _kickButton;
	[Export] private TextureRect _notReady;
	[Export] private TextureRect _ready;
	[Export] private ColorRect _teamColor;

	private Player _currentPlayer;
	private Color _currentColor = new ("5DBB63");
	private Color _defaultColor = new ("FFFFFF");
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void UpdateSlotUi(Player player)
	{
		_currentPlayer = player;
		_nameText.Text = player.Username;
		_nameText.Set("theme_override_colors/font_color",player.IsMultiplayerAuthority() ? _currentColor : _defaultColor);
		_kickButton.Visible = Multiplayer.IsServer() && !player.IsMultiplayerAuthority();
		_ready.Visible = player.IsReady;
		_notReady.Visible = !player.IsReady;
		_teamColor.Color = TeamColor.Colors[player.TeamNumber];
	}

	private void _on_kick_button_pressed()
	{
		if (!Multiplayer.IsServer())
		{
			return;
		}
		
		GetNode<NetworkManager>("/root/Main/NetworkManager")
			.Rpc(NetworkManager.MethodName.DisconnectPlayer, _currentPlayer.PlayerId);
	}
}
