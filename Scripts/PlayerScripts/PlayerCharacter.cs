using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
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
    
    private Random rnd = new Random();
    
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
    
    private System.Collections.Generic.Dictionary<PlayerCharacter, Darkness> _darknessInstances;
    private System.Collections.Generic.Dictionary<PlayerCharacter, PoisonDebuff> _poisonInstances;
    
    [Export]
    private float _roundMaxLife;

    public override void _EnterTree()
    {
        _inputSync.SetMultiplayerAuthority(int.Parse(Name));
        _playerInput = (PlayerInput)_inputSync;
        StatBlock.Define(StatType.MovementSpeed, 300, 0);
        StatBlock.Define(StatType.Life, 100, float.NegativeInfinity, float.PositiveInfinity);
        StatBlock.Define(StatType.Armor, 0);
        StatBlock.Define(StatType.EnergyShield, 0);
        StatBlock.Define(StatType.CritBonus, 0);
        StatBlock.Define(StatType.CritChance, 0);
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
        return StatBlock.GetStat(StatType.Life) <= 0;
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
            
        } else if (Multiplayer.IsServer())
        {
            
        }

        _playerHealth.Value = StatBlock.GetStat(StatType.Life) * 100 / this._roundMaxLife;
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

    public void ApplyDamage(HitContext ctx)
    {
        
        Rpc(MethodName.applyDamageServer, ctx.ToDict());
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void applyDamageServer(Godot.Collections.Dictionary<string, Variant> dict)
    {
        if (Multiplayer.IsServer())
        {
            var ctx = HitContext.FromDict(dict, _gameManager);
            
            if (playerIsDead())
            {
                return;
            }
            
            var hitMods = ctx.Source.GetHitModifiers();
            var abilityHitMods = ctx.Source.Abilities.FirstOrDefault(a => a.GUID == ctx.AbilityGuid)?.GetHitModifiers();
            if (abilityHitMods != null)
            {
                hitMods.AddRange(abilityHitMods);
            }

            foreach (var mod in hitMods)
            {
                mod.ApplyBefore(ctx);
            }
            
            DamageCalculator.CalculateTotalDamage(ctx.Damages, ctx.Source.DamageModifier);

            // apply mitigation
            foreach (var dmg in ctx.Damages)
            {
                var defenseStat = dmg.Key switch
                {
                    DamageType.Physical or DamageType.Poison => StatBlock.GetStat(StatType.Armor),
                    DamageType.Darkness => 0,
                    DamageType.Holy => 0,
                    DamageType.Fire => StatBlock.GetStat(StatType.EnergyShield),
                    DamageType.Ice => StatBlock.GetStat(StatType.EnergyShield),
                    DamageType.Lightning => StatBlock.GetStat(StatType.EnergyShield),
                    _ => 0,
                };

                var dr = defenseStat / (defenseStat + 5 * dmg.Value.DamageNumber);
                ctx.Damages[dmg.Key].DamageNumber = dmg.Value.DamageNumber * (1 - dr);
                StatBlock.AddModifiers(new StatModifier(Damage.SOURCE_MODIFIER_ID, StatType.Life, StatOp.FlatAdd, -ctx.Damages[dmg.Key].DamageNumber));
                ApplyDamageTypeAilment(dmg.Value.Type, dmg.Value.AilmentChange, ctx.Source);
            }

            if (playerIsDead())
            {
                EmitSignal(SignalName.OnKilled, this, ctx.Source);
                _buffManagerComponent.ClearAllBuffs();
            }

            foreach (var mod in hitMods)
            {
                mod.ApplyAfter(ctx);
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

    private void ApplyDamageTypeAilment(DamageType type, float ailmentChance, PlayerCharacter attacker)
    {
        var chance = rnd.Next(0, 100) / 100;
        if (chance > ailmentChance)
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
                if (!_poisonInstances.ContainsKey(attacker))
                {
                    _poisonInstances.Add(attacker, new PoisonDebuff(attacker, this));
                }
                var poison =  _poisonInstances[attacker];
                _buffManagerComponent.ApplyBuff(poison);
                break;
            case DamageType.Ice:
                _buffManagerComponent.ApplyBuff(new Frost(attacker, this));
                break;
            case DamageType.Lightning:
                _buffManagerComponent.ApplyBuff(new ShockDebuff(attacker, this));
                break;
            case DamageType.Darkness:
                if (!_darknessInstances.ContainsKey(attacker))
                {
                    _darknessInstances.Add(attacker, new Darkness(attacker, this));
                }
                var darkness =  _darknessInstances[attacker];
                _buffManagerComponent.ApplyBuff(darkness);
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

    public int ConsumeBuffType(DamageType type)
    {
        if (Multiplayer.IsServer())
        {
            return this._buffManagerComponent.ConsumeBuffType(type);
        }

        return 0;
    }

    public int ConsumeBuff(Type type)
    {
        if (type.BaseType != typeof(Buff))
        {
            return 0;
        }
        
        if (Multiplayer.IsServer())
        {
            return this._buffManagerComponent.ConsumeBuff(type);
        }

        return 0;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void applyBuffServer(Dictionary dict)
    {
        var caller = _gameManager.GetPlayerCharacter((long)dict["caller_id"]);
        var target = _gameManager.GetPlayerCharacter((long)dict["target_id"]);
        var buff = BuffManager.Create((string)dict["guid"], caller, target);
        ApplyBuff(buff);
    }

    public List<IHitModifier> GetHitModifiers()
    {
        return new List<IHitModifier>();
    }

    public void RequestReposition(Vector2 newPosition)
    {
        var dict = new Godot.Collections.Dictionary<string, Variant>
        {
            ["position"] = newPosition
        };
        RpcId(1, MethodName.repositionServer, dict);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true,  TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void repositionServer(Godot.Collections.Dictionary<string, Variant> data)
    {
        this.GlobalPosition = (Vector2)data["position"];
    }

    public void RoundReset()
    {
        _buffManagerComponent.ClearAllBuffs();
        StatBlock.RemoveModifierSource(Damage.SOURCE_MODIFIER_ID);
        this._roundMaxLife = StatBlock.GetStat(StatType.Life);
    }
}