using System.Collections.Generic;
using CardBase.Scripts;
using Godot;

namespace CardBase.Scripts.PlayerScripts;

public enum AbilityNumber
{
    ABILITY_0 = 0,
    ABILITY_1 = 1,
    ABILITY_2 = 2,
    ABILITY_3 = 3,
}

public enum AbilityKeyState
{
    ABILITY_NONE = 0,
    ABILITY_PRESSED = 1,
    ABILITY_HOLD = 2,
    ABILITY_RELEASED = 3,
}

public partial class PlayerInput : MultiplayerSynchronizer
{
    private const int AbilitySlotCount = 4;
    
    [Export]
    public float XDirection;

    [Export]
    public float YDirection;

    private readonly Queue<AbilityKeyState>[] pendingAbilityStates =
    {
        new Queue<AbilityKeyState>(),
        new Queue<AbilityKeyState>(),
        new Queue<AbilityKeyState>(),
        new Queue<AbilityKeyState>()
    };
    private readonly bool[] heldAbilities = new bool[AbilitySlotCount];

    [Export]
    public Node2D LookAtRotation;

    [Export] public float LookAtRotationValue;

    [Export] public Vector2 ClientGlobalMousePosition;

    public override void _Ready()
    {
        AbilityKeyBindings.ApplySavedBindings();
        if (GetMultiplayerAuthority() != Multiplayer.GetUniqueId())
        {
            SetPhysicsProcess(false);
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        XDirection = Input.GetAxis("MoveLeft", "MoveRight");
        YDirection = Input.GetAxis("MoveUp", "MoveDown");
        ClientGlobalMousePosition = GetParent<PlayerCharacter>().GetGlobalMousePosition();
        var desiredRotation = (ClientGlobalMousePosition - LookAtRotation.GlobalPosition).Angle() - Mathf.Tau / 4;
        LookAtRotation.Rotation = Mathf.LerpAngle(
            LookAtRotation.Rotation,
            desiredRotation,
            GetAimRotationSpeed());
        LookAtRotationValue = LookAtRotation.Rotation;
    }

    private float GetAimRotationSpeed()
    {
        var player = GetParent<PlayerCharacter>();
        if (player?.StatBlock == null || !player.StatBlock.ReplicatedCurrent.ContainsKey((int)StatType.AimRotationSpeed))
        {
            return 1f;
        }

        return Mathf.Clamp(player.StatBlock.GetStat(StatType.AimRotationSpeed), 0f, 1f);
    }

    public override void _Input(InputEvent @event)
    {
        if (GetMultiplayerAuthority() != Multiplayer.GetUniqueId())
        {
            return;
        }

        if (@event is InputEventKey { Echo: true } or InputEventKey {CtrlPressed: true})
        {
            return;
        }

        for (var i = 0; i < AbilityKeyBindings.AbilityActions.Length; i++)
        {
            if (@event.IsActionPressed(AbilityKeyBindings.AbilityActions[i]))
            {
                SendAbilityPressed(i);
            }

            if (@event.IsActionReleased(AbilityKeyBindings.AbilityActions[i]))
            {
                SendAbilityReleased(i);
            }
        }
    }

    public AbilityKeyState[] ConsumeAbilityKeyStates()
    {
        var states = new AbilityKeyState[AbilitySlotCount];
        for (var i = 0; i < AbilitySlotCount; i++)
        {
            if (pendingAbilityStates[i].Count > 0)
            {
                states[i] = pendingAbilityStates[i].Dequeue();
            }
            else if (heldAbilities[i])
            {
                states[i] = AbilityKeyState.ABILITY_HOLD;
            }
            else
            {
                states[i] = AbilityKeyState.ABILITY_NONE;
            }
        }

        return states;
    }

    private void SendAbilityPressed(int slot)
    {
        if (Multiplayer.IsServer())
        {
            RequestAbilityPressed(slot);
            return;
        }

        RpcId(1, MethodName.RequestAbilityPressed, slot);
    }

    private void SendAbilityReleased(int slot)
    {
        if (Multiplayer.IsServer())
        {
            RequestAbilityReleased(slot);
            return;
        }

        RpcId(1, MethodName.RequestAbilityReleased, slot);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RequestAbilityPressed(int slot)
    {
        if (!IsValidAbilityRequest(slot))
        {
            return;
        }

        pendingAbilityStates[slot].Enqueue(AbilityKeyState.ABILITY_PRESSED);
        heldAbilities[slot] = true;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RequestAbilityReleased(int slot)
    {
        if (!IsValidAbilityRequest(slot))
        {
            return;
        }

        pendingAbilityStates[slot].Enqueue(AbilityKeyState.ABILITY_RELEASED);
        heldAbilities[slot] = false;
    }

    private bool IsValidAbilityRequest(int slot)
    {
        if (slot is < 0 or >= AbilitySlotCount)
        {
            return false;
        }

        var senderId = Multiplayer.GetRemoteSenderId();
        return senderId == 0 || senderId == GetMultiplayerAuthority();
    }
}
