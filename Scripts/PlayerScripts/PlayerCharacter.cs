using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.Cards;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace CardBase.Scripts.PlayerScripts;

public partial class PlayerCharacter : CharacterbodyEntityComponent, ITeamAffiliation
{
    [Export] private MultiplayerSynchronizer _inputSync;
    [Export] private AnimatedSprite2D _playerAnimation;
    private PlayerInput _playerInput;
    private GameManager _gameManager;


    [Export] private Label _playerNameLabel;
    
    [Export] private Sprite2D _lookAtIndicator;
    [Export] private Node2D _lookAtDirectionPoint;
    private Vector2 _lookAtDirectionCorrection = Vector2.FromAngle(Mathf.Tau / 4);
    [Export] private Node2D _characterCenterPoint;
    
    [Export]
    private Camera2D _camera;

    private Rect2 _mapBounds;

    public BuffManagerComponent BuffManagerComponent;

    [Signal]
    public delegate void OnKilledEventHandler(long victimId, long killerId);
    
    public HealthComponent HealthComponent { get; private set; }
    public AbilityComponent AbilityComponent { get; private set; }
    public StatblockComponent StatBlock { get; private set; }
    
    public string PlayerName
    {
        get => _playerName;
        set
        {
            _playerName = value;
            _playerNameLabel.Text = value;
        }
    }
    private string _playerName;
    
    public Array<Card> Cards = new Array<Card>();
    public Array<Card> SelectedCards = new Array<Card>();
    public int TeamId { get; set; }
    public long PlayerId { get; set; }
    
    public event EventHandler<PlayerEventArgs>? KilledPlayer;
    public void NotifyPlayerKilled(PlayerCharacter victim)
    {
        this.KilledPlayer?.Invoke(this, new PlayerEventArgs(victim));
    }
    
    public event EventHandler<DamageEventArgs>? DamageDealt;
    public void NotifyDamageDealt(List<Damage> damage)
    {
        this.DamageDealt?.Invoke(this, new DamageEventArgs(damage));
    }
    
    public event EventHandler<DamageEventArgs>? DamageMitigated;
    public void NotifyDamageMitigated(List<Damage> damage)
    {
        this.DamageMitigated?.Invoke(this, new DamageEventArgs(damage));
    }
    
    public event EventHandler<BuffEventArgs>? BuffApplied;
    public void NotifyBuffApplied(Buff buff)
    {
        this.BuffApplied?.Invoke(this, new BuffEventArgs(buff));
    }
    
    public event EventHandler<BuffEventArgs>? BuffRemoved;
    public void NotifyBuffRemoved(Buff buff)
    {
        this.BuffRemoved?.Invoke(this, new BuffEventArgs(buff));
    }

    public event EventHandler? NewRoundStarted;
    
    public override void _EnterTree()
    {
        _inputSync.SetMultiplayerAuthority(int.Parse(Name));
        _playerInput = (PlayerInput)_inputSync;
        
        _gameManager = (GameManager)GetNode("/root/Main/Game");
        _playerAnimation.Material = _playerAnimation.Material.Duplicate() as ShaderMaterial;
        var spriteMaterial = _playerAnimation.Material as ShaderMaterial;
        var teamColor = ColorPlate.GetColor(TeamId);
        
        spriteMaterial?.SetShaderParameter("team_color", teamColor);

        if (int.Parse(Name) == Multiplayer.GetUniqueId())
        {
            _lookAtIndicator.Visible = true;
            _lookAtIndicator.Material = _lookAtIndicator.Material.Duplicate() as ShaderMaterial;
            spriteMaterial = _lookAtIndicator.Material as ShaderMaterial;
            spriteMaterial?.SetShaderParameter("mask_color", new Godot.Color(1f, 1f, 1f));
            spriteMaterial?.SetShaderParameter("team_color", teamColor);
            spriteMaterial?.SetShaderParameter("tolerance", 0.4);
            _camera.Enabled = true;
            _mapBounds = _gameManager.GetMapBoundry();
            _camera.LimitTop = (int)_mapBounds.Position.Y;
            _camera.LimitLeft = (int)_mapBounds.Position.X;
            _camera.LimitRight = (int)_mapBounds.Position.X + (int)_mapBounds.Size.X;
            _camera.LimitBottom = (int)_mapBounds.Position.Y + (int)_mapBounds.Size.Y;
        }

        BuffManagerComponent = new BuffManagerComponent();
        AddComponent(BuffManagerComponent);
        AbilityComponent = new AbilityComponent
        {
            Position = _characterCenterPoint.Position
        };
        AddComponent(AbilityComponent);
        
        HealthComponent = new HealthComponent();
        AddComponent(HealthComponent);
        
        var dac = new DamageAbleComponent();
        AddComponent(dac);
        
        StatBlock = new StatblockComponent();
        AddComponent(StatBlock);
        
        AddComponent(new MoveComponent());
        
        var aimComponent = new AimComponent(_characterCenterPoint, _lookAtDirectionPoint, _lookAtDirectionCorrection);
        AddComponent(aimComponent);
       
        var overhead = new OverHeadUiComponent();
        AddComponent(overhead);
    }

    public override void _Ready()
    {
        defineCharacterStats();
    }
    
    private void defineCharacterStats()
    {
        StatBlock.Define(StatType.MovementSpeed, 300, 0, float.PositiveInfinity);
        StatBlock.Define(StatType.Life, 100, float.NegativeInfinity, float.PositiveInfinity);
        StatBlock.Define(StatType.Armor, 0, 0, float.PositiveInfinity);
        StatBlock.Define(StatType.EnergyShield, 0, float.NegativeInfinity, float.PositiveInfinity);
        StatBlock.Define(StatType.CritBonus, 0, 0, float.PositiveInfinity);
        StatBlock.Define(StatType.CritChance, 0, 0, 100);
        StatBlock.Define(StatType.Darkness, 0, 0, 20);
        StatBlock.Define(StatType.Blinding, 0, 0, 20);
        
        StatBlock.Define(StatType.AddPullRadius, 0, 0, float.PositiveInfinity);
        StatBlock.Define(StatType.AddPullStrength, 0, 0,  float.PositiveInfinity);
        StatBlock.Define(StatType.CooldownReduction, 1, 0.2f, 1.8f); // max +-80% cooldown 
    }

    public override void _PhysicsProcess(double delta)
    {
        if (TryGetComponent(out MoveComponent moveComponent))
        {
            moveComponent.ProcessMovement(delta, new Vector2(_playerInput.XDirection, _playerInput.YDirection));
        }
    }

    public override void _Process(double delta)
    {
        ((PlayerAnimation)_playerAnimation).UpdateAnimation();
        if (Multiplayer.IsServer())
        {
            if (TryGetComponent(out AbilityComponent abilityComponent))
            {
                abilityComponent.ProcessAbilities(delta, _playerInput.KeyState.ToArray());
            }
        }
    }



    public void RoundReset()
    {
        BuffManagerComponent.ClearAllBuffs();
        StatBlock.RemoveModifierSource(Damage.SOURCE_MODIFIER_ID);
        if (TryGetComponent(out HealthComponent healthComponent))
        {
            healthComponent.Reset(StatBlock.GetStat(StatType.Life));
        }

        this.NewRoundStarted?.Invoke(this,  EventArgs.Empty);
    }

    public void EnterStealth()
    {
        Rpc(MethodName.enterStealthServer);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void enterStealthServer()
    {
        if (Multiplayer.GetUniqueId() == this.PlayerId)
        {
            this.Modulate = new Godot.Color(this.Modulate, 0.5f);
        }
        else
        {
            this.Modulate = new Godot.Color(this.Modulate, 0.0f);
        }
    }
    
    public void ExitStealth()
    {
        Rpc(MethodName.exitStealthServer);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,  CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void exitStealthServer()
    {
        this.Modulate = new Godot.Color(this.Modulate, 1.0f);
    }
    
}

public class AbilityEventArgs : EventArgs
{
    public readonly Ability Ability;
    public AbilityEventArgs(Ability ability)
    {
        Ability = ability;
    }
}

public class PlayerEventArgs : EventArgs
{
    public readonly PlayerCharacter Player;
    public PlayerEventArgs(PlayerCharacter player)
    {
        Player = player;
    }
}

public class DamageEventArgs : EventArgs
{
    public readonly List<Damage> Damage;
    public DamageEventArgs(List<Damage> damage)
    {
        Damage = damage;
    }
    
}

public class BuffEventArgs : EventArgs
{
    public readonly Buff Buff;
    public BuffEventArgs(Buff buff)
    {
        Buff = buff;
    }
}
