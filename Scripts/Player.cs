using System.Linq;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.Cards;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts;

public partial class Player : Node
{
    private NetworkManager _network;

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(int.Parse(Name));
    }

    public override void _Ready()
    {
        _network = GetNode<NetworkManager>(NetworkManager.GetNetworkManagerPath());
        Username = _network.LocalUsername;
        PlayerId = int.Parse(Name);
        _network.AddPlayerToList(this);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            _network.RemovePlayerFromList(this);
        }
    }

    public string Username { get; set; }
    public long PlayerId { get; set; }
    public bool IsReady { get; set; }

    public int TeamNumber
    {
        get => _teamNumber;
        set => _teamNumber = value % TeamColor.MaxTeams;
    }
    private int _teamNumber;

    public Deck SelectedDeck { get; set; }
}