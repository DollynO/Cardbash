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


public class AbilityTransferStat
{
    public string ImagePath { get; set; }
    public Vector2 Stacks { get; set; }
    public double RemainingCooldown { get; set; }
    public string Description { get; set; }
    public string GUID { get; set; }
}

public partial class PlayerCharacter : CharacterBody2D, IHitableObject
{
    [Export] private MultiplayerSynchronizer _inputSync;
    [Export] private AnimatedSprite2D _playerAnimation;
    private PlayerInput _playerInput;
    private GameManager _gameManager;

    [Export] public StatBlockComponent StatBlock;
    public readonly List<DamageModifier> DamageModifier = new();

    [Export] private Label _playerNameLabel;
    
    [Export] private Sprite2D _lookAtIndicator;
    [Export] private Node2D _lookAtDirectionPoint;
    private Vector2 _lookAtDirectionCorrection = Vector2.FromAngle(Mathf.Tau / 4);
    [Export] private Node2D _characterCenterPoint;
    
    [Export]
    private Camera2D _camera;

    private Rect2 _mapBounds;

    [Export] public BuffManagerComponent BuffManagerComponent;
    
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
    
    private System.Collections.Generic.Dictionary<PlayerCharacter, Darkness> _darknessInstances = new();
    private System.Collections.Generic.Dictionary<PlayerCharacter, PoisonDebuff> _poisonInstances = new();
    private System.Collections.Generic.Dictionary<PlayerCharacter, Frost> _frostInstances = new();

    public AbilityController AbilityController;
    public MoveController MoveController;
    public HealthController HealthController;

    public event EventHandler<AbilityEventArgs>? AbilityCasted;
    public void NotifyAbilityCasted(Ability ability)
    {
        this.AbilityCasted?.Invoke(this, new AbilityEventArgs(ability));
    }
    
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
    
    public event EventHandler<DamageEventArgs>? DamageTaken;
    public void NotifyDamageTaken(List<Damage> damage)
    {
        this.DamageTaken?.Invoke(this, new DamageEventArgs(damage));
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
        defineCharacterStats();
        
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
        
        AbilityController = new AbilityController(this);
        AddChild(AbilityController);

        MoveController = new MoveController(this);
        AddChild(MoveController);

        HealthController = new HealthController();
        AddChild(HealthController);
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
    }

    public override void _PhysicsProcess(double delta)
    {
        MoveController.ProcessMovement(delta, new Vector2(_playerInput.XDirection,  _playerInput.YDirection));
    }

    public override void _Process(double delta)
    {
        ((PlayerAnimation)_playerAnimation).UpdateAnimation();
        if (Multiplayer.IsServer())
        {
            AbilityController.ProcessAbilities(delta, _playerInput.KeyState.ToArray());
        }
    }
    
    public bool IsDead()
    {
        return StatBlock.GetStat(StatType.Life) <= 0;
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

    public Node2D GetCharacterCenterPoint()
    {
        return _characterCenterPoint;
    }

    public void ApplyDamage(HitContext ctx)
    {
        if (Multiplayer.IsServer())
        {
            if (IsDead())
            {
                return;
            }
            
            var hitMods = ctx.Source.GetHitModifiers();
            var abilityHitMods 
                = ctx.Source.AbilityController.Abilities.FirstOrDefault(a => a.GUID == ctx.AbilityGuid)?.GetHitModifiers();
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
                HealthController.ApplyDamage(ctx.Damages[dmg.Key].DamageNumber);
                ApplyDamageTypeAilment(dmg.Value.Type, dmg.Value.AilmentChange, ctx.Source);
            }
            
            if (IsDead())
            {
                EmitSignal(SignalName.OnKilled, this, ctx.Source);
                BuffManagerComponent.ClearAllBuffs();
            }

            NotifyDamageTaken(ctx.Damages.Values.ToList());
            
            foreach (var mod in hitMods)
            {
                mod.ApplyAfter(ctx);
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
        var chance = rnd.NextDouble();
        if (chance > ailmentChance)
        {
            return;
        }
        
        switch (type)
        {
            case DamageType.Fire:
                BuffManagerComponent.ApplyBuff(new BurnDebuff(attacker, this));
                break;
            case DamageType.Physical:
                break;
            case DamageType.Poison:
                if (!_poisonInstances.ContainsKey(attacker))
                {
                    _poisonInstances.Add(attacker, new PoisonDebuff(attacker, this));
                }
                var poison =  _poisonInstances[attacker];
                BuffManagerComponent.ApplyBuff(poison);
                break;
            case DamageType.Ice:
                if (!_frostInstances.ContainsKey(attacker))
                {
                    _frostInstances.Add(attacker, new Frost(attacker, this));
                }
                var frost = _frostInstances[attacker];
                BuffManagerComponent.ApplyBuff(frost);
                break;
            case DamageType.Lightning:
                BuffManagerComponent.ApplyBuff(new ShockDebuff(attacker, this));
                break;
            case DamageType.Darkness: 
                if (!_darknessInstances.ContainsKey(attacker))
                {
                    _darknessInstances.Add(attacker, new Darkness(attacker, this));
                }
                var darkness =  _darknessInstances[attacker];
                BuffManagerComponent.ApplyBuff(darkness);
                break;
            case DamageType.Holy:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public List<IHitModifier> GetHitModifiers()
    {
        return new List<IHitModifier>();
    }

    public void RoundReset()
    {
        BuffManagerComponent.ClearAllBuffs();
        StatBlock.RemoveModifierSource(Damage.SOURCE_MODIFIER_ID);
        HealthController.Reset(StatBlock.GetStat(StatType.Life));
        this.NewRoundStarted?.Invoke(this,  EventArgs.Empty);
    }

    public void RequestAddDamageModifier(DamageModifier  modifier)
    {
        var dict = DamageModifierHelper.ToDict(modifier);
        Rpc(MethodName.AddDamageModifierServer, dict);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void AddDamageModifierServer(Variant data)
    {
        var dict = data.AsGodotDictionary<string, Variant>();
        var mod = DamageModifierHelper.FromDict(dict);
        
        DamageModifier.Add(mod);
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