using Godot;
using System;
using CardBase.Scripts;
using CardBase.Scripts.Abilities.Buffs;

public partial class BuffIcon : Control
{
	private IBuff buff;

	[Export] public ProgressBar TimerBar;
	[Export] public TextureRect IconRect;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetProcess(false);

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		TimerBar.Value = buff.RemainingDuration / buff.Duration * 100;
		if (buff.RemainingDuration <= 0)
		{
			QueueFree();
		}
	}

	public void SetBuff(ref Buff rbuff)
	{
		buff = rbuff;
		IconRect.Texture = IconLoader.Instance.LoadImage(buff.IconPath);
		SetProcess(true);
	}
}
