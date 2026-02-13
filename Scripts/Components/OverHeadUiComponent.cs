using System;
using System.Collections.Generic;
using Godot;

namespace CardBase.Scripts;

[GlobalClass]
public partial class OverHeadUiComponent : Node2D, IComponent
{
    public IEntityComponent Parent { get; private set; }
    public void SetParent(IEntityComponent component)
    {
        this.Parent = component;
    }

    public OverHeadUiComponent()
    {
        Name = "OverHeadUiComponent";
    }
    
    //[Export] private HBoxContainer _barContainer;
    private OverheadUiBar _lifeBar;
    private OverheadUiBar _stunBar;

    private IReadOnlyList<Node> _customContianer;

    private float refreshTimeMax = 1f/60f;
    private float refreshTime = 0;

    private MoveComponent moveController;
    private HealthComponent healtController;
    public override void _Ready()
    {
        SetMultiplayerAuthority(1);
        if (!Parent.TryGetComponent(out moveController) || !Parent.TryGetComponent(out healtController))
        {
            throw new NullReferenceException();
        }

        
        SetPosition( new Vector2(0, -63));
        
        _lifeBar = new OverheadUiBar(Color.FromHtml("dc5845"), Color.FromHtml("99e299"));
        AddChild(_lifeBar);
        _lifeBar.Name = "LifeBar";
        _lifeBar.Position = new Vector2(0, 0);
        
        _stunBar = new OverheadUiBar(Color.FromHtml("7c7c7c"), Color.FromHtml("c1c1c1"));
        AddChild(_stunBar);
        _stunBar.Name = "StunBar";
        _stunBar.Position = new  Vector2(0, 20);

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