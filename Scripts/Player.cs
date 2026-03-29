using CardBase.Scripts.Cards;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts;

public partial class Player : Node
{
    private enum PropertyIds{
        Username = 1,
        TeamNumber = 2,
        PlayerId = 3,
        IsReady = 4
    } 
    
    public override void _EnterTree()
    {
        SetMultiplayerAuthority(int.Parse(Name));
    }

    public override void _Ready()
    {
        GD.Print("Player: " + Name);
        PlayerId = int.Parse(Name);
    }

    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            if (IsInsideTree() && IsMultiplayerAuthority())
            {
                Rpc(MethodName.SyncProperty, 1, value);
            }
        }
    }
    private string _username;

    public long PlayerId
    {
        get => _playerId;
        set
        {
            _playerId = value;
            if (IsInsideTree() && IsMultiplayerAuthority())
            {
                Rpc(MethodName.SyncProperty, 3, value);
            }
        }
    }
    private long _playerId;

    public bool IsReady
    {
        get => _isReady;
        set
        {
            _isReady = value;
            if (IsInsideTree() && IsMultiplayerAuthority())
            {
                Rpc(MethodName.SyncProperty, 4, value);
            }
        }
    }
    private bool _isReady;

    public int TeamNumber
    {
        get => _teamNumber;
        set
        {
            _teamNumber = value % ColorPlate.MaxTeams;
            if (IsInsideTree() && IsMultiplayerAuthority())
            {
                Rpc(MethodName.SyncProperty, 2, value);
            }
        }
    }

    private int _teamNumber;

    public Deck SelectedDeck { get; set; }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncProperty(int property, Variant value)
    {
        
        switch (property)
        {
            case 1:
                Username = (string)value;
                break;
            case 2:
                TeamNumber = (int)value;
                break;
            case 3:
                PlayerId = (long)value;
                break;
            case 4:
                IsReady = (bool)value;
                break;
            
            default:
                return;
        }
    }

    public Dictionary<int, Variant> ToDict()
    {
        var dict = new Dictionary<int, Variant>()
        {
            { (int)PropertyIds.Username, Username },
            { (int)PropertyIds.TeamNumber, TeamNumber },
            { (int)PropertyIds.PlayerId, PlayerId },
            { (int)PropertyIds.IsReady, IsReady }
        };
        
        return dict;
    }

    public static Player FromDict(Dictionary<int, Variant> dict)
    {
        var player = new Player();
        player.Username = (string)dict[(int)PropertyIds.Username];
        player.TeamNumber = (int)dict[(int)PropertyIds.TeamNumber];
        player.PlayerId = (long)dict[(int)PropertyIds.PlayerId];
        player.IsReady = (bool)dict[(int)PropertyIds.IsReady];
        player.Name = player.PlayerId.ToString();

        return player;
    }
}