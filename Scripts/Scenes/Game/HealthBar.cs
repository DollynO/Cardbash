using Godot;
using System;

public partial class HealthBar : ProgressBar
{

    [Export] private Label _label;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    public void SetHealth(float current, float max)
    {
        this.MaxValue = max;
        this.Value = current;
        _label.Text = $"{current}/{max}";
    }
}
