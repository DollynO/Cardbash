namespace CardBase.Scripts.Abilities.Buffs;

using Godot;
using System.Collections.Generic;

public partial class BuffRow2D : Node2D
{
    [Export] public int Spacing = 2;          // pixels between icons (world pixels)
    [Export] public bool KeepScreenSize = true; // counteract camera zoom to keep icon size constant on screen

    private Camera2D _cam;

    public override void _Ready()
    {
        _cam = GetViewport().GetCamera2D();
        Rearrange();
    }

    public override void _Process(double delta)
    {
        // Optional: keep constant on screen even if camera zooms
        if (KeepScreenSize && _cam != null)
            Scale = new Vector2(1f / _cam.Zoom.X, 1f / _cam.Zoom.Y);
    }

    public void AddBuffIcon(BuffIconTemplate icon)
    {
        //AddChild(icon);
        Rearrange();
    }

    public void RemoveBuffIcon(BuffIconTemplate icon)
    {
        if (icon.GetParent() == this)
            icon.QueueFree();
        Rearrange();
    }

    private void Rearrange()
    {
        float x = 0f;
        foreach (var child in GetChildren())
        {
            if (child is BuffIconTemplate icon)
            {
                icon.Position = new Vector2(x, 0);
                x += 20 + Spacing;
            }
        }
    }
}
