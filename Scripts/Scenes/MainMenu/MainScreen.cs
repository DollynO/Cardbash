using Godot;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using CardBase.Scripts.Cards;
using CardBase.Scripts.PlayerScripts;
using Godot.Collections;

public partial class MainScreen : Control
{
    private const string LobbySettingsPath = "user://lobby_settings.txt";

    [Export] private Panel _mpScreen;

    [Export] private LineEdit _username;

    [Export] private LineEdit _ip_address;
    [Export] private LineEdit _port;
    [Export] private Label _localIpAddress;
    [Export] private Panel _keybindingScreen;
    [Export] private Array<ButtonPrefab> _keybindingButtons;
    [Export] private Label _keybindingMessage;

    private NetworkManager _network;
    private int _listeningAbilitySlot = -1;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        AbilityKeyBindings.ApplySavedBindings();
        _network = GetNode<NetworkManager>(NetworkManager.GetNetworkManagerPath());
        _network.OnConnectedToServer += OnConnectedToServer;
        LoadLobbySettings(_username.Text,  _ip_address.Text);
        UpdateLocalIpAddress();
        UpdateKeybindingButtons();
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
        LoadLobbySettings(_username.Text,  _ip_address.Text);
        UpdateLocalIpAddress();
        _mpScreen.Visible = true;
    }

    private void _on_keybindings_pressed()
    {
        _listeningAbilitySlot = -1;
        _keybindingScreen.Visible = true;
        UpdateKeybindingButtons();
        SetKeybindingMessage("Select an ability, then press a key or mouse button.");
    }

    private void _on_exit_pressed()
    {
        GetTree().Quit();
    }

    private void _on_cancel_mp_pressed()
    {
        _port.Text = string.Empty;
        LoadLobbySettings(_username.Text,  _ip_address.Text);
        _mpScreen.Visible = false;
    }

    private void _on_cancel_keybindings_pressed()
    {
        _listeningAbilitySlot = -1;
        _keybindingScreen.Visible = false;
        UpdateKeybindingButtons();
    }

    private void _on_reset_keybindings_pressed()
    {
        _listeningAbilitySlot = -1;
        AbilityKeyBindings.ResetToDefaults();
        UpdateKeybindingButtons();
        SetKeybindingMessage("Ability bindings reset to defaults.");
    }

    private void _on_ability_1_key_pressed()
    {
        StartListeningForAbilityKey(0);
    }

    private void _on_ability_2_key_pressed()
    {
        StartListeningForAbilityKey(1);
    }

    private void _on_ability_3_key_pressed()
    {
        StartListeningForAbilityKey(2);
    }

    private void _on_ability_4_key_pressed()
    {
        StartListeningForAbilityKey(3);
    }

    public override void _Input(InputEvent @event)
    {
        if (_listeningAbilitySlot < 0)
        {
            return;
        }

        if (@event is InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            GetViewport().SetInputAsHandled();
            if (AbilityKeyBindings.TrySetKeyForSlot(_listeningAbilitySlot, keyEvent, out var error))
            {
                CompleteAbilityBinding();
                return;
            }

            SetKeybindingMessage(error);
            return;
        }

        if (@event is InputEventMouseButton { Pressed: true } mouseEvent)
        {
            GetViewport().SetInputAsHandled();
            if (AbilityKeyBindings.TrySetMouseButtonForSlot(_listeningAbilitySlot, mouseEvent, out var error))
            {
                CompleteAbilityBinding();
                return;
            }

            SetKeybindingMessage(error);
        }
    }

    private void _on_host_pressed()
    {
        if (!TrySetLocalUsername())
        {
            return;
        }

        _network.StartHost(int.Parse(_port.Text));
        var scene_manager = GetNode("..") as SceneManager;
        scene_manager?.LoadLobbyScene();
    }

    private void _on_join_pressed()
    {
        if (!TrySetLocalUsername())
        {
            return;
        }

        _network.StartClient(_ip_address.Text, int.Parse(_port.Text));
        var scene_manager = GetNode("..") as SceneManager;
        scene_manager?.LoadLobbyScene();
    }

    private bool TrySetLocalUsername()
    {
        var username = _username.Text.Trim();
        if (string.IsNullOrEmpty(username))
        {
            _username.Text = string.Empty;
            _username.PlaceholderText = "Username required";
            _username.GrabFocus();
            return false;
        }

        _network.LocalUsername = username;
        _username.Text = username;
        return true;
    }

    private void StartListeningForAbilityKey(int slot)
    {
        _listeningAbilitySlot = slot;
        UpdateKeybindingButtons();
        SetKeybindingMessage($"Press a new key or mouse button for Ability {slot + 1}.");
    }

    private void CompleteAbilityBinding()
    {
        var abilityNumber = _listeningAbilitySlot + 1;
        _listeningAbilitySlot = -1;
        UpdateKeybindingButtons();
        SetKeybindingMessage($"Ability {abilityNumber} binding updated.");
    }

    private void UpdateKeybindingButtons()
    {
        if (_keybindingButtons == null)
        {
            return;
        }

        for (var slot = 0; slot < _keybindingButtons.Count; slot++)
        {
            var button = _keybindingButtons[slot];
            if (button == null)
            {
                continue;
            }

            var text = _listeningAbilitySlot == slot
                ? $"Ability {slot + 1}: ..."
                : $"Ability {slot + 1}: {AbilityKeyBindings.GetBindingDisplayName(slot)}";
            button.SetText(text);
        }
    }

    private void SetKeybindingMessage(string message)
    {
        if (_keybindingMessage != null)
        {
            _keybindingMessage.Text = message;
        }
    }

    private void UpdateLocalIpAddress()
    {
        if (_localIpAddress == null)
        {
            return;
        }

        _localIpAddress.Text = GetLocalIpAddressText();
    }

    private static string GetLocalIpAddressText()
    {
        var addresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(networkInterface =>
                networkInterface.OperationalStatus == OperationalStatus.Up
                && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(networkInterface => networkInterface.GetIPProperties().UnicastAddresses)
            .Select(addressInfo => addressInfo.Address)
            .Where(address =>
                address.AddressFamily == AddressFamily.InterNetwork
                && !IPAddress.IsLoopback(address))
            .Select(address => address.ToString())
            .Distinct()
            .ToList();

        return addresses.Count == 0 ? "Not available" : string.Join(", ", addresses);
    }

    private void OnConnectedToServer()
    {
        SaveSettings(_network.LocalUsername, _network.HostIP);
    }

    private static void LoadLobbySettings(string name, string ip)
    {
        using var file = FileAccess.Open(LobbySettingsPath, FileAccess.ModeFlags.Read);
        name = file?.GetLine().Trim() ?? string.Empty;
        ip = file?.GetLine().Trim() ?? string.Empty;
    }

    private static void SaveSettings(string username, string ip)
    {
        if (username == null || ip == null)
        {
            return;
        }

        username = username.Trim();
        ip = ip.Trim();
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(ip))
        {
            return;
        }

        using var file = FileAccess.Open(LobbySettingsPath, FileAccess.ModeFlags.Write);
        file.StoreString($"{username}\n{ip}");
    }
}
