using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.Cards;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.PlayerScripts;

public partial class PlayerCharacter : CharacterBody2D, IHitableObject
{
    [Export] private MultiplayerSynchronizer _inputSync;
    [Export] private AnimatedSprite2D _playerAnimation;
    private PlayerInput _playerInput;
    private GameManager _gameManager;

    [Export] public StatBlockComponent StatBlock;
    public readonly List<Ability> Abilities = new();
    public readonly List<DamageModifier> DamageModifier = new();

    [Export] private Label _playerNameLabel;
    [Export] private ProgressBar _playerHealth;
    
    [Export] private Sprite2D _lookAtIndicator;
    [Export] private Node2D _lookAtDirectionPoint;
    private Vector2 _lookAtDirectionCorrection = Vector2.FromAngle(Mathf.Tau / 4);
    [Export] private Node2D _characterCenterPoint;
    
    [Export]
    private Camera2D _camera;

    private Rect2 _mapBounds;

    [Export] private BuffManagerComponent _buffManagerComponent;
    
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
    
    public Array<Card> Cards = new Array<Card>();
    public Array<Card> SelectedCards = new Array<Card>();
    public int TeamId { get; set; }
    public long PlayerId { get; set; }
    
    public override void _EnterTree()
    {
        _inputSync.SetMultiplayerAuthority(int.Parse(Name));
        _playerInput = (PlayerInput)_inputSync;
        StatBlock.Define(StatType.MovementSpeed, 300, 0);
        StatBlock.Define(StatType.CurrentLife, 100);
        StatBlock.Define(StatType.Life, 100, float.NegativeInfinity, float.PositiveInfinity, new []
            {
                StatType.CurrentLife
            });
        StatBlock.Define(StatType.Armor, 0);
        StatBlock.Define(StatType.EnergyShield, 0);
        StatBlock.Define(StatType.CritBonus, 0);
        StatBlock.Define(StatType.CritChance, 0);
        StatBlock.Define(StatType.Int, 0);
        StatBlock.Define(StatType.Str, 0);
        StatBlock.Define(StatType.Darkness, 0);
        StatBlock.Define(StatType.Blinding, 0);

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
            _camera.Enabled = true;
            _mapBounds = _gameManager.GetMapBoundry();
            _camera.LimitTop = (int)_mapBounds.Position.Y;
            _camera.LimitLeft = (int)_mapBounds.Position.X;
            _camera.LimitRight = (int)_mapBounds.Position.X + (int)_mapBounds.Size.X;
            _camera.LimitBottom = (int)_mapBounds.Position.Y + (int)_mapBounds.Size.Y;
        }

        DamageModifier.Add(new DamageModifier {OutputDamageType = DamageType.Fire, TargetDamageType = DamageType.Ice, Type = DamageModifierType.ExtraDamage, Value = 10.0f});
        DamageModifier.Add(new DamageModifier {OutputDamageType = DamageType.Ice, TargetDamageType = DamageType.Ice, Type = DamageModifierType.Modifier, Value = 10.0f});
        DamageModifier.Add(new DamageModifier {OutputDamageType = DamageType.Fire, TargetDamageType = DamageType.Fire, Type = DamageModifierType.Modifier, Value = 10.0f});
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Multiplayer.IsServer())
        {
            if (!this.playerIsDead())
            {
                _move(delta);
            }
        }
    }

    private bool playerIsDead()
    {
        return StatBlock.GetStat(StatType.CurrentLife) <= 0;
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

        _playerHealth.Value = StatBlock.GetStat(StatType.CurrentLife) * 100 / StatBlock.GetStat(StatType.Life);
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

    public void ApplyDamage(System.Collections.Generic.Dictionary<DamageType, Damage> damage, PlayerCharacter attacker)
    {
        var dict = new Godot.Collections.Dictionary<DamageType, Variant>();
        foreach (var dmg in damage)
        {
            dict.Add(dmg.Key, dmg.Value.ToDict());
        }
        Rpc(MethodName.applyDamageServer, dict, long.Parse(attacker.Name));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void applyDamageServer(Dictionary dict, long attackerId)
    {
        if (Multiplayer.IsServer())
        {

            var damage = new System.Collections.Generic.Dictionary<DamageType, Damage>();
            foreach (var entry in dict)
            {
                damage.Add((DamageType)(int)entry.Key,
                    Damage.FromDict((Godot.Collections.Dictionary<string, Variant>)entry.Value));
            }
            
            if (playerIsDead())
            {
                return;
            }

            var attacker = _gameManager.GetPlayerCharacter(attackerId);
            foreach (var dmg in damage)
            {
                var defenseStat = dmg.Key switch
                {
                    DamageType.Physical => StatBlock.GetStat(StatType.Armor),
                    DamageType.Poison => StatBlock.GetStat(StatType.Armor),
                    DamageType.Darkness => 0,
                    DamageType.Holy => 0,
                    DamageType.Fire => StatBlock.GetStat(StatType.EnergyShield),
                    DamageType.Ice => StatBlock.GetStat(StatType.EnergyShield),
                    DamageType.Lightning => StatBlock.GetStat(StatType.EnergyShield),
                    _ => 0,
                };

                var dr = defenseStat / (defenseStat + 5 * dmg.Value.DamageNumber);
                StatBlock.AddModifiers(new StatModifier(Damage.SOURCE_MODIFIER_ID, StatType.CurrentLife, StatOp.FlatAdd, -(dmg.Value.DamageNumber * (1 - dr))));
                ApplyDamageTypeAilment(dmg.Value.Type, dmg.Value.AilmentChange, attacker);
            }

            if (playerIsDead())
            {
                EmitSignal(SignalName.OnKilled, this, attacker);
            }
        }
    }

    private void _move(double delta)
    {
        var inputDir = new Vector2(_playerInput.XDirection, _playerInput.YDirection);
        Velocity = inputDir * StatBlock.GetStat(StatType.MovementSpeed);
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

    private void ApplyDamageTypeAilment(DamageType type, float ailmentChange, PlayerCharacter attacker)
    {
        if (ailmentChange < 1)
        {
            return;
        }
        
        switch (type)
        {
            case DamageType.Fire:
                _buffManagerComponent.ApplyBuff(new BurnDebuff(attacker, this));
                break;
            case DamageType.Physical:
                break;
            case DamageType.Poison:
                break;
            case DamageType.Ice:
                break;
            case DamageType.Lightning:
                break;
            case DamageType.Darkness:
                break;
            case DamageType.Holy:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public void ApplyBuff(Buff buff)
    {
        if (Multiplayer.IsServer())
        {
            _buffManagerComponent.ApplyBuff(buff);
        }
        else
        {
            var dict = new Godot.Collections.Dictionary<string, Variant>()
            {
                { "guid", buff.Guid },
                { "caller_id", buff.Caller.PlayerId },
                { "target_id", buff.Target.PlayerId },
            };
            RpcId(1, MethodName.applyBuffServer, dict);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void applyBuffServer(Dictionary dict)
    {
        var caller = _gameManager.GetPlayerCharacter((long)dict["caller_id"]);
        var target = _gameManager.GetPlayerCharacter((long)dict["target_id"]);
        var buff = BuffManager.Create((string)dict["guid"], caller, target);
        ApplyBuff(buff);
    }
}