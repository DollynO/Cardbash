using Godot;
using System.Linq;
using CardBase.Scripts;
using CardBase.Scripts.Cards;
using Godot.Collections;

public partial class LobbyManager : ColorRect
{
	[Export] private TextureButton readyButton;
	[Export] private TextureButton notReadyButton;
	[Export] private TextureButton startButton;
	
	[Export] private OptionButton _teamSelect;
	[Export] private OptionButton _deckSelect;

	[Export] private VBoxContainer _playerListContainer;
	[Export] private Array<LineEdit> gameSettingFields;
	private Array<PlayerSlot> _playerSlots = new();

	private SceneManager _sceneManager;
	private NetworkManager _network;
	
	private Player currentPlayer;
	
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
			_network.OnPlayerJoined += _ => { Rpc(MethodName._allUnready); };
		}

		while (_teamSelect.ItemCount > 0)
		{
			_teamSelect.RemoveItem(0);
		}
		
		foreach (var color in ColorPlate.Colors)
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
		
		
		readyButton.Visible = false;
		foreach (var child in _playerListContainer.GetChildren())
		{
			_playerSlots.Add(child as PlayerSlot);
		}
	}

	public void update_ui()
	{
		startButton.Disabled = !(Multiplayer.IsServer() && _network.CurrentPlayers.Values.All(p =>p.IsReady));
		var playerCount = _network.CurrentPlayers.Count;
		var playerList = _network.CurrentPlayers.Values.ToList();
		
		for (var i = 0; i < _playerSlots.Count; i++)
		{
			var slot = _playerSlots[i];
			if (i < playerCount)
			{
				slot.Visible = true;
				slot.UpdateSlotUi(playerList[i]);
				if (playerList[i].PlayerId == Multiplayer.GetUniqueId())
				{
					currentPlayer = playerList[i];
				}
			}
			else
			{
				slot.Visible = false;
			}
		}
		
		_teamSelect.Selected = currentPlayer?.TeamNumber ?? 0;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void _on_ready_pressed()
	{
		if (currentPlayer?.SelectedDeck == null)
		{
			return;
		}
		
		currentPlayer.IsReady = false;
		notReadyButton.Visible = true;
		readyButton.Visible = false;
	}

	private void _on_not_ready_pressed()
	{
		if (currentPlayer?.SelectedDeck == null)
		{
			return;
		}
		
		Rpc(MethodName._syncDeck, currentPlayer.PlayerId, currentPlayer.SelectedDeck.ToDict());
		
		currentPlayer.IsReady = true;
		notReadyButton.Visible = false;
		readyButton.Visible = true;
	}

	private void _on_start_pressed()
	{
		var settings = new GameModeSettings();
		settings.CardsPerRound = int.TryParse(gameSettingFields[0].Text, out var value) ? value : 3;
		settings.PointsToWin = int.TryParse(gameSettingFields[1].Text, out value) ? value : 100;
		settings.PointsOnRoundEnd = int.TryParse(gameSettingFields[2].Text, out value) ? value : 15;
		settings.PointsOnKill = int.TryParse(gameSettingFields[3].Text, out value) ? value : 10;

		_sceneManager.LoadGameScene(settings);
	}

	private void _on_back_pressed()
	{
		Multiplayer?.MultiplayerPeer.Close();
		_sceneManager?.LoadMenuScene();
	}

	private void open_main_menu()
	{
		_sceneManager?.LoadMenuScene();
	}
	
	private void _on_team_selected(int index)
	{
		currentPlayer.TeamNumber = index;
		currentPlayer.IsReady = false;
	}

	private void _on_deck_selected(int index)
	{
		currentPlayer.SelectedDeck = GlobalCardManager.Instance.Decks[index];
		currentPlayer.IsReady = false;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void _syncDeck(long id, Dictionary deckDict)
	{
		if (_network.CurrentPlayers.TryGetValue(id, out var player))
		{
			player.SelectedDeck = Deck.FromDict(deckDict);
		}
	}

	[Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void _allUnready()
	{
		foreach (var player in _network.CurrentPlayers.Values)
		{
			player.IsReady = false;
		}
	}
}
