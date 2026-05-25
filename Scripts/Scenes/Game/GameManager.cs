using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CardBase.Scripts;
using CardBase.Scripts.Cards;
using CardBase.Scripts.GameSettings;
using CardBase.Scripts.PlayerScripts;
using Godot.Collections;

public partial class GameManager : Node2D
{
    public GameContext Context { get; private set; }
    private int _playersInGame;
    private int _playersReady;
    private readonly HashSet<long> _readyPeers = new();
    private bool _playersInitialized;

    private NetworkManager _network;
    [Export] private PackedScene _playerCharScene;
    [Export] private MultiplayerSpawner _spawner;
    [Export] public Hud Hud;
    [Export] private TileMapLayer _tileMapLayer;
    [Export] private Node2D _spawnPoint;
    private GameFlowController _flowController;

    private System.Collections.Generic.Dictionary<long, PlayerCharacter> _currentCharacters = new();
    private PlayerCharacter _currentPlayer;

    [Signal]
    public delegate void OnPlayerKilledEventHandler(PlayerCharacter victimId, PlayerCharacter killerId);
    
    public EventBus EventBus { get; } = new EventBus();
    public ScoreSystem ScoreSystem { get; private set; }
    public TeamSystem TeamSystem { get; private set; }
    public CardSystem CardSystem { get; private set; }
    public GameModeSettings Settings { get; } = new ();

    public override void _EnterTree()
    {
        _spawner.SpawnFunction = new Callable(this, MethodName.CustomSpawner);
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _network = GetNode<NetworkManager>(NetworkManager.GetNetworkManagerPath());

        this.TeamSystem = new TeamSystem(this, _currentCharacters);
        this.ScoreSystem = new ScoreSystem();
        this.CardSystem = new CardSystem(this);
        Context = new GameContext(this, _currentCharacters, TeamSystem, CardSystem, ScoreSystem);

        _flowController = new GameFlowController(Context, Settings);
        _flowController.Name = "flowControl";
        AddChild(_flowController);

        if (Multiplayer.IsServer())
        {
            im_in_game(Multiplayer.GetUniqueId());
        }
        else
        {
            RpcId(1, MethodName.im_in_game, Multiplayer.GetUniqueId());
        }
    }

    public void SetStats(GameModeSettings stats)
    {
        Settings.Copy(stats);
    }

    public void NotifyPlayerDeath(PlayerCharacter victim, PlayerCharacter killer)
    {
        EmitSignal(SignalName.OnPlayerKilled, victim.PlayerId, killer.PlayerId);
        victim.Cleanup();

    }

    public WorldContext GetWorldContext()
    {
        return new WorldContext()
        {
            players = _currentCharacters.Values.ToList(),
        };
    }

    public Rect2 GetMapBoundry()
    {
        var rect = _tileMapLayer.GetUsedRect();
        var tileSize = _tileMapLayer.TileSet.TileSize;

        var origion = _tileMapLayer.MapToLocal(rect.Position);
        var size = rect.Size * tileSize;
        return new Rect2(origion, size);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        Hud.UpdatePlayerHud(GetPlayerCharacter(Multiplayer.GetUniqueId()));
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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ClientReady(long peerId)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        _readyPeers.Add(peerId);
        TryInitializePlayers();
    }

    private void _spawn_players()
    {
        foreach (var player in _network.CurrentPlayers.Values)
        {
            _spawn_player_character(player);
        }

        //_readyPeers.Add(Multiplayer.GetUniqueId());
        //TryInitializePlayers();
    }

    private void TryInitializePlayers()
    {
        if (_playersInitialized)
        {
            return;
        }

        var expected = Multiplayer.GetPeers().Length + 1;
        if (_readyPeers.Count < expected)
        {
            return;
        }

        _playersInitialized = true;
        foreach (var player in _currentCharacters.Values)
        {
            player.InitializeServerStats();
        }
        _flowController.Start();
    }

    private void _spawn_player_character(Player player)
    {
        var deckDict = player.SelectedDeck.ToDict();
        var dict = new Godot.Collections.Dictionary<string, Variant>
        {
            { "playerName", player.Username },
            { "teamId", player.TeamNumber },
            { "playerId", player.Name },
            { "deck", deckDict}
        };
        var node = _spawner.Spawn(dict);
    }

    private Node CustomSpawner(Variant data)
    {
        var dic = data.AsGodotDictionary<string, Variant>();
        var playerName = (string)dic["playerName"];
        var teamId = int.Parse((string)dic["teamId"]);
        var playerId = (string)dic["playerId"];
        var deck = Deck.FromDict((Dictionary)dic["deck"]);
        if (_playerCharScene.Instantiate() is not PlayerCharacter node)
        {
            return null;
        }

        node.Name = playerId;
        node.PlayerName = playerName;
        node.TeamId = teamId;
        node.PlayerId = long.Parse(playerId);

        node.GlobalPosition = GetNextFreeSpawnPoint();
        node.Deck = deck;
        _currentCharacters.Add(node.PlayerId, node);
        node.AssignEventBus(this.EventBus);
        return node;
    }

    public Vector2 GetNextFreeSpawnPoint()
    {
        var random = new Random();
        var offset = 600;
        var angle = random.NextDouble() * Math.Tau;
        var randomSpawn = new Vector2(
            (float)Math.Cos(angle) * offset,
            (float)Math.Sin(angle) * offset);

        return _spawnPoint.GlobalPosition + randomSpawn;
    }

    private void onKillReported(long victimId, long killerId)
    {
        EmitSignal(SignalName.OnPlayerKilled, victimId, killerId);
    }

    [Rpc]
    public void SyncPlayerName(long id, string name)
    {
        var player = GetPlayerCharacter(id);
        player.PlayerName = name;
    }

    public PlayerCharacter GetPlayerCharacter(long id)
    {
        if (id == Multiplayer.GetUniqueId() && _currentPlayer != null)
        {
            return _currentPlayer;

        }

        foreach (var child in _spawner.GetChildren())
        {
            if (child is PlayerCharacter player && player.PlayerId == id)
            {
                if (id == Multiplayer.GetUniqueId())
                {
                    _currentPlayer = player;
                }
                return player;
            }
        }

        return null;
    }

    public IList<PlayerCharacter> GetPlayers()
    {
        return this._currentCharacters.Values.ToList();
    }

    public void LeaveGame(long playerId)
    {
        if (Multiplayer.IsServer())
        {

            foreach (var character in _currentCharacters.Values)
            {
                character.Cleanup();
                character.QueueFree();
            }
            _currentCharacters.Clear();
            _network.CloseServer();
            return;
        }

        RpcId(1, MethodName.requestClientLeaveMatch, playerId);
        Multiplayer.MultiplayerPeer = null;
        SceneManager.Instance.LeaveGame();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void requestClientLeaveMatch(long playerId)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        var character = _currentCharacters[playerId];
        if (character == null)
        {
            return;
        }

        character.Cleanup();
        _currentCharacters.Remove(playerId);
    }
}
