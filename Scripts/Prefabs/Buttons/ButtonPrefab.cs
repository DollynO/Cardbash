using Godot;
using System;

public partial class ButtonPrefab : TextureButton
{
    [Export] private Label TextLabel;

    [Export] private String Text;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        SetText(Text);
    }

    public void SetText(string text)
    {
        Text = text;
        if (TextLabel != null)
        {
            TextLabel.Text = text;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
