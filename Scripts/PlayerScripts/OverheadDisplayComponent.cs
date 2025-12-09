using System.Collections.Generic;
using Godot;

namespace CardBase.Scripts.PlayerScripts;

[GlobalClass]
public partial class OverheadDisplayComponent : Node2D
{
    //[Export] private HBoxContainer _barContainer;
    [Export] private ProgressBar _lifeBar;
    [Export] private ProgressBar _stunBar;
    [Export] private PlayerCharacter  _player;

    private IReadOnlyList<Node> _customContianer;

    private float refreshTimeMax = 1f/60f;
    private float refreshTime = 0;

    private MoveController moveController;
    private HealthController healtController;
    public override void _Ready()
    {
        SetMultiplayerAuthority(1);
        moveController = _player.MoveController;
        healtController = _player.HealthController;
    }

    public override void _Process(double delta)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }
        
        refreshTime += (float)delta;
        if (!(refreshTime > refreshTimeMax))
        {
            return;
        }
        
        refreshTime = 0;

        var dict = new Godot.Collections.Dictionary<string, Variant>()
        {
            { "MaxHealth", healtController.MaxHealth },
            { "CurrentHealth", healtController.CurrentHealth },
            { "Stun", moveController.Stun },
            { "MaxStunTime", moveController.MaxStunTime },
            { "StunTime", moveController.StunTime },
        };
        Rpc(MethodName.updateClient, dict);
    }

    public void Reset()
    {
        
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void updateClient(Godot.Collections.Dictionary<string, Variant> dict)
    {
        _lifeBar.MaxValue = (float)dict["MaxHealth"];
        _lifeBar.Value = (float)dict["CurrentHealth"];

        if (_stunBar.Visible != (bool)dict["Stun"])
        { 
            _stunBar.Visible = (bool)dict["Stun"];   
        }
        _stunBar.MaxValue = (float)dict["MaxStunTime"];
        _stunBar.Value = (float)dict["StunTime"]; 
    }
}