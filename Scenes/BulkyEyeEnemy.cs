using Godot;
using System;
using CardBase.Scripts.PlayerScripts;

public partial class BulkyEyeEnemy : CharacterBody2D
{
	public const float Speed = 300.0f;
	[Export] private NavigationAgent2D nav;
	private PlayerCharacter Owner;
	private PlayerCharacter Target;

	public override void _Ready()
	{
		CallDeferred(nameof(actorSetup));
		nav.VelocityComputed += OnVelocityComputed;
	}

	private async void actorSetup()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		
	}
	
	public override void _PhysicsProcess(double delta)
	{
		MoveTowardsPlayer();
	}

	private void MoveTowardsPlayer()
	{
		if (Owner == null)
		{
			return;
		}

		if (nav.IsNavigationFinished())
		{
			return;
		}

		nav.TargetPosition = Owner.GlobalPosition;
		

		var nextPathPos = nav.GetNextPathPosition();

		var new_velocity = GlobalPosition.DirectionTo(nextPathPos) * Speed;

		if (nav.AvoidanceEnabled)
		{
			nav.SetVelocity(new_velocity);
		}
		else
		{
			OnVelocityComputed(new_velocity);
		}

		MoveAndSlide();
	}

	private void OnVelocityComputed(Vector2 safeVelocity)
	{
		Velocity = safeVelocity;
	}
}
