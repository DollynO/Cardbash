using Godot;
using Godot.Collections;

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
    [Export]
    public float XDirection;
	
    [Export]
    public float YDirection;
    
    [Export]
    public Array<AbilityKeyState> KeyState = new();
    
    [Export]
    public Node2D LookAtRotation;

    [Export] public float LookAtRotationValue;
	
    public override void _Ready()
    {
        if (GetMultiplayerAuthority() != Multiplayer.GetUniqueId())
        {
            SetPhysicsProcess(false);
        }
        KeyState.Add(AbilityKeyState.ABILITY_NONE);
        KeyState.Add(AbilityKeyState.ABILITY_NONE);
        KeyState.Add(AbilityKeyState.ABILITY_NONE);
        KeyState.Add(AbilityKeyState.ABILITY_NONE);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        XDirection = Input.GetAxis("MoveLeft", "MoveRight");
        YDirection = Input.GetAxis("MoveUp", "MoveDown");
        var state = AbilityKeyState.ABILITY_NONE;
        if ((int)KeyState[1] + (int)KeyState[2] + (int)KeyState[3] == 0)
        {
            setKeyState(ref state, "Ability1");
            KeyState[0] = state;
        }

        if ((int)KeyState[0] + (int)KeyState[2] + (int)KeyState[3] == 0)
        {
            setKeyState(ref state, "Ability2");
            KeyState[1] = state;
        }

        if ((int)KeyState[1] + (int)KeyState[0] + (int)KeyState[3] == 0)
        {
            setKeyState(ref state, "Ability3");
            KeyState[2] = state;
        }

        if ((int)KeyState[1] + (int)KeyState[2] + (int)KeyState[0] == 0)
        {
            setKeyState(ref state, "Ability4");
            KeyState[3] = state;
        }

        KeyState = new Array<AbilityKeyState>(KeyState);
        
        LookAtRotation.LookAt(GetParent<PlayerCharacter>().GetGlobalMousePosition());
        LookAtRotation.Rotate(-Mathf.Tau / 4);
        LookAtRotationValue = LookAtRotation.Rotation;
    }

    private void setKeyState(ref AbilityKeyState keyState, string actionName)
    {
        if (Input.IsActionJustPressed(actionName))
        {
            keyState = AbilityKeyState.ABILITY_PRESSED;
        } else if (Input.IsActionPressed(actionName))
        {
            keyState = AbilityKeyState.ABILITY_HOLD;
        } else if (Input.IsActionJustReleased(actionName))
        {
            keyState = AbilityKeyState.ABILITY_RELEASED;
        }
        else
        {
            keyState = AbilityKeyState.ABILITY_NONE;
        }
    }
}
