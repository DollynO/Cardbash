using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.Cards;
using CardBase.Scripts.Items;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.PlayerScripts;

public partial class PlayerCharacter : CharacterbodyEntityComponent, ITeamAffiliation, ITargetableEntity
{
    private static readonly Vector2 EliminatedPosition = Vector2.One * -20000;

    [Export] private MultiplayerSynchronizer _inputSync;
    [Export] private AnimatedSprite2D _playerAnimation;
    public PlayerInput PlayerInput => _playerInput;
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
    private VisualComponent visualComponent;
    public ItemManagerComponent ItemManagerComponent { get; private set; }

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

    public Deck Deck { get; set; }
    public Array<Card> SelectedCards = new Array<Card>();
    public int TeamId { get; set; }
    public long PlayerId { get; set; }
    public bool IsTargetable => !_isEliminated && HealthComponent is { IsDead: false };

    private bool _statsInitialized;
    private bool _isEliminated;
    private bool _isSpectating;
    private uint _defaultCollisionLayer;
    private uint _defaultCollisionMask;
    private PlayerCharacter _spectateTarget;

    public override void _EnterTree()
    {
        _defaultCollisionLayer = CollisionLayer;
        _defaultCollisionMask = CollisionMask;

        _inputSync.SetMultiplayerAuthority(int.Parse(Name));
        _playerInput = (PlayerInput)_inputSync;

        _gameManager = (GameManager)GetNode("/root/Main/Game");

        if (int.Parse(Name) == Multiplayer.GetUniqueId())
        {
            _lookAtIndicator.Visible = true;
            _lookAtIndicator.Material = _lookAtIndicator.Material.Duplicate() as ShaderMaterial;
            var spriteMaterial = _lookAtIndicator.Material as ShaderMaterial;
            spriteMaterial?.SetShaderParameter("mask_color", new Godot.Color(1f, 1f, 1f));
            var teamColorArrow = ColorPlate.GetColor(TeamId);
            spriteMaterial?.SetShaderParameter("team_color", teamColorArrow);
            spriteMaterial?.SetShaderParameter("tolerance", 0.4);
            _camera.Enabled = true;
            _mapBounds = _gameManager.GetMapBoundry();
            _camera.LimitTop = (int)_mapBounds.Position.Y;
            _camera.LimitLeft = (int)_mapBounds.Position.X;
            _camera.LimitRight = (int)_mapBounds.Position.X + (int)_mapBounds.Size.X;
            _camera.LimitBottom = (int)_mapBounds.Position.Y + (int)_mapBounds.Size.Y;
        }

        ItemManagerComponent = new ItemManagerComponent();
        ItemManagerComponent.Name = nameof(ItemManagerComponent);
        AddComponent(ItemManagerComponent);

        BuffManagerComponent = new BuffManagerComponent();
        AddComponent(BuffManagerComponent);

        HealthComponent = new HealthComponent();
        AddComponent(HealthComponent);

        AbilityComponent = new AbilityComponent
        {
            Position = _characterCenterPoint.Position
        };
        AddComponent(AbilityComponent);

        var dac = new DamageAbleComponent();
        AddComponent(dac);

        StatBlock = new StatblockComponent();
        AddComponent(StatBlock);

        AddComponent(new MoveComponent());
        AddComponent(new MovementPredictionComponent());

        var aimComponent = new AimComponent(_characterCenterPoint, _lookAtDirectionPoint, _lookAtDirectionCorrection);
        AddComponent(aimComponent);

        visualComponent = new VisualComponent();
        visualComponent.SetAnimation("res://AnimationRes/PlayerAnimation/PlayerCharacterAnimation.tres", Vector2.Zero);
        var shader = GD.Load<Shader>("res://Shaders/PlayerCharacter_TeamColor.gdshader");
        var shaderMaterial = new ShaderMaterial();
        shaderMaterial.Shader = shader;
        shaderMaterial.SetShaderParameter("mask_color", new Vector4(0.341f, 0.227f, 0.196f, 1));
        shaderMaterial.SetShaderParameter("mask_color_2", new Vector4(0.251f, 0.153f, 0.09f, 1));
        shaderMaterial.SetShaderParameter("tolerance", 0.1f);
        var teamColor = ColorPlate.GetColor(TeamId);
        shaderMaterial.SetShaderParameter("team_color", teamColor);

        AddComponent(visualComponent);
        visualComponent.SetShader(shaderMaterial);

        var overhead = new OverHeadUiComponent();
        AddComponent(overhead);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsLocalPlayer() || !_isSpectating)
        {
            return;
        }

        if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            return;
        }

        var isSpectateSwitch = keyEvent.Keycode is Key.Tab or Key.Backtab
                               || keyEvent.PhysicalKeycode == Key.Tab;
        if (!isSpectateSwitch)
        {
            return;
        }

        SpectateRelative(keyEvent.ShiftPressed || keyEvent.Keycode == Key.Backtab ? -1 : 1);
        GetViewport().SetInputAsHandled();
    }

    public override async void _Ready()
    {
        if (!Multiplayer.IsServer() && PlayerId == Multiplayer.GetUniqueId())
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _gameManager.RpcId(1, GameManager.MethodName.ClientReady, Multiplayer.GetUniqueId());
        }
        else
        {
            _gameManager.RpcId(1, GameManager.MethodName.ClientReady, Multiplayer.GetUniqueId());
        }
    }

    public void InitializeServerStats()
    {
        if (!Multiplayer.IsServer() || _statsInitialized)
        {
            return;
        }

        _statsInitialized = true;
        defineCharacterStats();
    }

    private void defineCharacterStats()
    {
        StatBlock.Define(StatType.MovementSpeed, 150, 0, float.PositiveInfinity);
        StatBlock.Define(StatType.Life, 100, float.NegativeInfinity, float.PositiveInfinity);
        StatBlock.Define(StatType.Armor, 0, 0, float.PositiveInfinity);
        StatBlock.Define(StatType.EnergyShield, 0, float.NegativeInfinity, float.PositiveInfinity);
        StatBlock.Define(StatType.CritBonus, 0, 0, float.PositiveInfinity);
        StatBlock.Define(StatType.CritChance, 0, 0, 100);
        StatBlock.Define(StatType.Darkness, 0, 0, 20);
        StatBlock.Define(StatType.Blinding, 0, 0, 20);
        StatBlock.Define(StatType.DmgLightningBonus, 0, float.NegativeInfinity, float.PositiveInfinity);
        StatBlock.Define(StatType.DmgIceBonus, 0, float.NegativeInfinity, float.PositiveInfinity);
        StatBlock.Define(StatType.DmgFireBonus, 0, float.NegativeInfinity, float.PositiveInfinity);
        StatBlock.Define(StatType.DmgHolyBonus, 0, float.NegativeInfinity, float.PositiveInfinity);
        StatBlock.Define(StatType.DmgDarknessBonus, 0, float.NegativeInfinity, float.PositiveInfinity);
        StatBlock.Define(StatType.DmgPhysicalBonus, 0, float.NegativeInfinity, float.PositiveInfinity);
        StatBlock.Define(StatType.DmgPoisonBonus, 0, float.NegativeInfinity, float.PositiveInfinity);

        StatBlock.Define(StatType.AddPullRadius, 0, 0, float.PositiveInfinity);
        StatBlock.Define(StatType.AddPullStrength, 0, 0, float.PositiveInfinity);
        StatBlock.Define(StatType.CooldownReduction, 1, 0.2f, 1.8f); // max +-80% cooldown 
        StatBlock.Define(StatType.IncreasedMinionLife, 0, float.NegativeInfinity, float.PositiveInfinity);
    }

    public override void _Process(double delta)
    {
        visualComponent?.UpdateAnimation(_playerInput);
        ((PlayerAnimation)_playerAnimation).UpdateAnimation();
        UpdateSpectatorCamera();

        if (Multiplayer.IsServer())
        {
            if (TryGetComponent(out AbilityComponent abilityComponent))
            {
                abilityComponent.ProcessAbilities(delta, _playerInput.ConsumeAbilityKeyStates());
            }
        }
    }



    public void RoundReset(int roundIndex)
    {
        _isEliminated = false;
        Rpc(MethodName.syncEliminatedState, false);
        BuffManagerComponent.ClearAllBuffs();
        StatBlock.RemoveModifierSource(Damage.SOURCE_MODIFIER_ID);
        if (TryGetComponent(out HealthComponent healthComponent))
        {
            healthComponent.Reset(StatBlock.GetStat(StatType.Life));
        }

        if (TryGetComponent(out MoveComponent moveComponent))
        {
            moveComponent.IsMovementDisabled = true;
        }

        if (TryGetComponent(out AbilityComponent abilityComponent))
        {
            abilityComponent.RoundReset();
            abilityComponent.Disable();
        }

        EventBus.MatchEventBus.EmitRoundStart(new MatchEventArgs(roundIndex));
    }

    public void RoundStart()
    {
        if (TryGetComponent(out MoveComponent moveComponent))
        {
            moveComponent.IsMovementDisabled = false;
        }

        if (TryGetComponent(out AbilityComponent abilityComponent))
        {
            abilityComponent.Enable();
        }
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

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void exitStealthServer()
    {
        this.Modulate = new Godot.Color(this.Modulate, 1.0f);
    }

    public void Cleanup()
    {
        BuffManagerComponent.ClearAllBuffs();
        StatBlock.RemoveModifierSource(Damage.SOURCE_MODIFIER_ID);

        if (TryGetComponent(out MoveComponent moveComponent))
        {
            moveComponent.IsMovementDisabled = true;
        }

        if (TryGetComponent(out AbilityComponent abilityComponent))
        {
            abilityComponent.Cleanup();
            abilityComponent.Disable();
        }

        _isEliminated = true;
        GlobalPosition = EliminatedPosition;
        Rpc(MethodName.syncEliminatedState, true);
    }

    public PlayerCharacter GetCameraTarget()
    {
        return _isSpectating && _spectateTarget != null ? _spectateTarget : this;
    }

    private bool IsLocalPlayer()
    {
        return PlayerId == Multiplayer.GetUniqueId();
    }

    private void SetTargetable(bool targetable)
    {
        CollisionLayer = targetable ? _defaultCollisionLayer : 0;
        CollisionMask = targetable ? _defaultCollisionMask : 0;
    }

    private void StartSpectating()
    {
        if (!IsLocalPlayer())
        {
            return;
        }

        _isSpectating = true;
        _camera.Enabled = true;
        _camera.SetAsTopLevel(true);
        SpectateRelative(1);
    }

    private void StopSpectating()
    {
        if (!IsLocalPlayer())
        {
            return;
        }

        _isSpectating = false;
        _spectateTarget = null;
        _camera.SetAsTopLevel(false);
        _camera.Position = Vector2.Zero;
        _camera.Rotation = 0;
        _camera.Enabled = true;
    }

    private void SpectateRelative(int direction)
    {
        var targets = GetSpectateTargets();
        if (targets.Count == 0)
        {
            _spectateTarget = null;
            _camera.GlobalPosition = _mapBounds.Position + _mapBounds.Size / 2;
            return;
        }

        var currentIndex = targets.IndexOf(_spectateTarget);
        if (currentIndex < 0)
        {
            currentIndex = direction > 0 ? -1 : 0;
        }

        var nextIndex = PosMod(currentIndex + direction, targets.Count);
        _spectateTarget = targets[nextIndex];
        _camera.GlobalPosition = _spectateTarget.GlobalPosition;
    }

    private List<PlayerCharacter> GetSpectateTargets()
    {
        return _gameManager.GetPlayers()
            .Where(player => player != this && player.IsTargetable)
            .OrderBy(player => player.PlayerId)
            .ToList();
    }

    private void UpdateSpectatorCamera()
    {
        if (!_isSpectating || !IsLocalPlayer())
        {
            return;
        }

        if (_spectateTarget == null || !_spectateTarget.IsTargetable)
        {
            SpectateRelative(1);
            return;
        }

        _camera.GlobalPosition = _spectateTarget.GlobalPosition;
    }

    private static int PosMod(int value, int modulo)
    {
        return (value % modulo + modulo) % modulo;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void syncEliminatedState(bool eliminated)
    {
        _isEliminated = eliminated;
        SetTargetable(!eliminated);

        if (eliminated)
        {
            if (TryGetComponent(out MoveComponent moveComponent))
            {
                moveComponent.IsMovementDisabled = true;
            }

            if (TryGetComponent(out AbilityComponent abilityComponent))
            {
                abilityComponent.Disable();
            }

            GlobalPosition = EliminatedPosition;
            StartSpectating();
        }
        else
        {
            StopSpectating();
        }
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
