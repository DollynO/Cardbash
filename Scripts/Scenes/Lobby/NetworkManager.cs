using Godot;
using System.Linq;
using CardBase.Scripts;
using Godot.Collections;

public partial class NetworkManager : Node
{
    public Dictionary<long, Player> CurrentPlayers = new();
    public string LocalUsername { get; set; }
    private bool _hostSignalsConnected;
    private bool _clientSignalsConnected;

    [Export] private PackedScene player_scene;
    //[Export] private MultiplayerSpawner multiplayer_spawner;

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
        SetMultiplayerAuthority(1);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public void StartHost(int port)
    {
        ResetNetworkState();
        CurrentPlayers.Clear();
        clearMultiplayerSpawner();
        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(port);
        Multiplayer.MultiplayerPeer = peer;

        Multiplayer.PeerConnected += _on_player_connected;
        Multiplayer.PeerDisconnected += _on_player_disconnected;
        _hostSignalsConnected = true;

        _on_player_connected(Multiplayer.GetUniqueId());
        _connected_to_server();
    }

    public void StartClient(string ip, int port)
    {
        ResetNetworkState();
        CurrentPlayers.Clear();
        var peer = new ENetMultiplayerPeer();
        peer.CreateClient(ip, port);
        Multiplayer.MultiplayerPeer = peer;

        Multiplayer.ConnectedToServer += _connected_to_server;
        Multiplayer.ConnectionFailed += _connection_failed;
        Multiplayer.ServerDisconnected += _server_disconnected;
        _clientSignalsConnected = true;
    }

    public void DeleteClient()
    {
        ResetNetworkState();
    }

    public async void CloseServer()
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        if (Multiplayer.MultiplayerPeer == null)
        {
            SceneManager.Instance?.LoadMenuScene();
            ResetNetworkState();
            return;
        }

        Rpc(MethodName.CloseNetworkSceneOnPeer);
        await ToSignal(GetTree().CreateTimer(0.1), Timer.SignalName.Timeout);

        if (Multiplayer.MultiplayerPeer != null)
        {
            Multiplayer.MultiplayerPeer.Close();
        }

        ResetNetworkState();
        SceneManager.Instance?.LoadMenuScene();
    }

    public async void DisconnectPlayer(int playerId)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        if (Multiplayer.MultiplayerPeer == null)
        {
            return;
        }

        if (!Multiplayer.GetPeers().Contains(playerId))
        {
            return;
        }

        removed_player(playerId);

        Multiplayer.MultiplayerPeer.DisconnectPeer(playerId);
    }

    private void _on_player_connected(long id)
    {
        if (id != 1)
        {
            var dict = new Dictionary<long, Variant>();
            foreach (var currentPlayer in CurrentPlayers)
            {
                dict.Add(currentPlayer.Key, currentPlayer.Value.ToDict());
            }

            RpcId(id, MethodName.syncPlayerList, dict);
        }

        Rpc(MethodName.spawnPlayerClient, id);
        EmitSignal(SignalName.OnPlayerJoined, id);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void syncPlayerList(Dictionary<long, Variant> dict)
    {
        foreach (var kvp in dict)
        {
            var playerDict = kvp.Value.AsGodotDictionary<int, Variant>();
            var player = Player.FromDict(playerDict);
            CurrentPlayers.Add(kvp.Key, player);
            AddChild(player);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void spawnPlayerClient(long id)
    {
        var player = player_scene.Instantiate<Player>();
        player.Name = id.ToString();
        player.TeamNumber = CurrentPlayers.Count;
        CurrentPlayers.Add(id, player);

        AddChild(player);

        if (Multiplayer.GetUniqueId() == id)
        {
            player.Username = LocalUsername;
        }
    }

    private void removed_player(long id)
    {
        Rpc(MethodName.despawnPlayerClient, id);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void despawnPlayerClient(long id)
    {
        if (CurrentPlayers.ContainsKey(id))
        {
            CurrentPlayers[id].QueueFree();
            CurrentPlayers.Remove(id);
        }
    }

    private void _on_player_disconnected(long id)
    {
        removed_player(id);
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
        DeleteClient();
        EmitSignal(SignalName.OnServerDisconnected);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void CloseNetworkSceneOnPeer()
    {
        ResetNetworkState();
        SceneManager.Instance?.LoadMenuScene();
    }

    private void ResetNetworkState()
    {
        if (_hostSignalsConnected)
        {
            Multiplayer.PeerConnected -= _on_player_connected;
            Multiplayer.PeerDisconnected -= _on_player_disconnected;
            _hostSignalsConnected = false;
        }

        if (_clientSignalsConnected)
        {
            Multiplayer.ConnectedToServer -= _connected_to_server;
            Multiplayer.ConnectionFailed -= _connection_failed;
            Multiplayer.ServerDisconnected -= _server_disconnected;
            _clientSignalsConnected = false;
        }

        foreach (var child in this.GetChildren())
        {
            child.QueueFree();
        }

        CurrentPlayers.Clear();

        if (Multiplayer.MultiplayerPeer != null)
        {
            Multiplayer.MultiplayerPeer.Close();
        }

        Multiplayer.MultiplayerPeer = null;
    }

    private void clearMultiplayerSpawner()
    {
        foreach (var player in CurrentPlayers.Values)
        {
            player.QueueFree();
        }

        CurrentPlayers.Clear();
    }
}
