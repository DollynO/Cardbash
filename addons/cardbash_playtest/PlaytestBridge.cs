#if TOOLS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CardBase.Scripts;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.Cards;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Playtesting;

// Compiled only for editor development builds; activation also requires a flag and token.
public partial class PlaytestBridge : Node
{
    private const int MaxRequestBytes = 65536;
    private readonly TcpServer _server = new();
    private readonly List<Connection> _connections = new();
    private readonly Queue<object> _events = new();
    private readonly HashSet<string> _heldActions = new();
    private SceneManager _main;
    private string _token;
    private string _artifactDirectory;
    private bool _stopping;
    private bool _commandRunning;
    private static PlaytestBridge _active;
    private Vector2? _aimPosition;
    private bool _aimInWorldSpace;

    // Synthetic viewport events do not update the OS pointer used by GetGlobalMousePosition.
    // Keep a virtual pointer for bridge instances, including headless and unfocused clients.
    public static bool TryGetAimPosition(out Vector2 position)
    {
        position = default;
        if (_active?._aimPosition is not Vector2 aim) return false;
        position = _active._aimInWorldSpace ? aim : _active.GetViewport().GetCanvasTransform().AffineInverse() * aim;
        return true;
    }

    private sealed class Connection
    {
        public StreamPeerTcp Peer;
        public readonly List<byte> Input = new();
        public byte[] Output;
        public int Sent;
        public bool Running;
        public ulong Created = Time.GetTicksMsec();
    }

    public static void Attach(SceneManager main)
    {
        var argument = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--playtest-port="));
        if (argument == null || Engine.IsEditorHint()) return;
        var token = OS.GetEnvironment("CARDBASH_PLAYTEST_TOKEN");
        var artifacts = OS.GetEnvironment("CARDBASH_PLAYTEST_ARTIFACTS");
        if (!int.TryParse(argument.Split('=', 2)[1], out var port) || port < 1024 || port > 65535
            || token.Length < 32 || !Path.IsPathFullyQualified(artifacts))
        {
            GD.PushError("Playtest bridge requires a valid port, token, and absolute artifact directory.");
            return;
        }
        var bridge = new PlaytestBridge
        {
            Name = "PlaytestBridge", _main = main, _token = token, _artifactDirectory = artifacts,
            ProcessMode = ProcessModeEnum.Always,
        };
        var error = bridge._server.Listen((ushort)port, "127.0.0.1");
        if (error != Error.Ok)
        {
            GD.PushError($"Playtest bridge could not listen on port {port}: {error}");
            bridge._server.Dispose();
            bridge.Free();
            return;
        }
        main.AddChild(bridge);
        _active = bridge;
        EventBus.Instance.EventTraceEventHandler += bridge.RecordEvent;
        GD.Print($"PLAYTEST_READY port={port}");
    }

    public override void _Process(double delta)
    {
        if (_server.IsConnectionAvailable())
        {
            var peer = _server.TakeConnection();
            if (_connections.Count >= 8) { peer.DisconnectFromHost(); peer.Dispose(); }
            else _connections.Add(new Connection { Peer = peer });
        }
        foreach (var connection in _connections.ToArray())
        {
            connection.Peer.Poll();
            if (connection.Peer.GetStatus() != StreamPeerTcp.Status.Connected
                || Time.GetTicksMsec() - connection.Created > 20000)
            {
                Close(connection);
                continue;
            }
            if (connection.Output != null)
            {
                var result = connection.Peer.PutPartialData(connection.Output.AsSpan(connection.Sent).ToArray());
                connection.Sent += (int)result[1];
                if ((Error)(int)result[0] != Error.Ok || connection.Sent == connection.Output.Length)
                    Close(connection);
                continue;
            }
            if (connection.Running) continue;
            var available = connection.Peer.GetAvailableBytes();
            if (available <= 0) continue;
            if (connection.Input.Count + available > MaxRequestBytes) { Close(connection); continue; }
            var data = connection.Peer.GetData(available);
            if ((Error)(int)data[0] != Error.Ok) { Close(connection); continue; }
            connection.Input.AddRange((byte[])data[1]);
            var newline = connection.Input.IndexOf((byte)'\n');
            if (newline < 0) continue;
            connection.Running = true;
            _ = Respond(connection, Encoding.UTF8.GetString(connection.Input.ToArray(), 0, newline));
        }
    }

    private async Task Respond(Connection connection, string request)
    {
        var ownsCommand = false;
        object response;
        try
        {
            using var document = JsonDocument.Parse(request);
            var root = document.RootElement;
            Require(Text(root, "token") == _token, "Invalid playtest token.");
            Require(!_commandRunning, "Another playtest command is running; retry after it completes.");
            _commandRunning = ownsCommand = true;
            var args = root.TryGetProperty("arguments", out var value) ? value : default;
            response = new { ok = true, result = await Execute(Text(root, "command"), args) };
        }
        catch (Exception error)
        {
            response = new { ok = false, error = error.Message };
        }
        finally
        {
            if (ownsCommand) _commandRunning = false;
        }
        connection.Output = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response) + "\n");
    }

    private async Task<object> Execute(string command, JsonElement args)
    {
        switch (command)
        {
            case "ping": return new { version = 1, pid = OS.GetProcessId(), user_data = OS.GetUserDataDir() };
            case "state": return Snapshot();
            case "catalog":
                return GlobalCardManager.Instance.AbilityCards.Values.Cast<Card>()
                    .Concat(GlobalCardManager.Instance.ItemCards.Values)
                    .Select(c => new { guid = c.EffectGUID, name = c.DisplayName, type = c.CardType.ToString() }).ToArray();
            case "tree":
                return Walk(_main, Integer(args, "depth", 5, 0, 12)).Take(500).Select(NodeInfo).ToArray();
            case "events": return _events.ToArray();
            case "wait":
                await Frames(Integer(args, "frames", 1, 1, 600));
                return Snapshot();
            case "host":
            case "join":
                Require(Game == null && Lobby == null, "Leave the current session before hosting or joining.");
                Network.LocalUsername = Text(args, "username", command == "host" ? "Playtest host" : "Playtest client");
                var port = Integer(args, "port", 18080, 1024, 65535);
                if (command == "host") Network.StartHost(port);
                else Network.StartClient("127.0.0.1", port);
                _main.LoadLobbyScene();
                await Until(() => Network.Multiplayer.MultiplayerPeer?.GetConnectionStatus()
                    == MultiplayerPeer.ConnectionStatus.Connected && Network.CurrentPlayers.ContainsKey(Multiplayer.GetUniqueId()));
                return Snapshot();
            case "prepare_player":
                Require(Lobby != null, "Player preparation requires a lobby.");
                Require(Network.CurrentPlayers.ContainsKey(Multiplayer.GetUniqueId()), "Local player has not connected yet.");
                var deck = new Deck { DisplayName = "Playtest deck", GUID = Guid.NewGuid().ToString() };
                try
                {
                    deck.SetIcon(0);
                    var cards = args.ValueKind == JsonValueKind.Object && args.TryGetProperty("cards", out var cardArgs)
                        ? cardArgs : default;
                    if (cards.ValueKind == JsonValueKind.Undefined)
                        deck.Cards.Add(GlobalCardManager.Instance.AbilityCards[AbilityIds.FireballGuid], new Counter(10));
                    else
                    {
                        Require(cards.ValueKind == JsonValueKind.Object, "cards must map card GUIDs to counts.");
                        foreach (var entry in cards.EnumerateObject())
                        {
                            var card = FindCard(entry.Name);
                            var count = entry.Value.GetInt32();
                            Require(count >= 1 && count <= 100, "Card counts must be between 1 and 100.");
                            deck.Cards.Add(card, new Counter(count));
                        }
                        Require(deck.Cards.Count > 0 && deck.Cards.Count <= 100, "Supply 1 to 100 card types.");
                    }
                    var team = Integer(args, "team", Multiplayer.IsServer() ? 0 : 1, 0, ColorPlate.Colors.Count - 1);
                    AddChild(deck);
                    GlobalCardManager.Instance.Decks.Add(deck);
                    Lobby.update_ui();
                    Lobby.Call("_on_deck_selected", GlobalCardManager.Instance.Decks.Count - 1);
                    Lobby.Call("_on_team_selected", team);
                    Lobby.Call("_on_not_ready_pressed");
                }
                catch { if (deck.GetParent() == null) deck.Free(); throw; }
                await Frames(3);
                return Snapshot();
            case "start_match":
                Require(Lobby != null && Multiplayer.IsServer(), "Only the lobby host can start a match.");
                Require(Network.CurrentPlayers.Count > 0 && Network.CurrentPlayers.Values.All(p => p.IsReady && p.SelectedDeck != null),
                    "Every player must prepare a deck and be ready.");
                var fields = Lobby.Get("gameSettingFields").AsGodotArray<LineEdit>();
                fields[0].Text = Integer(args, "cards_per_round", 1, 1, 20).ToString();
                Lobby.Call("_on_start_pressed");
                await Until(() => Game?.GetPlayerCharacter(Multiplayer.GetUniqueId())?.HealthComponent?.MaxHealth > 0);
                return Snapshot();
            case "choose_card":
                Require(Game != null, "No match is running.");
                var guid = Text(args, "guid");
                var cardBox = Game.Hud.Get("_cardBox").AsGodotObject() as Control;
                var template = Walk(cardBox, 8).OfType<CardTemplate>()
                    .FirstOrDefault(c => c.IsVisibleInTree() && c.Card?.EffectGUID == guid && !c.IsQueuedForDeletion());
                Require(template != null, "That card is not in the visible hand.");
                template.EmitSignal(CardTemplate.SignalName.CardClicked, template.Card);
                Game.Hud.Call("_on_card_lock_pressed");
                await Frames(3);
                return Snapshot();
            case "input":
                var actions = args.TryGetProperty("actions", out var actionArgs)
                    ? actionArgs.EnumerateArray().Select(a => a.GetString()).ToArray() : Array.Empty<string>();
                Require(actions.Length <= 16 && actions.All(a => a != null && InputMap.HasAction(a)), "Unknown input action.");
                var frames = Integer(args, "frames", 1, 1, 300);
                if (args.TryGetProperty("aim", out var aim))
                {
                    var point = Point(aim);
                    var space = Text(args, "aim_space", "viewport");
                    Require(space is "world" or "viewport", "aim_space must be world or viewport.");
                    _aimPosition = point;
                    _aimInWorldSpace = space == "world";
                    if (_aimInWorldSpace) point = GetViewport().GetCanvasTransform() * point;
                    GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
                }
                try
                {
                    foreach (var action in actions) SetAction(action, true);
                    await Frames(frames);
                }
                finally { ReleaseActions(); }
                await Frames(2);
                return Snapshot();
            case "place_player":
                Require(Game != null && Multiplayer.IsServer(), "Only the match host can place players for a test.");
                var playerId = args.GetProperty("player_id").GetInt64();
                var player = Game.GetPlayerCharacter(playerId);
                Require(player != null && player.TryGetComponent<MoveComponent>(out _), "Player has not spawned.");
                player.TryGetComponent<MoveComponent>(out var movement);
                var destination = Point(args.GetProperty("position"));
                // Apply on the next physics tick; a render-frame tween can clear
                // its final position before headless physics observes it.
                movement.RequestReposition(destination, 0);
                await Until(() => player.GlobalPosition.DistanceTo(destination) < 1);
                await Frames(2);
                return Snapshot();
            case "click":
                var position = Point(args.GetProperty("position"));
                GetViewport().PushInput(new InputEventMouseMotion { Position = position, GlobalPosition = position }, true);
                GetViewport().PushInput(new InputEventMouseButton { Position = position, GlobalPosition = position,
                    ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true }, true);
                await Frames(1);
                GetViewport().PushInput(new InputEventMouseButton { Position = position, GlobalPosition = position,
                    ButtonIndex = MouseButton.Left, Pressed = false }, true);
                await Frames(2);
                return Snapshot();
            case "screenshot":
                Require(DisplayServer.GetName() != "headless", "Screenshots require a rendered instance; launch with headless=false.");
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using (var image = GetViewport().GetTexture().GetImage())
                {
                    var width = Integer(args, "max_width", 1280, 320, 1920);
                    if (image.GetWidth() > width)
                        image.Resize(width, Math.Max(1, image.GetHeight() * width / image.GetWidth()));
                    Directory.CreateDirectory(_artifactDirectory);
                    var path = Path.Combine(_artifactDirectory, $"frame-{Time.GetTicksUsec()}.png");
                    Require(image.SavePng(path) == Error.Ok, "Could not save screenshot.");
                    return new { path, width = image.GetWidth(), height = image.GetHeight() };
                }
            case "leave":
                ReleaseActions();
                _aimPosition = null;
                if (Game != null) Game.LeaveGame(Multiplayer.GetUniqueId());
                else if (Lobby != null) Lobby.Call("_on_back_pressed");
                await Until(() => Game == null && Lobby == null);
                return Snapshot();
            default: throw new ArgumentException($"Unknown playtest command: {command}");
        }
    }

    private NetworkManager Network => _main.GetNode<NetworkManager>("NetworkManager");
    private GameManager Game => _main.GetNodeOrNull<GameManager>("Game");
    private LobbyManager Lobby => _main.GetNodeOrNull<LobbyManager>("LobbyScreen");

    private object Snapshot()
    {
        var game = Game;
        var peer = Network.Multiplayer.MultiplayerPeer;
        var connected = peer?.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;
        var cardBox = game?.Hud.Get("_cardBox").AsGodotObject() as Control;
        return new
        {
            stage = game != null ? "game" : Lobby != null ? "lobby" : "menu",
            peer_id = connected ? Multiplayer.GetUniqueId() : 0,
            is_server = connected && Multiplayer.IsServer(),
            connection = peer?.GetConnectionStatus().ToString() ?? "Disconnected",
            physics_frame = Engine.GetPhysicsFrames(), fps = Engine.GetFramesPerSecond(),
            viewport = Vec(GetViewport().GetVisibleRect().Size),
            lobby_players = Network.CurrentPlayers.Values.Select(p => new
                { id = p.PlayerId, name = p.Username, team = p.TeamNumber, ready = p.IsReady, deck = p.SelectedDeck?.DisplayName }).ToArray(),
            players = game?.GetPlayers().Where(GodotObject.IsInstanceValid).Select(p => new
            {
                id = p.PlayerId, name = p.PlayerName, team = p.TeamId, position = Vec(p.GlobalPosition),
                velocity = Vec(p.Velocity), health = p.HealthComponent?.CurrentHealth, max_health = p.HealthComponent?.MaxHealth,
                targetable = p.IsTargetable, kills = p.Kills, deaths = p.Deaths,
                aim = p.PlayerInput == null ? null : Vec(p.PlayerInput.ClientGlobalMousePosition),
                abilities = p.AbilityComponent == null ? Array.Empty<object>() : (Multiplayer.IsServer()
                    ? p.AbilityComponent.Abilities.Select(a => NetAbility.CreateFromAbility(a.Value, a.Key))
                    : p.AbilityComponent.GetNetAbilities()).Select(a => (object)new
                    { guid = a.GUID, name = GlobalCardManager.Instance.AbilityCards.TryGetValue(a.GUID, out var card)
                            ? card.DisplayName : a.GUID,
                        slot = a.Index + 1, cooldown = a.Cooldowns.X, max_cooldown = a.Cooldowns.Y,
                        stacks = a.Stacks.X, max_stacks = a.Stacks.Y, level = a.SkillLevel }).ToArray(),
            }).ToArray(),
            card_selection_visible = cardBox?.IsVisibleInTree() ?? false,
            hand = cardBox == null ? Array.Empty<object>() : Walk(cardBox, 8).OfType<CardTemplate>()
                .Where(c => !c.IsQueuedForDeletion() && c.Card != null).Select(c => (object)new
                { guid = c.Card.EffectGUID, name = c.Card.DisplayName }).ToArray(),
        };
    }

    private static object NodeInfo(Node node) => new
    {
        path = node.GetPath().ToString(), type = node.GetClass().ToString(),
        visible = node is CanvasItem item ? (bool?)item.IsVisibleInTree() : null,
        text = node switch { Label label => label.Text, Button button => button.Text,
            LineEdit edit => edit.Text, _ => null },
        rect = node is Control control ? new { position = Vec(control.GetGlobalRect().Position), size = Vec(control.Size) } : null,
    };

    private static IEnumerable<Node> Walk(Node node, int depth)
    {
        if (node == null) yield break;
        yield return node;
        if (depth <= 0) yield break;
        foreach (var child in node.GetChildren())
            foreach (var descendant in Walk(child, depth - 1)) yield return descendant;
    }

    private static Card FindCard(string guid)
    {
        if (GlobalCardManager.Instance.AbilityCards.TryGetValue(guid, out var ability)) return ability;
        if (GlobalCardManager.Instance.ItemCards.TryGetValue(guid, out var item)) return item;
        throw new ArgumentException($"Unknown card GUID: {guid}. Use catalog to list cards.");
    }

    private async Task Frames(int count)
    {
        for (var frame = 0; frame < count; frame++)
        {
            Require(!_stopping, "Playtest instance is stopping.");
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
    }

    private async Task Until(Func<bool> condition)
    {
        var deadline = Time.GetTicksMsec() + 8000;
        while (!condition())
        {
            Require(Time.GetTicksMsec() < deadline, "Timed out waiting for game state; inspect state and logs.");
            await Frames(1);
        }
    }

    private void SetAction(string action, bool pressed)
    {
        if (pressed) _heldActions.Add(action); else _heldActions.Remove(action);
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = pressed, Strength = pressed ? 1f : 0f });
    }

    private void ReleaseActions()
    {
        foreach (var action in _heldActions.ToArray()) SetAction(action, false);
    }

    private void RecordEvent(object sender, EventBusTraceEventArgs args)
    {
        _events.Enqueue(new { time_ms = Time.GetTicksMsec(), bus = args.BusName, name = args.EventName });
        while (_events.Count > 200) _events.Dequeue();
    }

    private void Close(Connection connection)
    {
        connection.Peer.DisconnectFromHost();
        connection.Peer.Dispose();
        _connections.Remove(connection);
    }

    public override void _ExitTree()
    {
        _stopping = true;
        if (_active == this) _active = null;
        ReleaseActions();
        EventBus.Instance.EventTraceEventHandler -= RecordEvent;
        _server.Stop();
        _server.Dispose();
        foreach (var connection in _connections.ToArray()) Close(connection);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string Text(JsonElement args, string key, string fallback = "") =>
        args.ValueKind == JsonValueKind.Object && args.TryGetProperty(key, out var value) ? value.GetString() ?? fallback : fallback;

    private static int Integer(JsonElement args, string key, int fallback, int min, int max)
    {
        var number = args.ValueKind == JsonValueKind.Object && args.TryGetProperty(key, out var value) ? value.GetInt32() : fallback;
        Require(number >= min && number <= max, $"{key} must be between {min} and {max}.");
        return number;
    }

    private static Vector2 Point(JsonElement args)
    {
        var x = args.GetProperty("x").GetSingle();
        var y = args.GetProperty("y").GetSingle();
        Require(float.IsFinite(x) && float.IsFinite(y), "Coordinates must be finite.");
        return new Vector2(x, y);
    }

    private static object Vec(Vector2 vector) => new { x = vector.X, y = vector.Y };
}
#endif
