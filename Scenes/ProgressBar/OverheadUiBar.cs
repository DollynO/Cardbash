using Godot;
using System;
using System.Drawing;
using Color = Godot.Color;

public partial class OverheadUiBar : Node2D
{
	private ProgressBar Bar;

	public float MaxValue
	{
		get => (float)Bar.MaxValue;
		set => Bar.MaxValue = value;
	}

	public float Value
	{
		get => (float)Bar.Value;
		set =>  Bar.Value = value;
	}

	public OverheadUiBar(Color backgroundColor, Color foregroundColor)
	{
		Bar = new ProgressBar();
		Bar.SetAnchorsPreset(Control.LayoutPreset.Center);
		Bar.Size = new Vector2(64, 4);
		Bar.Position = new Vector2(-32, -2);
		Bar.ShowPercentage = false;
		SetFillColor(backgroundColor, foregroundColor);
	}
	
	public OverheadUiBar() {}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AddChild(Bar);
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
		Bar.AddThemeStyleboxOverride("background", bg);

		// Fill
		var fill = new StyleBoxFlat();
		fill.BgColor = innerColor;
		Bar.AddThemeStyleboxOverride("fill", fill);
	}
}
