using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CardBase.Scripts;
using CardBase.Scripts.Cards;
using CardBase.Scripts.GameSettings;
using CardBase.Scripts.PlayerScripts;
using Godot.Collections;
using Array = Godot.Collections.Array;

public partial class GameManager : Node2D
{
	private int _playersInGame = 0;
	private int _playersReady = 0;

	private NetworkManager _network;
	[Export] private PackedScene _playerCharScene;
	[Export] private MultiplayerSpawner _spawner;
	[Export] private Hud _hud;
	
	private Godot.Collections.Dictionary<long, PlayerCharacter> _currentCharacters = new();
	private PlayerCharacter _currentPlayer;
	public GameSettings GameSettings;
	
	[Signal]
	public delegate void OnPlayerKilledEventHandler(PlayerCharacter victim, PlayerCharacter killer);
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_network = GetNode<NetworkManager>(NetworkManager.GetNetworkManagerPath());
		_hud.CardLocked += onCardLocked;
		
		Rpc(MethodName.im_in_game, Multiplayer.GetUniqueId());
		GameSettings = new GameSettings()
		{
			GameMode = new TeamDeathMatch()
		};
		
		if (Multiplayer.IsServer() && GameSettings is not null)
		{
			GameSettings.GameMode.OnGameOver += onGameOver;
			GameSettings.GameMode.OnRoundOver += onRoundOver;
			GameSettings.GameMode.AssignGameHooks(this);
		}
	}

	private void onRoundOver(int winnerId)
	{
		GameSettings.GameMode.CheckGameWinCondition(GetWorldContext());
		Rpc(MethodName.StartDrawPhase);
	}

	private void onGameOver(int winnerId)
	{
		showWinnerScreen();
	}

	private void showWinnerScreen()
	{
		
	}

	public WorldContext GetWorldContext()
	{
		return new WorldContext()
		{
			players = _currentCharacters.Values.ToList(),
			settings = GameSettings.WorldSettings,
		};
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_hud.UpdatePlayerHud(GetPlayerCharacter(Multiplayer.GetUniqueId()));
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void im_in_game(long id)
	{
		if (!Multiplayer.IsServer())
		{
			return;
		}
		
		_playersInGame += 1;

		if (_playersInGame == _network.CurrentPlayers.Count)
		{
			_spawn_players();
		}
	}

	private void _spawn_players()
	{
		foreach (var player in _network.CurrentPlayers)
		{
			_spawn_player_character(player);
		}

		foreach (var playerDict in _currentCharacters)
		{
			Rpc(MethodName.SyncPlayerStats, playerDict.Key, playerDict.Value.PlayerStats.ToDict());
		}
		Rpc(MethodName.StartDrawPhase);
	}

	private void _spawn_player_character(Player player)
	{
		var character = _playerCharScene.Instantiate();
		character.Name = player.Name;
		if (character is PlayerCharacter playerChar)
		{
			playerChar.PlayerStats.SetDefault();
			playerChar.PlayerName = player.Username;
			foreach (var cardCounter in player.SelectedDeck.Cards)
			{
				for (var i = 0; i < cardCounter.Value.Count; i++)
				{
					playerChar.PlayerStats.Cards.Add(cardCounter.Key);
				}
			}

			playerChar.OnKilled += onKillReported;
		}
		_spawner.AddChild(character, true);
		_currentCharacters.Add(int.Parse(player.Name), (PlayerCharacter)character);
		Rpc(MethodName.SyncPlayerName, int.Parse(player.Name), player.Username);
	}

	private void onKillReported(PlayerCharacter victim, PlayerCharacter killer)
	{
		EmitSignal(SignalName.OnPlayerKilled, victim, killer);	
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void StartDrawPhase()
	{		
		var player = GetPlayerCharacter(Multiplayer.GetUniqueId());
		if (player == null)
		{
			throw new Exception();
		}
		
		var cards = player.PlayerStats.Cards[..5].ToList();
		_hud.ShowDrawUi(true, cards);
	}

	private void onCardLocked(int id, Dictionary cardDict)
	{
		Rpc(MethodName.SyncLockCard, id, cardDict);
	}
	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SyncLockCard(long id, Dictionary cardDict)
	{
		if (Multiplayer.IsServer())
		{
			var card = Card.FromDict(cardDict);
			switch (card.CardType)
			{
				case CardType.Ability:
					Rpc(MethodName.SyncAbility, id, cardDict);
					break;
				case CardType.Item:
					var playerContext = new PlayerContext()
					{
						player = _currentCharacters[id],
					};
					card.ApplyEffect(playerContext);
					break;
				default:
					break;
			}
			var currentPlayer = _currentCharacters[id];
			currentPlayer.PlayerStats.SetDefault();
			Rpc(MethodName.SyncPlayerStats, id, _currentCharacters[id].PlayerStats.ToDict());
			
			_playersReady++;
			if (_playersReady == _network.CurrentPlayers.Count)
			{
				_playersReady = 0;
				Rpc(MethodName.StartGamePhase);
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SyncAbility(long id, Dictionary cardDict)
	{
		var card = Card.FromDict(cardDict);
		var playerContext = new PlayerContext()
		{
			player = GetPlayerCharacter(id),
		};
		card.ApplyEffect(playerContext);
	}

	[Rpc]
	public void SyncPlayerStats(long id, Dictionary statsDict)
	{
		var player = GetPlayerCharacter(id);
		player.PlayerStats.Update(PlayerStats.FromDict(statsDict));
	}

	[Rpc]
	public void SyncPlayerName(long id, string name)
	{
		var player = GetPlayerCharacter(id);
		player.PlayerName = name;
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void StartGamePhase()
	{
		_hud.ShowDrawUi(false, null);
	}

	public PlayerCharacter GetPlayerCharacter(long id)
	{
		if (id == Multiplayer.GetUniqueId())
		{
			return _currentPlayer ??=
				_spawner.GetChildren().FirstOrDefault(c => c.Name == id.ToString()) as PlayerCharacter;

		}

		return _spawner.GetChildren().FirstOrDefault(c => c.Name == id.ToString()) as PlayerCharacter;
	}
}
