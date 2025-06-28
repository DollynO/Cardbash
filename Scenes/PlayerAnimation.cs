using Godot;
using CardBase.Scripts.PlayerScripts;

public partial class PlayerAnimation : AnimatedSprite2D
{
	[Export] private MultiplayerSynchronizer _inputSynchronizer;
	private PlayerInput _input;
	private string animation_string = "idle_down";

	public override void _Ready()
	{
		_input = (PlayerInput)_inputSynchronizer;
	}

	public void UpdateAnimation()
	{
		if (_input.XDirection == 0
		    && _input.YDirection == 0)
		{
			Play(animation_string.Replace("move", "idle"));
			return;
		}

		// Set the correct walking animation
		if (_input.XDirection > 0)
		{
			animation_string = "move_side";
			FlipH = false;
		}
		else if (_input.XDirection < 0)
		{
			animation_string = "move_side";
			FlipH = true;
		}
		else if (_input.YDirection > 0)
		{
			animation_string = "move_down";
		}
		else if (_input.YDirection < 0)
		{
			animation_string = "move_up";
		}

		Play(animation_string);
	}
}
