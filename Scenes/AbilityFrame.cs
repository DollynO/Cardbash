using Godot;
using System;
using System.Globalization;
using CardBase.Scripts;
using CardBase.Scripts.Abilities;

public partial class AbilityFrame : TextureRect
{
	[Export] private TextureRect _abilityIcon;

	[Export] private Label _cdNumber;

	[Export] private ProgressBar _cdBar;

	[Export] private Label _stackCount;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_abilityIcon.Texture = null;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void UpdateUi(NetAbility ability)
	{
		if (ability == null)
		{
			_cdBar.Visible = false;
			_cdNumber.Visible = false;
			_stackCount.Text = string.Empty;
			_abilityIcon.Texture = null;
			return;
		}
		
		if (ability.Stacks.X < ability.Stacks.Y)
		{
			_cdBar.Visible = true;
			_cdNumber.Visible = true;
			_cdBar.Value = ability.Cooldowns.X / ability.Cooldowns.Y * 100;
			_cdNumber.Text = ability.Cooldowns.X.ToString("0.0");
		}
		else
		{
			_cdBar.Visible = false;
			_cdNumber.Visible = false;
		}

		_stackCount.Text = ability.Stacks.Y > 1 ? $"{ability.Stacks.X} / {ability.Stacks.Y}" : string.Empty;
		_abilityIcon.Texture ??= IconLoader.Instance.LoadImage(ability.IconPath);
	}
}
