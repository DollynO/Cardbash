using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardBase.Scripts;
using CardBase.Scripts.Cards;
using CardBase.Scripts.GameSettings;
using Godot.Collections;

public partial class LobbyManager : ColorRect
{
    [Export] private TextureButton readyButton;
    [Export] private TextureButton notReadyButton;
    [Export] private TextureButton startButton;

    [Export] private OptionButton _teamSelect;
    [Export] private OptionButton _deckSelect;

    [Export] private VBoxContainer _playerListContainer;
    [Export] private PackedScene _playerSlotScene;
    [Export] private Array<LineEdit> gameSettingFields;
    [Export] private CheckBox friendlyFireToggle;
    private Array<PlayerSlot> _playerSlots = new();

    private SceneManager _sceneManager;
    private NetworkManager _network;
    private bool _subscribedToServerDisconnected;
    private bool _subscribedToPlayerJoined;

    private Player currentPlayer;
    private GameplayConfigEditor _configEditor;
    private readonly HashSet<long> _configSyncAcks = new();
    private string _configSyncHash = string.Empty;
    private bool _isStartingGame;

    // Called when the node enters the scene tree for the first time.
    public override void _EnterTree()
    {
    }

    public override void _Ready()
    {
        _sceneManager = GetNode<SceneManager>("/root/Main");
        _network = GetNode<NetworkManager>(NetworkManager.GetNetworkManagerPath());
        GameplayConfigManager.LoadHostConfig();
        _network.OnServerDisconnected += open_main_menu;
        _subscribedToServerDisconnected = true;
        if (Multiplayer.IsServer())
        {
            _network.OnPlayerJoined += on_player_joined;
            _subscribedToPlayerJoined = true;
            AddGameplayConfigEditor();
        }

        while (_teamSelect.ItemCount > 0)
        {
            _teamSelect.RemoveItem(0);
        }

        foreach (var color in ColorPlate.Colors)
        {
            var image = new Image();
            var dummy = new byte[20 * 20 * 3];
            image.SetData(20, 20, false, Image.Format.Rgb8, dummy);
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
            if (child is PlayerSlot playerSlot)
            {
                _playerSlots.Add(playerSlot);
            }
        }
    }

    public void update_ui()
    {
        startButton.Disabled = _isStartingGame || !(Multiplayer.IsServer() && _network.CurrentPlayers.Values.All(p => p.IsReady));
        var playerCount = _network.CurrentPlayers.Count;
        var playerList = _network.CurrentPlayers.Values.ToList();
        currentPlayer = playerList.FirstOrDefault(p => p.PlayerId == Multiplayer.GetUniqueId());

        EnsurePlayerSlotCount(playerCount);

        for (var i = 0; i < _playerSlots.Count; i++)
        {
            var slot = _playerSlots[i];
            if (i < playerCount)
            {
                slot.Visible = true;
                slot.UpdateSlotUi(playerList[i]);
            }
            else
            {
                slot.Visible = false;
            }
        }

        _teamSelect.Selected = currentPlayer?.TeamNumber ?? 0;
    }

    private void EnsurePlayerSlotCount(int playerCount)
    {
        while (_playerSlots.Count < playerCount)
        {
            PlayerSlot slot = null;
            if (_playerSlotScene != null)
            {
                slot = _playerSlotScene.Instantiate<PlayerSlot>();
            }
            else if (_playerSlots.Count > 0)
            {
                slot = _playerSlots[0].Duplicate() as PlayerSlot;
            }

            if (slot == null)
            {
                GD.PrintErr("Unable to create lobby player slot.");
                return;
            }

            slot.Name = $"PlayerSlot{_playerSlots.Count + 1}";
            _playerListContainer.AddChild(slot);
            _playerSlots.Add(slot);
        }
    }

    public override void _ExitTree()
    {
        if (_network == null)
        {
            return;
        }

        if (_subscribedToServerDisconnected)
        {
            _network.OnServerDisconnected -= open_main_menu;
            _subscribedToServerDisconnected = false;
        }

        if (_subscribedToPlayerJoined)
        {
            _network.OnPlayerJoined -= on_player_joined;
            _subscribedToPlayerJoined = false;
        }
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
        StartGame();
    }

    private async void StartGame()
    {
        if (!Multiplayer.IsServer() || _isStartingGame)
        {
            return;
        }

        _isStartingGame = true;
        update_ui();

        var settings = new GameModeSettings();
        settings.CardsPerRound = int.TryParse(gameSettingFields[0].Text, out var value) ? value : 3;
        settings.PointsToWin = int.TryParse(gameSettingFields[1].Text, out value) ? value : 100;
        settings.PointsOnRoundEnd = int.TryParse(gameSettingFields[2].Text, out value) ? value : 15;
        settings.PointsOnKill = int.TryParse(gameSettingFields[3].Text, out value) ? value : 10;
        settings.FriendlyFire = friendlyFireToggle?.ButtonPressed ?? false;

        if (!await SyncGameplayConfigBeforeGameStart())
        {
            _isStartingGame = false;
            update_ui();
            return;
        }

        _sceneManager.LoadGameScene(settings);
    }

    private void _on_back_pressed()
    {
        if (Multiplayer.IsServer())
        {
            _network.CloseServer();
            return;
        }

        _network.DeleteClient();
        _sceneManager?.LoadMenuScene();
    }

    private void open_main_menu()
    {
        _sceneManager?.LoadMenuScene();
    }

    private void on_player_joined(long id)
    {
        Rpc(MethodName._allUnready);
    }

    private void AddGameplayConfigEditor()
    {
        var footer = startButton.GetParent<Control>();
        var configButton = new Button
        {
            Text = "Card Config",
            MouseFilter = Control.MouseFilterEnum.Stop,
            ZIndex = 10,
            AnchorLeft = 0,
            AnchorTop = 0.5f,
            AnchorRight = 0,
            AnchorBottom = 0.5f,
            OffsetLeft = 0,
            OffsetTop = -20,
            OffsetRight = 190,
            OffsetBottom = 20,
        };
        configButton.Pressed += () => _configEditor?.Open();
        footer.AddChild(configButton);

        _configEditor = new GameplayConfigEditor();
        _configEditor.ZIndex = 100;
        _configEditor.ConfigSaved += OnGameplayConfigSaved;
        GetNode<CanvasLayer>("CanvasLayer").AddChild(_configEditor);
    }

    private void OnGameplayConfigSaved()
    {
        Rpc(MethodName._allUnready);
    }

    private async Task<bool> SyncGameplayConfigBeforeGameStart()
    {
        GameplayConfigManager.LoadHostConfig();
        var json = GameplayConfigManager.GetMergedJson();
        _configSyncHash = GameplayConfigManager.ComputeHash(json);
        _configSyncAcks.Clear();

        Rpc(MethodName._syncGameplayConfig, json, _configSyncHash);

        var deadline = Time.GetTicksMsec() + 3000;
        while (Time.GetTicksMsec() < deadline)
        {
            var expectedAcks = Multiplayer.GetPeers().Length + 1;
            if (_configSyncAcks.Count >= expectedAcks)
            {
                return true;
            }

            await ToSignal(GetTree().CreateTimer(0.05), Timer.SignalName.Timeout);
        }

        GD.PrintErr("Timed out while syncing gameplay config to clients.");
        return false;
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

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void _syncGameplayConfig(string json, string hash)
    {
        if (!GameplayConfigManager.LoadSyncedConfig(json, hash, out var error))
        {
            GD.PrintErr(error);
            return;
        }

        if (Multiplayer.IsServer())
        {
            _configSyncAcks.Add(Multiplayer.GetUniqueId());
            return;
        }

        RpcId(1, MethodName._ackGameplayConfig, Multiplayer.GetUniqueId(), hash);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void _ackGameplayConfig(long playerId, string hash)
    {
        if (!Multiplayer.IsServer() || hash != _configSyncHash)
        {
            return;
        }

        _configSyncAcks.Add(playerId);
    }
}
