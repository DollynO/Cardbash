using Godot;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using CardBase.Scripts.Cards;

public partial class MainScreen : Control
{
    private const string UsernameSettingsPath = "user://player_username.txt";

    [Export] private Panel _mpScreen;

    [Export] private LineEdit _username;

    [Export] private LineEdit _ip_address;
    [Export] private LineEdit _port;
    [Export] private Label _localIpAddress;

    private NetworkManager _network;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _network = GetNode<NetworkManager>(NetworkManager.GetNetworkManagerPath());
        _network.OnConnectedToServer += OnConnectedToServer;
        _username.Text = LoadSavedUsername();
        UpdateLocalIpAddress();
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
        _username.Text = LoadSavedUsername();
        UpdateLocalIpAddress();
        _mpScreen.Visible = true;
    }

    private void _on_exit_pressed()
    {
        GetTree().Quit();
    }

    private void _on_cancel_mp_pressed()
    {
        _port.Text = string.Empty;
        _username.Text = LoadSavedUsername();
        _mpScreen.Visible = false;
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
        SaveUsername(_network.LocalUsername);
    }

    private static string LoadSavedUsername()
    {
        using var file = FileAccess.Open(UsernameSettingsPath, FileAccess.ModeFlags.Read);
        return file?.GetAsText().Trim() ?? string.Empty;
    }

    private static void SaveUsername(string username)
    {
        if (username == null)
        {
            return;
        }

        username = username.Trim();
        if (string.IsNullOrEmpty(username))
        {
            return;
        }

        using var file = FileAccess.Open(UsernameSettingsPath, FileAccess.ModeFlags.Write);
        file.StoreString(username);
    }
}
