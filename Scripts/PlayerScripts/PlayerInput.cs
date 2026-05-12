using System.Collections.Generic;
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
    private static readonly string[] AbilityActions = { "Ability1", "Ability2", "Ability3", "Ability4" };
    
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
        LookAtRotation.LookAt(ClientGlobalMousePosition);
        LookAtRotation.Rotate(-Mathf.Tau / 4);
        LookAtRotationValue = LookAtRotation.Rotation;
    }

    public override void _Input(InputEvent @event)
    {
        if (GetMultiplayerAuthority() != Multiplayer.GetUniqueId())
        {
            return;
        }

        if (@event is InputEventKey { Echo: true })
        {
            return;
        }

        for (var i = 0; i < AbilityActions.Length; i++)
        {
            if (@event.IsActionPressed(AbilityActions[i]))
            {
                SendAbilityPressed(i);
            }

            if (@event.IsActionReleased(AbilityActions[i]))
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
