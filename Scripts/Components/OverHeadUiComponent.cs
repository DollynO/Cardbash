using System;
using System.Collections.Generic;
using Godot;

namespace CardBase.Scripts;

[GlobalClass]
public partial class OverHeadUiComponent : Node2D, IComponent
{
    public IEntityComponent Parent { get; set; }
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
    private HBoxContainer _barContainer;

    private IReadOnlyList<Node> _customContianer;

    private float refreshTimeMax = 1f/60f;
    private float refreshTime = 0;

    private MoveComponent moveController = null;
    private HealthComponent healtController = null;
    private VisualComponent vs = null;
    public override void _Ready()
    {
        SetMultiplayerAuthority(1);
        _barContainer = new HBoxContainer();
        AddChild(_barContainer);
        _barContainer.Name = "BarContainer";
        _barContainer.Position = new Vector2(0, 0);
        _barContainer.AnchorBottom = 1;
        

        if (Parent.TryGetComponent(out healtController))
        {
            _lifeBar = new OverheadUiBar(Color.FromHtml("dc5845"), Color.FromHtml("99e299"));
            _lifeBar.Name = "LifeBar";
            _lifeBar.Position = new Vector2(0, 0);
            _barContainer.AddChild(_lifeBar);
        }

        if (Parent.TryGetComponent(out moveController))
        {
            _stunBar = new OverheadUiBar(Color.FromHtml("7c7c7c"), Color.FromHtml("c1c1c1"));
            _stunBar.Name = "StunBar";
            _stunBar.Position = new  Vector2(0, -10);
            _barContainer.AddChild(_stunBar);
        }

        Parent.TryGetComponent(out vs);
        SetPosition( new Vector2(0, -vs.TextureSize.Y/2));
        
    }
    
    public override void _Process(double delta)
    {

        Scale = Vector2.One / ((Node2D)Parent).Scale;
        
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
            { "MaxHealth", healtController?.MaxHealth ?? -1 },
            { "CurrentHealth", healtController?.CurrentHealth ?? -1 },
            { "Stun", moveController?.Stun ?? false },
            { "MaxStunTime", moveController?.MaxStunTime ?? 0 },
            { "StunTime", moveController?.StunTime ?? 0 },
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
        if (_lifeBar != null)
        {
            var maxHealth = (float)dict["MaxHealth"];
            _lifeBar.Visible = maxHealth >= 0;
            _lifeBar.MaxValue = maxHealth;
            _lifeBar.Value = (float)dict["CurrentHealth"];
        }

        if (_stunBar != null)
        {
            if (_stunBar.Visible != (bool)dict["Stun"])
            { 
                _stunBar.Visible = (bool)dict["Stun"];   
            }

            _stunBar.MaxValue = (float)dict["MaxStunTime"];
            _stunBar.Value = (float)dict["StunTime"];
        }
    }
}