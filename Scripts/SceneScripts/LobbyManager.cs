using Godot;
using System;
using System.Linq;
using CardBase.Scripts;
using CardBase.Scripts.Cards;
using Godot.Collections;

public partial class LobbyManager : ColorRect
{
	[Export] private TextureButton ReadyButton;
	[Export] private TextureButton NotReadyButton;
	[Export] private TextureButton StartButton;
	
	[Export] private OptionButton _teamSelect;
	[Export] private OptionButton _deckSelect;

	[Export] private VBoxContainer _playerListContainer;
	private Array<PlayerSlot> _playerSlots = new();

	private SceneManager _sceneManager;
	private NetworkManager _network;
	private Player _currentPlayer;
	
	// Called when the node enters the scene tree for the first time.
	public override void _EnterTree()
	{
	}

	public override void _Ready()
	{		
		_sceneManager = GetNode<SceneManager>("/root/Main");
		_network = GetNode<NetworkManager>(NetworkManager.GetNetworkManagerPath());
		_network.OnServerDisconnected += open_main_menu;
		if (Multiplayer.IsServer())
		{
			_network.OnPlayerJoined += (long id) => { Rpc(MethodName._allUnready); };
		}

		while (_teamSelect.ItemCount > 0)
		{
			_teamSelect.RemoveItem(0);
		}
		
		foreach (var color in TeamColor.Colors)
		{
			var image = new Image();
			var dummy = new byte[20 * 20 * 3];
			image.SetData(20,20, false, Image.Format.Rgb8, dummy);
			image.Fill(color);
			var texture = new ImageTexture();
			
			texture.SetImage(image);
			_teamSelect.AddIconItem(texture, string.Empty);
		}

		while (_deckSelect.ItemCount > 0)
		{
			_deckSelect.RemoveItem(0);
		}
		
		foreach (var deck in GlobalCardManager.Instance.Decks)
		{
			_deckSelect.AddItem(deck.DisplayName);
		}
		_deckSelect.Selected = -1;
		
		
		ReadyButton.Visible = false;
		foreach (var child in _playerListContainer.GetChildren())
		{
			_playerSlots.Add(child as PlayerSlot);
		}
	}

	public void update_ui()
	{
		StartButton.Disabled = !(Multiplayer.IsServer() && _network.CurrentPlayers.All(p =>p.IsReady));
		var player_count = _network.CurrentPlayers.Count;
		for (var i = 0; i < _playerSlots.Count; i++)
		{
			var slot = _playerSlots[i];
			if (i < player_count)
			{
				slot.Visible = true;
				slot.UpdateSlotUi(_network.CurrentPlayers[i]);
				if (_network.CurrentPlayers[i].PlayerId == Multiplayer.GetUniqueId())
				{
					_currentPlayer = _network.CurrentPlayers[i];
				}
			}
			else
			{
				slot.Visible = false;
			}
		}
		
		_teamSelect.Selected = _currentPlayer?.TeamNumber ?? 0;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void _on_ready_pressed()
	{
		if (_currentPlayer?.SelectedDeck == null)
		{
			return;
		}
		
		_currentPlayer.IsReady = false;
		NotReadyButton.Visible = true;
		ReadyButton.Visible = false;
	}

	private void _on_not_ready_pressed()
	{
		if (_currentPlayer?.SelectedDeck == null)
		{
			return;
		}
		
		Rpc(MethodName._syncDeck, _currentPlayer.PlayerId, _currentPlayer.SelectedDeck.ToDict());
		
		_currentPlayer.IsReady = true;
		NotReadyButton.Visible = false;
		ReadyButton.Visible = true;
	}

	private void _on_start_pressed()
	{
		_sceneManager?.Rpc(SceneManager.MethodName.LoadGameScene);
	}

	private void _on_back_pressed()
	{
		_sceneManager?.LoadMenuScene();
		
		Multiplayer?.MultiplayerPeer.Close();
	}

	private void open_main_menu()
	{
		_sceneManager?.LoadMenuScene();
	}
	
	private void _on_team_selected(int index)
	{
		_currentPlayer.TeamNumber = index;
	}

	private void _on_deck_selected(int index)
	{
		_currentPlayer.SelectedDeck = GlobalCardManager.Instance.Decks[index];
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void _syncDeck(long id, Dictionary deckDict)
	{
		if (_network.CurrentPlayers.FirstOrDefault(p => p.PlayerId == id) is { } player)
		{
			player.SelectedDeck = Deck.FromDict(deckDict);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void _allUnready()
	{
		foreach (var player in _network.CurrentPlayers)
		{
			player.IsReady = false;
		}
	}
}
