using Godot;
using System;
using CardBase.Scripts;

public partial class StatOverviewElement : Control
{
    [Export] private Tooltip _tooltip;
    [Export] private Label _value;
    [Export] private TextureRect _icon;

    [Export] private HBoxContainer _container;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _tooltip.Visible = false;

        _container.MouseEntered += ContainerOnMouseEntered;
        _container.MouseExited += ContainerOnMouseExited;
    }

    private void ContainerOnMouseExited()
    {
        _tooltip.HideTooltip();
    }

    private void ContainerOnMouseEntered()
    {
        _tooltip.ShowTooltip(GetGlobalMousePosition());
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public void Init(StatOverviewElementData data)
    {
        _value.Text = data.Value;
        _icon.Texture = IconLoader.Instance.LoadImage(data.IconPath);
        _tooltip.Init(data.Description, data.Name);
    }
}

public record StatOverviewElementData(string IconPath, string Name, string Description, string Value)
{
    public string IconPath = IconPath;
    public string Name = Name;
    public string Description = Description;
    public string Value = Value;
}
