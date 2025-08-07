using System.Collections.Generic;
using System.Linq;
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

    public PlayerStats PlayerStats = new();
    public readonly List<Ability> Abilities = new();
    public readonly List<DamageModifier> DamageModifier = new();

    [Export] private Label _playerNameLabel;
    [Export] private ProgressBar _playerHealth;
    
    [Export] private Sprite2D _lookAtIndicator;
    [Export] private Node2D _lookAtDirectionPoint;
    private Vector2 _lookAtDirectionCorrection = Vector2.FromAngle(Mathf.Tau / 4);

    [Export] private Node2D _characterCenterPoint;
    
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

        if (int.Parse(Name) == Multiplayer.GetUniqueId())
        {
            _lookAtIndicator.Visible = true;
            _lookAtIndicator.Material = _lookAtIndicator.Material.Duplicate() as ShaderMaterial;
            spriteMaterial = _lookAtIndicator.Material as ShaderMaterial;
            spriteMaterial?.SetShaderParameter("mask_color", new Color(1f, 1f, 1f));
            spriteMaterial?.SetShaderParameter("team_color", teamColor);
            spriteMaterial?.SetShaderParameter("tolerance", 0.4);
        }

        DamageModifier.Add(new DamageModifier {OutputDamageType = DamageType.Fire, TargetDamageType = DamageType.Ice, Type = DamageModifierType.ExtraDamage, Value = 10.0f});
        DamageModifier.Add(new DamageModifier {OutputDamageType = DamageType.Ice, TargetDamageType = DamageType.Ice, Type = DamageModifierType.Modifier, Value = 10.0f});
        DamageModifier.Add(new DamageModifier {OutputDamageType = DamageType.Fire, TargetDamageType = DamageType.Fire, Type = DamageModifierType.Modifier, Value = 10.0f});
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

    public Vector2 GetLookAtDirection()
    {
        return (_lookAtDirectionPoint.GlobalPosition - _characterCenterPoint.GlobalPosition).Normalized();
    }

    public Vector2 GetProjectileStartPosition()
    {
        return _lookAtDirectionPoint.GlobalPosition;
    }

    public Vector2 GetCharacterCenterPosition()
    {
        return _characterCenterPoint.GlobalPosition;
    }

    public void ApplyDamage(Godot.Collections.Dictionary<DamageType, float> damage, PlayerCharacter attacker)
    {
        Rpc(MethodName.applyDamageServer, damage, long.Parse(attacker.Name));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void applyDamageServer(Godot.Collections.Dictionary<DamageType, float> damage, long attackerId)
    {
        if (Multiplayer.IsServer())
        {
            if (PlayerStats.IsDead)
            {
                return;
            }

            foreach (var dmg in damage)
            {
                var defenseStat = dmg.Key switch
                {
                    DamageType.Physical => PlayerStats.Armor,
                    DamageType.Poison => PlayerStats.Armor,
                    DamageType.Darkness => 0,
                    DamageType.Holy => 0,
                    DamageType.Fire => PlayerStats.EnergyShield,
                    DamageType.Ice => PlayerStats.EnergyShield,
                    DamageType.Lightning => PlayerStats.EnergyShield,
                    _ => 0,
                };

                var dr = defenseStat / (defenseStat + 5 * dmg.Value);
                
                this.PlayerStats.CurrentLife -= (int)(dmg.Value * (1 - dr));
            }
            
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

    public void RegisterAbilityObject(Node node)
    {
        if (node is ICustomSpawnObject customSpawnObject)
        {
            if (Multiplayer.GetUniqueId() == customSpawnObject.CreatorId)
            {
                Abilities.FirstOrDefault(a => a.GUID == customSpawnObject.AbilityGuid)?
                    .RegisterSpawnedNode(node);
            }
        }
    }

    public void RequestMeleeCone(MeleeConeProperties properties)
    {
        var dict = properties.ToDict();
        Rpc(MethodName.CreateMeleeCone, dict);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void CreateMeleeCone(Godot.Collections.Dictionary<string, Variant>dict)
    {
        var cone = new MeleeCone();
        var property = MeleeConeProperties.FromDict(dict);
        property.Owner = this;
        cone.SetStats(property);
        _characterCenterPoint.AddChild(cone);
    }
}