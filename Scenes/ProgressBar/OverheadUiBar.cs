using Godot;
using System;

public partial class OverheadUiBar : Node2D
{
	[Export] private ProgressBar bar = new ();

	public float MaxValue
	{
		get => (float)bar.MaxValue;
		set => bar.MaxValue = value;
	}

	public float Value
	{
		get => (float)bar.Value;
		set =>  bar.Value = value;
	}
	
	private Color backgroundColor;
	private Color foregroundColor;

	public OverheadUiBar(Color backgroundColor, Color foregroundColor)
	{
		this.backgroundColor = backgroundColor;
		this.foregroundColor = foregroundColor;
	}
	
	public OverheadUiBar() {}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetFillColor(backgroundColor, foregroundColor);
		Value = 0;
		MaxValue = 100;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SetFillColor(Color outerColor, Color innerColor)
	{
		var bg = new StyleBoxFlat();
		bg.BgColor = outerColor;
		bar.AddThemeStyleboxOverride("background", bg);

		// Fill
		var fill = new StyleBoxFlat();
		fill.BgColor = innerColor;
		bar.AddThemeStyleboxOverride("fill", fill);
	}
}
