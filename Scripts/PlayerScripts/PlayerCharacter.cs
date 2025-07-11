using CardBase.Scripts.Abilities;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.PlayerScripts;

public partial class PlayerCharacter : CharacterBody2D, IHitableObject
{
    [Export] private MultiplayerSynchronizer _inputSync;
    [Export] private AnimatedSprite2D _playerAnimation;
    private PlayerInput _playerInput;
    private GameManager _gameManager;

    public PlayerStats PlayerStats = new PlayerStats();
    public Array<Ability> Abilities = new Array<Ability>();

    [Export] private Label _playerNameLabel;
    [Export] private ProgressBar _playerHealth;

    [Signal]
    public delegate void OnKilledEventHandler(PlayerCharacter victim, PlayerCharacter killer);
    
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

    public int TeamId { get; set; }
    public long PlayerId { get; set; }
    
    public override void _EnterTree()
    {
        _inputSync.SetMultiplayerAuthority(int.Parse(Name));
        _playerInput = (PlayerInput)_inputSync;
        PlayerStats.MovementSpeed = 300;
        _playerHealth.MaxValue = 100;
        _gameManager = (GameManager)GetNode("/root/Main/Game");
        _playerAnimation.Material = _playerAnimation.Material.Duplicate() as ShaderMaterial;
        var spriteMaterial = _playerAnimation.Material as ShaderMaterial;
        var teamColor = TeamColor.GetColor(TeamId);
        
        spriteMaterial?.SetShaderParameter("team_color", teamColor);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Multiplayer.IsServer())
        {
            if (!PlayerStats.IsDead)
            {
                _move(delta);
            }
        }
    }
    
    public override void _Process(double delta)
    {
        ((PlayerAnimation)_playerAnimation).UpdateAnimation();
        if (_inputSync.GetMultiplayerAuthority() == Multiplayer.GetUniqueId())
        {
            for (var i = 0; i < Abilities.Count; i++)
            {   
                Abilities[i].HandleInput(_playerInput.KeyState[i] ,delta);
                Abilities[i].UpdateCooldown(delta);
            }
        }

        _playerHealth.Value = PlayerStats.CurrentLife * 100 / PlayerStats.Life;
    }

    public void ApplyDamage(double damage, PlayerCharacter attacker)
    {
        if (Multiplayer.GetUniqueId() == _inputSync.GetMultiplayerAuthority())
        {
            Rpc(MethodName.applyDamageServer, damage, long.Parse(attacker.Name));
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void applyDamageServer(double damage, long attackerId)
    {
        if (Multiplayer.IsServer())
        {
            if (PlayerStats.IsDead)
            {
                return;
            }
        
            this.PlayerStats.CurrentLife -= (int)damage;
            Rpc(MethodName.SyncPlayerLife,  Name, PlayerStats.CurrentLife);
            if (PlayerStats.CurrentLife > 0)
            {
                return;
            }

            PlayerStats.IsDead = true;
            var attacker = _gameManager.GetPlayerCharacter(attackerId);
            EmitSignal(SignalName.OnKilled, this, attacker);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SyncPlayerLife(string name, int currenLife)
    {
        if (Name == name)
        {
            PlayerStats.CurrentLife = currenLife;
        }
    }

    private void _move(double delta)
    {
        var inputDir = new Vector2(_playerInput.XDirection, _playerInput.YDirection);
        Velocity = inputDir * PlayerStats.MovementSpeed;
        MoveAndSlide();
    }
}