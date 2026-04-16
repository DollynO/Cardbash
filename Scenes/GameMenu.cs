using Godot;
using System;

public partial class GameMenu : CanvasLayer
{
	[Export] private TextureButton _leaveMatch;
	[Export] private TextureButton _settings;
	
	[Export] private GameManager _gameManager;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_settings.Disabled = true;
		_leaveMatch.ButtonDown += leaveMatchOnButtonDown;
		Visible = false;
	}

	private void leaveMatchOnButtonDown()
	{
		_gameManager.LeaveGame(Multiplayer.GetUniqueId());
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("OpenGameMenu"))
		{
			Visible = !Visible;
		}
	}
}
